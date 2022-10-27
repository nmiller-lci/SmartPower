using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.BLE.Platforms.Shared.BleManager;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DryIoc;
using IDS.UI.UserDialogs;
using IBleDevice = Plugin.BLE.Abstractions.Contracts.IDevice;

namespace SmartPower.Services
{
    public class AppDirectConnectionMonitorPairBond : CommonDisposable
    {
        private const string LogTag = nameof(AppDirectConnectionMonitorPairBond);

        private CancellationTokenSource? _userAlertCts;
        private readonly CancellationToken _userAlertCt;
        private int _userAlertedStatus;
        private const int Idle = 0;
        private const int Alerted = 1;

        private IDisposable? _deviceNotPairBondedToPeerDisposable;
        private IDisposable? _peerLostPairBondWithDeviceDisposable;

        private readonly IEndPointConnectionBle? _bleConnection;
        private readonly IUserDialogService _userDialogService;

        public AppDirectConnectionMonitorPairBond(IEndPointConnectionBle? bleConnection)
        {
            _userDialogService = App.AppContainer.Resolve<IUserDialogService>(IfUnresolved.ReturnDefault);

            _bleConnection = bleConnection;
            if (bleConnection == null)
                return;
            
            _userAlertCts = new CancellationTokenSource();
            _userAlertCt = _userAlertCts.Token;
            _userAlertedStatus = Idle;

            _deviceNotPairBondedToPeerDisposable = Observable
                .FromEventPattern<BondErrorEventArgs>(
                    h => BleManager.Instance.NotBonded += h,
                    h => BleManager.Instance.NotBonded -= h)
                .Where(ep => bleConnection.ConnectionGuid.Equals(ep.EventArgs.Id))
                .Subscribe(ep => HandlePairBondIssue(ep.EventArgs.Device, ep.EventArgs.Id, false));

            _peerLostPairBondWithDeviceDisposable = Observable
                .FromEventPattern<BondErrorEventArgs>(
                    h => BleManager.Instance.PeripheralLostBondInfo += h,
                    h => BleManager.Instance.PeripheralLostBondInfo -= h)
                .Where(ep => bleConnection.ConnectionGuid.Equals(ep.EventArgs.Id))
                .Subscribe(ep => HandlePairBondIssue(ep.EventArgs.Device, ep.EventArgs.Id, true));
        }

        public override void Dispose(bool disposing)
        {
            _deviceNotPairBondedToPeerDisposable?.TryDispose();
            _deviceNotPairBondedToPeerDisposable = null;

            _peerLostPairBondWithDeviceDisposable?.TryDispose();
            _peerLostPairBondWithDeviceDisposable = null;

            _userAlertCts?.TryCancelAndDispose();
            _userAlertCts = null;

            _userAlertedStatus = Idle;
        }

        private void HandlePairBondIssue(IBleDevice device, Guid deviceId, bool removePairBondInfo)
        {
            if (_bleConnection == null)
                return;

            // Make sure the Pair Bond Issue is for us
            //
            if (_bleConnection.ConnectionGuid != device.Id)
                return;

            // Do not attempt to re-alert the User if they have already ben alerted during the current conncetion cycle
            // 
            if (Interlocked.CompareExchange(ref _userAlertedStatus, Alerted, Idle) == Alerted)
                return;

            BleScannerService.Instance
                .TryGetDeviceAsync<IPairableDeviceScanResult>(device?.Id ?? deviceId, _userAlertCt)
                .ContinueWith(async scanTask =>
                {
                    try
                    {
                        // Clear out connection settings
                        // 
                        if (AppSettings.Instance.SelectedRvGatewayConnection.Equals(_bleConnection))
                            AppSettings.Instance.SetSelectedRvGatewayConnection(AppSettings.DefaultRvDirectConnectionNone, true);

                        if (AppSettings.Instance.SelectedBrakingSystemGatewayConnection.Equals(_bleConnection))
                            AppSettings.Instance.SetSelectedBrakingSystemGatewayConnection(AppSettings.DefaultRvDirectConnectionNone, true);

                        var gatewayName = !string.IsNullOrWhiteSpace(device?.Name)
                            ? $"device named \"{device.Name}\""
                            : scanTask.IsCompleted
                                ? $"device named \"{scanTask.Result.DeviceName}\""
                                : "gateway device";

                        var userDevice = Xamarin.Essentials.DeviceInfo.Idiom.ToString().ToLower();

                        if (removePairBondInfo)
                        {
                            TaggedLog.Warning(LogTag, $"HandlePairBondIssue for {gatewayName}");
                            
                            await _userDialogService.AlertAsync(
                                "Gateway Connection Issue",
                                $"The gateway {gatewayName} encountered an issue while connecting. Please try re-adding it to your {userDevice}," +
                                "SETTINGS", _userAlertCt);

                            BleManager.Instance.GoToDeviceSettings();
                        }
                        else
                        {
                            await _userDialogService.AlertAsync(
                                "Gateway Connection Issue",
                                $"The gateway encountered an issue while connecting and needs to be paired with your " +
                                $"{userDevice} again. From your {userDevice}'s Bluetooth settings, you will need to " +
                                $"forget the {gatewayName} before your {userDevice} can pair with it again.",
                                "SETTINGS", _userAlertCt);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        /* ignored */
                        // Acr Dialogs will throw if the App is backgrounded and the cancellation token
                        // is canceled in the OnStop method before the User has dismissed the dialog.
                        // 
                    }
                    catch (Exception e)
                    {
                        // There shouldn't be any other exceptions here; but, we'll catch and print them regardless.
                        //
                        TaggedLog.Warning(LogTag, $"{e.GetType().Name} - {e.Message}\n{e.StackTrace}");
                    }
                }, CancellationToken.None);
        }

    }
}
