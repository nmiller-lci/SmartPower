using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using IDS.Core.IDS_CAN;
using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using SmartPower.Services;
using SmartPower.UserInterface.ScanVin;
using SmartPower.UserInterface.VIN;
using OneControl.Devices.AccessoryGateway;
using OneControl.Devices.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.AwningSensor;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;
using ZXing;
using IDS.UI.UserDialogs;
using ImTools;
using SmartPower.Connections.Rv;
using SmartPower.Model;
using SmartPower.Resources;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Devices.AwningSensor;
using OneControl.Devices;
using Xamarin.Forms;

namespace SmartPower.UserInterface.Pairing
{
    public class PairDeviceViewModel : BaseViewModel, IViewModelResumePause, IViewModelStartStop
    {
        public PairDeviceViewModel(INavigationService navigationService,
            ILogicalDeviceService logicalDeviceService,
            IDirectBatteryMonitorBle directBatteryMonitorBle,
            IDirectAwningSensorBle directAwningSensorBle,
            IAccessoryGatewayPairingService accessoryGatewayPairingService,
            IBundledDataService bundledDataService,
            IUserDialogService dialogService,
            ISessionService sessionService)
            : base(navigationService)
        {
            _navigationService = navigationService;
            _directBatteryMonitorBle = directBatteryMonitorBle;
            _directAwningSensorBle = directAwningSensorBle;
            _logicalDeviceService = logicalDeviceService;
            _accessoryGatewayPairingService = accessoryGatewayPairingService;
            _bundledDataService = bundledDataService;
            _dialogService = dialogService;
            _sessionService = sessionService;
        }

        #region Private Properties

        private readonly INavigationService _navigationService;
        private readonly IBundledDataService _bundledDataService;
        private readonly ILogicalDeviceService _logicalDeviceService;
        private readonly IDirectBatteryMonitorBle _directBatteryMonitorBle;
        private readonly IDirectAwningSensorBle _directAwningSensorBle;
        private readonly IAccessoryGatewayPairingService _accessoryGatewayPairingService;
        private readonly IUserDialogService _dialogService;
        private readonly ISessionService _sessionService;
        private const string LogTag = nameof(PairDeviceViewModel);
        private const int CheckForConnectionCompletedMs = 200;
        private const int CheckForGatewayAvailableDelayMs = 100;
        private const int LoopOperationsDelayMs = 2000;
        private const int MaxRvConnectionWaitTimeMs = 20000;
        private const int MaxGetAccessoryGatewayWaitTimeBeforeShowingErrorMs = 20000;
        private ConcurrentDictionary<string, bool> _scannedQrCodesQueue = new();
        private readonly object _scannedQrCodesQueueLock = new object();
        private CancellationTokenSource _resetScreenCancellationTokenSource;
        private CancellationTokenSource _pairDeviceCancellationTokenSource;

        #endregion

        #region Properties

        private bool _isScanning;

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        private string? _selectedFloorPlan;

        public string? SelectedFloorPlan
        {
            get => _selectedFloorPlan;
            set => SetProperty(ref _selectedFloorPlan, value);
        }

        private string? _vin;

        public string? Vin
        {
            get => _vin;
            set => SetProperty(ref _vin, value);
        }

        private List<IPairableDeviceCell> _pairDevices;

        public List<IPairableDeviceCell> PairDevices
        {
            get => _pairDevices;
            set => SetProperty(ref _pairDevices, value);
        }

        private bool _hasCameraPermissions;

        public bool HasCameraPermissions
        {
            get => _hasCameraPermissions;
            set => SetProperty(ref _hasCameraPermissions, value);
        }

        private bool _hasLocationPermissions;

        public bool HasLocationPermissions
        {
            get => _hasLocationPermissions;
            set => SetProperty(ref _hasLocationPermissions, value);
        }

        #endregion

        #region Lifecycle

        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters,
            CancellationToken resumePauseCancellationToken)
        {
            if (PairDevices is null)
            {
                PairDevices = new List<IPairableDeviceCell>();
            }

            if (parameters?.ContainsKey(VinAndFloorPlanViewModel.FloorPlanKey) == true)
            {
                SelectedFloorPlan = parameters[VinAndFloorPlanViewModel.FloorPlanKey].ToString();
                Vin = parameters[ScanVinViewModel.VinParameterKey].ToString();

                if (!String.IsNullOrEmpty(Vin))
                    await GetDataAsync();
            }

            BleScannerService.Instance.Start(false);

            IsScanning = true;
        }

        public void OnPause(PauseReason reason)
        {
            IsScanning = false;
        }

        public Task OnStartAsync(INavigationParameters? parameters, CancellationToken startStopCancellationToken)
        {
            return Task.CompletedTask;
        }

        public void OnStop()
        {
            StopAllServices();
        }

        #endregion

        #region Commands

        public ICommand BackCommand => new AsyncCommand(async () => await NavigationService.GoBackAsync());
        public ICommand ResetSettingsCommand => new AsyncCommand(async () => await ResetSettingsAsync());

        public ICommand DoneCommand => new AsyncCommand(async () =>
        {
            if (GetHasIncompleteConfiguration())
            {
                if (!await _dialogService.ConfirmAsync(
                        Strings.incomplete_configuration,
                        Strings.your_configuration_is_incomplete,
                        Strings.yes,
                        Strings.cancel,
                        StartStopCancellationToken))
                {
                    return;
                }
            }

            _sessionService.RecordSessionEnded(Vin);
            await NavigationService.GoBackAsync();
        });

        private bool GetHasIncompleteConfiguration()
        {
            var completedStates = new List<ConnectionState> { ConnectionState.Connected, ConnectionState.Verified };
            var hasAnySingleDeviceNotConfigured = PairDevices.Where(x => x is PairDeviceCellModel).Any(x => !completedStates.Contains(x.State));
            if (hasAnySingleDeviceNotConfigured)
                return true;

            if (PairDevices.FirstOrDefault(x => x is PairWindSensorCellModel) is PairWindSensorCellModel pairWindSensorCellModel)
            {
                var hasAnyWindSensorsNotConfigured = pairWindSensorCellModel.Devices.Any(x => !completedStates.Contains(x.State));
                if (hasAnyWindSensorsNotConfigured)
                    return true;
            }

            return false;
        }

        public IEnumerable<BarcodeFormat> PossibleFormats { get; } = new List<BarcodeFormat> { BarcodeFormat.QR_CODE };

        private ICommand? _scanResultCommand;

        public ICommand ScanResultCommand => _scanResultCommand ??= new AsyncCommand<Result>(result =>
        {
            if (result != null) OnScanResult(result);
            return Task.CompletedTask;
        });

        private void OnScanResult(Result result)
        {
            DEVICE_TYPE deviceType = DEVICE_TYPE.UNKNOWN;

            var qrScanResult = QrScanResult.TryParseQrCode(result.Text);

            lock (_scannedQrCodesQueueLock)
            {
                if (_scannedQrCodesQueue.ContainsKey(result.Text))
                    return;

                _scannedQrCodesQueue.TryAdd(result.Text, true);
            }

            _ = Task.Factory.StartNew(async () =>
            {
                try
                {
                    switch (qrScanResult)
                    {
                        case DeviceQrScanResult deviceQrScanResult:
                            if (_sessionService.WasDevicePairedPreviously(deviceQrScanResult.Mac.ToString(), null))
                            {
                                var shouldPairAgain = await _dialogService.ConfirmAsync(Resources.Strings.existing_device_title,
                                    Resources.Strings.existing_paired_device_error,
                                    Resources.Strings.yes,
                                    Resources.Strings.cancel,
                                    _pairDeviceCancellationTokenSource.Token);
                                if (!shouldPairAgain)
                                {
                                    lock (_scannedQrCodesQueueLock)
                                    {
                                        _scannedQrCodesQueue.TryRemove(result.Text);
                                    }

                                    return;
                                }
                            }
                            var successfullyConnectedToDevice = await ConnectToDevice(deviceQrScanResult);
                            if (!successfullyConnectedToDevice)
                            {
                                lock (_scannedQrCodesQueueLock)
                                {
                                    _scannedQrCodesQueue.TryRemove(result.Text);
                                }
                            }
                            break;
                        case GatewayQrScanResult gatewayQrScanResult:
                            if (_sessionService.WasDevicePairedPreviously(null, gatewayQrScanResult.Name))
                            {
                                var shouldPairAgain = await _dialogService.ConfirmAsync(Resources.Strings.existing_device_title,
                                    Resources.Strings.existing_accessory_gateway_error,
                                    Resources.Strings.yes,
                                    Resources.Strings.cancel,
                                    _pairDeviceCancellationTokenSource.Token);
                                if (!shouldPairAgain)
                                {
                                    lock (_scannedQrCodesQueueLock)
                                    {
                                        _scannedQrCodesQueue.TryRemove(result.Text);
                                    }

                                    return;
                                }
                            }
                            await ConnectToRvAsync(gatewayQrScanResult, _pairDeviceCancellationTokenSource.Token);
                            break;
                        case ErrorQrScanResult errorQrScanResult:
                            TaggedLog.Warning(LogTag, $"QR code parser error {errorQrScanResult.Error}; QR Code: \"{result.Text}\"");
                            return;
                        default:
                            TaggedLog.Warning(LogTag, $"Unable to parse QR code; QR Code: \"{result.Text}\"");
                            return;
                    }
                }
                catch (Exception ex)
                {
                    var message = $"Unable to connect to the {deviceType}; QR Code: \"{result.Text}\"; Error: {ex.GetType()?.Name} {ex.Message}";
                    _sessionService.AddLogEntry(deviceType, SessionState.Error, message);
                    TaggedLog.Error(LogTag, message);

                    lock (_scannedQrCodesQueueLock)
                    {
                        _scannedQrCodesQueue.TryRemove(result.Text);
                    }
                }
            });
        }

        private async Task<bool> ConnectToDevice(DeviceQrScanResult deviceQrScanResult)
        {
            var deviceType = deviceQrScanResult.Type;
            _sessionService.AddLogEntry(deviceType, SessionState.Connecting);

            var cell = PairDevices.SingleOrDefault(x => x.DeviceType == deviceType);
            AccessoryConnectionResult connectionResult = null;
            switch (deviceType)
            {
                case DEVICE_TYPE.BATTERY_MONITOR when cell is PairDeviceCellModel batteryMonitorCell:
                    if (batteryMonitorCell.State != ConnectionState.NotSelected)
                    {
                        await _dialogService.AlertAsync(Strings.existing_device_title,
                            Strings.battery_monitor_already_scanned,
                            Strings.ok,
                            _pairDeviceCancellationTokenSource.Token);
                        return false;
                    }

                    batteryMonitorCell.State = ConnectionState.Connecting;
                    connectionResult = await ConnectToBatteryMonitorAsync(deviceQrScanResult, batteryMonitorCell, ConnectionStateUpdate, _pairDeviceCancellationTokenSource.Token);
                    batteryMonitorCell.Device = connectionResult.Device;
                    batteryMonitorCell.ConnectionResult = connectionResult;
                    break;
                case DEVICE_TYPE.AWNING_SENSOR when cell is PairDeviceCellModel awningSensorCell:
                    if (awningSensorCell.State != ConnectionState.NotSelected)
                    {
                        await _dialogService.AlertAsync(Strings.existing_device_title,
                            Strings.wind_sensor_already_scanned,
                            Strings.ok,
                            _pairDeviceCancellationTokenSource.Token);
                        return false;
                    }

                    awningSensorCell.State = ConnectionState.Connecting;
                    connectionResult = await ConnectToAwningAsync(deviceQrScanResult, awningSensorCell, ConnectionStateUpdate, _pairDeviceCancellationTokenSource.Token);
                    break;
                case DEVICE_TYPE.AWNING_SENSOR when cell is PairWindSensorCellModel multipleDevicesCell:
                    var currentlySelectedCellForAwning = multipleDevicesCell.SelectedDevice;
                    if (currentlySelectedCellForAwning == null)
                    {
                        // Make sure the first cell is selected
                        multipleDevicesCell.SetNextWindSensor();
                        currentlySelectedCellForAwning = multipleDevicesCell.SelectedDevice;
                    }

                    // Now select the next cell if there is any
                    multipleDevicesCell.SetNextWindSensor();
                    if (multipleDevicesCell.State != ConnectionState.NotSelected)
                    {
                        await _dialogService.AlertAsync(Strings.existing_device_title,
                            Strings.all_wind_sensors_already_scanned,
                            Strings.ok,
                            _pairDeviceCancellationTokenSource.Token);
                        return false;
                    }

                    currentlySelectedCellForAwning.State = ConnectionState.Connecting;
                    connectionResult = await ConnectToAwningAsync(deviceQrScanResult, currentlySelectedCellForAwning, ConnectionStateUpdate, _pairDeviceCancellationTokenSource.Token);
                    break;
                default:
                    TaggedLog.Warning(LogTag, $"Device type {deviceType} and {cell} is not supported");
                    break;
            }

            if (_pairDeviceCancellationTokenSource.IsCancellationRequested)
                return false;

            if (connectionResult == null)
            {
                _sessionService.AddLogEntry(deviceType, SessionState.Error);
                return false;
            }

            _sessionService.AddLogEntry(deviceType, connectionResult.IsError ? SessionState.Error : SessionState.Connected);
            return !connectionResult.IsError;
        }

        private static void ConnectionStateUpdate(ConnectionState state, IPairableDeviceCell? pairDeviceCell)
        {
            if (pairDeviceCell == null)
                return;

            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() => { pairDeviceCell.State = state; });
        }

        private async Task ResetSettingsAsync()
        {
            bool isResetSettings = await _dialogService.ConfirmAsync(Resources.Strings.reset_settings_title,
                Resources.Strings.reset_settings_message,
                Resources.Strings.yes,
                Resources.Strings.cancel,
                ResumePauseCancellationToken);

            if (!isResetSettings)
                return;

            _resetScreenCancellationTokenSource.CancelAndDispose();

            PairDevices = new List<IPairableDeviceCell>();
            await GetDataAsync();
        }

        #endregion

        #region Awning Sensor

        private async Task<AccessoryConnectionResult> ConnectToAwningAsync(DeviceQrScanResult qrScanResult, IPairableDeviceCell deviceCell, Action<ConnectionState, IPairableDeviceCell> progressCallback, CancellationToken cancellationToken)
        {
            var currentConnectionState = ConnectionState.Connecting;

            void UpdateCellWithStatus(ConnectionState state, bool resetCurrentState = true)
            {
                progressCallback.Invoke(state, deviceCell);

                if (resetCurrentState)
                    currentConnectionState = state;
            }

            UpdateCellWithStatus(ConnectionState.Connecting);

            IdsCanAccessoryScanResult? idsAccessoryScanResult = null;
            var isWaitingToGetDeviceFromScan = new Func<bool>(() => idsAccessoryScanResult == null && !cancellationToken.IsCancellationRequested);
            while (isWaitingToGetDeviceFromScan())
            {
                idsAccessoryScanResult = await GetDeviceForQrCode(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken);
                if (isWaitingToGetDeviceFromScan())
                {
                    UpdateCellWithStatus(ConnectionState.Error, false);
                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            TaggedLog.Information(LogTag, $"Found Awning Sensor `{idsAccessoryScanResult!.DeviceName}`");
            UpdateCellWithStatus(ConnectionState.Connected);

            var success = false;
            var accessoryGateway = await GetAccessoryGateway(cancellationToken, async () =>
            {
                if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                    UpdateCellWithStatus(ConnectionState.Error, false);
                else
                    UpdateCellWithStatus(currentConnectionState);
            });

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            UpdateCellWithStatus(ConnectionState.Pairing);
            // This wait is necessary to update the UI
            await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);

            // If our awning sensor is already registered we're done, otherwise we register it.
            if (!_directAwningSensorBle.IsAwningSensorRegistered(idsAccessoryScanResult.DeviceId))
            {
                _directAwningSensorBle.RegisterAwningSensor(idsAccessoryScanResult.DeviceId, qrScanResult.Mac, idsAccessoryScanResult.SoftwarePartNumber ?? string.Empty,
                    idsAccessoryScanResult.DeviceName);

                var linkVerify = BleDeviceKeySeedExchangeResult.Unsupported;
                var isWaitingToSucceed = new Func<bool>(() => linkVerify != BleDeviceKeySeedExchangeResult.Succeeded && !cancellationToken.IsCancellationRequested);
                while (isWaitingToSucceed())
                {
                    linkVerify = await idsAccessoryScanResult.TryLinkVerificationAsync(requireLinkMode: false, cancellationToken: cancellationToken);
                    if (isWaitingToSucceed())
                    {
                        if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                            UpdateCellWithStatus(ConnectionState.Error, false);
                        else
                            UpdateCellWithStatus(currentConnectionState);

                        await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return new AccessoryConnectionResult(ConnectionStep.Connecting);
            }

            object? logicalDevice = null;
            ILogicalDeviceAwningSensor? awningSensor = null;
            var isWaitingForLogicalDevice = new Func<bool>(() => logicalDevice is not ILogicalDeviceAwningSensor && !cancellationToken.IsCancellationRequested);
            while (isWaitingForLogicalDevice())
            {
                logicalDevice = _logicalDeviceService.DeviceManager?.FindLogicalDevice(foundDevice => foundDevice.LogicalId.ProductMacAddress == idsAccessoryScanResult.AccessoryMacAddress);
                if (logicalDevice is ILogicalDeviceAwningSensor foundSensor)
                {
                    awningSensor = foundSensor;
                    break;
                }

                if (isWaitingForLogicalDevice())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            var isPaired = false;
            var isWaitingToPairWithRv = new Func<bool>(() => !isPaired && !cancellationToken.IsCancellationRequested);
            while (isWaitingToPairWithRv())
            {
                isPaired = await _accessoryGatewayPairingService.PairWithRv(awningSensor, accessoryGateway, cancellationToken);
                if (isWaitingToPairWithRv())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            UpdateCellWithStatus(ConnectionState.Verifying);

            var functionName = deviceCell.FunctionName ?? FUNCTION_NAME.UNKNOWN;
            if (functionName == FUNCTION_NAME.UNKNOWN)
            {
                // This is a genuine error that we can't automatically recover from. Therefore the function has to exit and the user will have to scan QR code again
                UpdateCellWithStatus(ConnectionState.Error);
                TaggedLog.Debug(LogTag, "Awning Sensor Connection Error: could not find matching function name for awning sensor.");
                return new AccessoryConnectionResult(ConnectionStep.Connecting);
            }

            object? awningLogicalDevice = null;
            ILogicalDeviceRelayHBridgeWithAssociatedAwningSensor? awningRelay = null;
            var isWaitingForHBridge = new Func<bool>(() => awningLogicalDevice is not ILogicalDeviceRelayHBridgeWithAssociatedAwningSensor && !cancellationToken.IsCancellationRequested);
            while (isWaitingForHBridge())
            {
                awningLogicalDevice = _logicalDeviceService.DeviceManager?.FindLogicalDevice(foundDevice => foundDevice.LogicalId.FunctionName == functionName);
                if (awningLogicalDevice is ILogicalDeviceRelayHBridgeWithAssociatedAwningSensor foundLogicalDevice)
                {
                    awningRelay = foundLogicalDevice;
                    break;
                }

                if (isWaitingForHBridge())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            await awningRelay!.TrySetAssociatedCircuitIdRoleAsync(LogicalDeviceRelayHBridgeCircuitIdRole.AwningSensor, cancellationToken);
            var circuitRole = LogicalDeviceRelayHBridgeCircuitIdRole.None;
            var isWaitingToGetAssociatedCircuitIdRole = new Func<bool>(() => circuitRole != LogicalDeviceRelayHBridgeCircuitIdRole.AwningSensor && !cancellationToken.IsCancellationRequested);
            while (isWaitingToGetAssociatedCircuitIdRole())
            {
                circuitRole = await awningRelay.TryGetAssociatedCircuitIdRoleAsync(cancellationToken);

                if (isWaitingToGetAssociatedCircuitIdRole())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            success = false;
            var isWaitingToLinkWithAwningRelay = new Func<bool>(() => !success && !cancellationToken.IsCancellationRequested);
            while (isWaitingToLinkWithAwningRelay())
            {
                try
                {
                    await awningSensor!.LinkWithAwningRelayAsync(awningRelay, cancellationToken);
                    success = true;
                    break;
                }
                catch (Exception e)
                {
                    TaggedLog.Debug(LogTag, $"Problem setting circuit id: {e} {e.StackTrace}. Will try again.");
                }

                if (isWaitingToLinkWithAwningRelay())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.AWNING_SENSOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            TaggedLog.Information(LogTag, $"Awning Sensor successfully paired.");
            _sessionService.RecordDevicePaired(DEVICE_TYPE.AWNING_SENSOR, qrScanResult.Mac.ToString(), awningSensor.DeviceName);
            UpdateCellWithStatus(ConnectionState.Verified);

            return new AccessoryConnectionResult(awningSensor);
        }

        #endregion

        #region Helper Methods

        private async Task GetDataAsync()
        {
            if (SelectedFloorPlan == null) return;

            var pairableDevices = _bundledDataService.GetDevicesForFloorPlan(SelectedFloorPlan);
            var pairDeviceCells = pairableDevices.Select(d => new PairDeviceCellModel(_navigationService, _dialogService)
            {
                DeviceType = d.DeviceType,
                DeviceName = d.FriendlyName ?? GetDeviceFriendlyName(d.DeviceType),
                FunctionName = d.FunctionName,
                State = ConnectionState.NotSelected
            }).Cast<IPairableDeviceCell>().ToList();

            var windSensors = pairDeviceCells.Where(x => x.DeviceType == DEVICE_TYPE.AWNING_SENSOR).ToList();
            if (windSensors.Count > 1)
            {
                pairDeviceCells.RemoveAll(x => x.DeviceType == DEVICE_TYPE.AWNING_SENSOR);
                pairDeviceCells.Insert(0, new PairWindSensorCellModel(_navigationService, _sessionService, windSensors));
            }

            _resetScreenCancellationTokenSource = new CancellationTokenSource();
            _pairDeviceCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ResumePauseCancellationToken, _resetScreenCancellationTokenSource.Token);

            StopAllServices();

            PairDevices = new List<IPairableDeviceCell>(pairDeviceCells);
            lock (_scannedQrCodesQueueLock)
            {
                _scannedQrCodesQueue.Clear();
            }

            _logicalDeviceService.Start();

            await Task.FromResult(0);
        }

        private void StopAllServices()
        {
            _logicalDeviceService.DeviceManager?.RemoveAllLogicalDevices();
            _logicalDeviceService.Stop();
            AppDirectConnectionService.Instance.Stop();
            AppDirectServices.Instance.Stop();
            AppSettings.Instance.SetSelectedRvGatewayConnection(null, false);
        }

        private string GetDeviceFriendlyName(DEVICE_TYPE deviceType)
        {
            switch (deviceType)
            {
                case DEVICE_TYPE.BLUETOOTH_GATEWAY:
                    return "RV";
                default:
                    return deviceType.Name;
            }
        }

        #endregion

        #region BatteryMonitor

        private async Task<AccessoryConnectionResult> ConnectToBatteryMonitorAsync(DeviceQrScanResult qrScanResult, PairDeviceCellModel deviceCell, Action<ConnectionState, PairDeviceCellModel> progressCallback, CancellationToken cancellationToken)
        {
            var currentConnectionState = ConnectionState.Connecting;

            void UpdateCellWithStatus(ConnectionState state, bool resetCurrentState = true)
            {
                progressCallback.Invoke(state, deviceCell);

                if (resetCurrentState)
                    currentConnectionState = state;
            }

            UpdateCellWithStatus(ConnectionState.Connecting);

            IdsCanAccessoryScanResult? idsAccessoryScanResult = null;
            var isWaitingToGetDeviceFromScan = new Func<bool>(() => idsAccessoryScanResult == null && !cancellationToken.IsCancellationRequested);
            while (isWaitingToGetDeviceFromScan())
            {
                idsAccessoryScanResult = await GetDeviceForQrCode(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken);
                if (isWaitingToGetDeviceFromScan())
                {
                    UpdateCellWithStatus(ConnectionState.Error, false);
                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            TaggedLog.Information(LogTag, $"Found Battery Monitor `{idsAccessoryScanResult.DeviceName}`");
            UpdateCellWithStatus(ConnectionState.Connected);

            var success = false;
            var accessoryGateway = await GetAccessoryGateway(cancellationToken, async () =>
            {
                if (await IsDeviceDiscoverable(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken) == false)
                    UpdateCellWithStatus(ConnectionState.Error, false);
                else
                    UpdateCellWithStatus(currentConnectionState);
            });

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            UpdateCellWithStatus(ConnectionState.Pairing);
            // This wait is necessary to update the UI
            await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);

            // Check if we need to remove any old battery monitors
            var oldBatteryMonitors = _logicalDeviceService.DeviceManager?.FindLogicalDevices<ILogicalDeviceBatteryMonitor>(logicalDevice => logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline);
            if (oldBatteryMonitors != null && oldBatteryMonitors.Any())
            {
                var isRemoveOldBatteryMonitor = await _dialogService.ConfirmAsync(Resources.Strings.existing_device_title,
                    Resources.Strings.existing_battery_monitor_error,
                    Resources.Strings.yes,
                    Resources.Strings.cancel,
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    return new AccessoryConnectionResult(ConnectionStep.Connecting);

                if (isRemoveOldBatteryMonitor)
                {
                    success = false;

                    var isWaitingToSucceed = new Func<bool>(() => !success && !cancellationToken.IsCancellationRequested);
                    while (isWaitingToSucceed())
                    {
                        success = await RemoveOldBatteryMonitors(oldBatteryMonitors, accessoryGateway, cancellationToken);
                        if (isWaitingToSucceed())
                        {
                            if (await IsDeviceDiscoverable(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken) == false)
                                UpdateCellWithStatus(ConnectionState.Error, false);
                            else
                                UpdateCellWithStatus(currentConnectionState);

                            await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        return new AccessoryConnectionResult(ConnectionStep.Connecting);
                }
            }

            // If our battery monitor isn't already registered we need to register it.
            if (!_directBatteryMonitorBle.IsBatteryMonitorRegistered(idsAccessoryScanResult.DeviceId))
            {
                success = false;
                var isWaitingToSucceed = new Func<bool>(() => !success && !cancellationToken.IsCancellationRequested);
                while (isWaitingToSucceed())
                {
                    success = AppSettings.Instance.TryAddSensorConnection(new SensorConnectionBatteryMonitor(idsAccessoryScanResult.DeviceName, idsAccessoryScanResult.DeviceId, qrScanResult.Mac, idsAccessoryScanResult.SoftwarePartNumber), requestSave: true);
                    if (isWaitingToSucceed())
                    {
                        if (await IsDeviceDiscoverable(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken) == false)
                            UpdateCellWithStatus(ConnectionState.Error, false);
                        else
                            UpdateCellWithStatus(currentConnectionState);

                        await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return new AccessoryConnectionResult(ConnectionStep.Connecting);
            }

            // Link to accessory gateway
            success = false;
            var isWaitingToLinkToAccessoryGateway = new Func<bool>(() => !success && !cancellationToken.IsCancellationRequested);
            while (isWaitingToLinkToAccessoryGateway())
            {
                success = await LinkToAccessoryGateway(accessoryGateway, qrScanResult.Mac, cancellationToken);
                if (isWaitingToLinkToAccessoryGateway())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            UpdateCellWithStatus(ConnectionState.Verifying);
            // This wait is necessary to update the UI
            await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);

            ILogicalDevice? newBatteryMonitor = null;
            var isWaitingForLogicalDevice = new Func<bool>(() => newBatteryMonitor == null && !cancellationToken.IsCancellationRequested);
            while (isWaitingToLinkToAccessoryGateway())
            {
                newBatteryMonitor = _logicalDeviceService.DeviceManager?.FindLogicalDevice(logicalDevice => logicalDevice.LogicalId.ProductMacAddress == qrScanResult.Mac);
                if (isWaitingForLogicalDevice())
                {
                    if (await IsDeviceDiscoverable(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult, cancellationToken) == false)
                        UpdateCellWithStatus(ConnectionState.Error, false);
                    else
                        UpdateCellWithStatus(currentConnectionState);

                    await TaskExtension.TryDelay(LoopOperationsDelayMs, cancellationToken);
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return new AccessoryConnectionResult(ConnectionStep.Connecting);

            UpdateCellWithStatus(ConnectionState.Verified);
            _sessionService.RecordDevicePaired(DEVICE_TYPE.BATTERY_MONITOR, qrScanResult.Mac.ToString(), null);
            return new AccessoryConnectionResult(newBatteryMonitor);
        }

        private async Task<bool> IsDeviceDiscoverable(DEVICE_TYPE deviceType, DeviceQrScanResult qrScanResult, CancellationToken cancellationToken)
        {
            var idsAccessoryScanResult = await GetDeviceForQrCode(deviceType, qrScanResult, cancellationToken);
            return idsAccessoryScanResult != null;
        }

        private async Task<IdsCanAccessoryScanResult?> GetDeviceForQrCode(DEVICE_TYPE deviceType, DeviceQrScanResult qrScanResult, CancellationToken cancellationToken)
        {
            IdsCanAccessoryScanResult? idsAccessoryScanResult = null;
            await BleScannerService.Instance.TryGetDevicesAsync<IdsCanAccessoryScanResult>(
                (_, scanResult) => idsAccessoryScanResult = scanResult,
                scanResult =>
                {
                    switch (scanResult.HasRequiredAdvertisements)
                    {
                        case BleRequiredAdvertisements.AllExist:
                            if (!scanResult.HasDeviceName || !scanResult.DeviceName.StartsWith(BatteryMonitorBle.DeviceNamePrefix))
                            {
                                TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as looking for names starting with {BatteryMonitorBle.DeviceNamePrefix}");
                                return BleScannerCommandControl.Skip;
                            }

                            // Try to parse the status to see if we found the right device
                            var accessoryStatus = scanResult.GetAccessoryStatus(qrScanResult.Mac);
                            if (accessoryStatus is null)
                            {
                                TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as missing accessory status");
                                return BleScannerCommandControl.Skip;
                            }

                            if (accessoryStatus.Value.DeviceType != deviceType)
                            {
                                TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as device isn't a {deviceType}: {accessoryStatus.Value.DeviceType}");
                                return BleScannerCommandControl.Skip;
                            }

                            return BleScannerCommandControl.IncludeAndFinish;

                        case BleRequiredAdvertisements.Unknown:
                        case BleRequiredAdvertisements.SomeExist:
                        case BleRequiredAdvertisements.NoneExist:
                        default:
                            return BleScannerCommandControl.Skip;
                    }
                }, cancellationToken);

            return idsAccessoryScanResult;
        }

        private async Task<bool> LinkToAccessoryGateway(ILogicalDeviceAccessoryGateway accessoryGateway, MAC accessoryMacAddress, CancellationToken cancellationToken)
        {
            var linkResult = await accessoryGateway.LinkDeviceAsync(accessoryMacAddress, cancellationToken);
            return linkResult == CommandResult.Completed;
        }

        private async Task<bool> RemoveOldBatteryMonitors(List<ILogicalDeviceBatteryMonitor>? oldBatteryMonitors,
            ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken cancellationToken)
        {
            // If we're not in a repair flow or we don't have any old devices we don't need to remove anything, so we're good.
            if (oldBatteryMonitors is null || oldBatteryMonitors.Count == 0)
                return true;

            var resyncSuccess = true;
            foreach (var oldBatteryMonitor in oldBatteryMonitors)
            {
                // Make sure we try to remove the old battery monitor from the accessory gateway.
                if (accessoryGateway is not null)
                {
                    var linked = await accessoryGateway.IsDeviceLinkedAsync(oldBatteryMonitor.LogicalId.ProductMacAddress, cancellationToken);
                    if (linked)
                    {
                        resyncSuccess = await _accessoryGatewayPairingService?.UnpairWithRv(oldBatteryMonitor, accessoryGateway, cancellationToken);
                        if (!resyncSuccess)
                            return resyncSuccess;
                    }
                }

                // If the battery monitor is still linked to our device we need to remove the sensor connection.
                var sensorConnection = AppSettings.Instance.SensorConnections<SensorConnectionBatteryMonitor>().FirstOrDefault(
                    (sensor) => sensor.AccessoryMac == oldBatteryMonitor.LogicalId.ProductMacAddress);
                if (sensorConnection is not null)
                {
                    resyncSuccess = AppSettings.Instance.TryRemoveSensorConnection(sensorConnection, requestSave: true);
                    if (!resyncSuccess)
                        return resyncSuccess;
                }
            }

            var directManagers = _logicalDeviceService.DeviceSourceManager.FindDeviceSources<ILogicalDeviceSourceDirectRemoveOfflineDevices>((ds) => ds.IsDeviceSourceActive);
            foreach (var directManager in directManagers)
            {
                try
                {
                    // This will send a message to RvLink to remove offline devices.
                    TaggedLog.Debug(LogTag, $"RemoveOfflineDevices from {directManager.GetType()} {directManager}");
                    await directManager.RemoveOfflineDevicesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    TaggedLog.Debug(LogTag, $"Unable to Remove Offline Devices From {directManager.GetType()} {directManager}: {ex.Message}");
                }
            }

            // Last we can completely remove the logical device. This will remove all the devices that match the filter.
            _logicalDeviceService.DeviceManager?.RemoveLogicalDevice((logicalDevice) =>
                logicalDevice is ILogicalDeviceBatteryMonitor && logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline);

            return resyncSuccess;
        }

        #endregion

        #region RvLink

        private async Task ConnectToRvAsync(GatewayQrScanResult qrScanResult, CancellationToken cancellationToken)
        {
            var deviceType = DEVICE_TYPE.BLUETOOTH_GATEWAY;
            if (PairDevices.SingleOrDefault(x => x.DeviceType == deviceType) is PairDeviceCellModel cell)
            {
                cell.ConnectionResult = new AccessoryConnectionResult(ConnectionStep.Connecting);
                ConnectionStateUpdate(ConnectionState.Connecting, cell);

                RvGatewayMyRvLinkConnectionBle? gatewayConnection = null;
                var isWaitingForGatewayConnection = new Func<bool>(() => gatewayConnection == null && !cancellationToken.IsCancellationRequested);
                while (isWaitingForGatewayConnection())
                {
                    gatewayConnection = await ConnectToRvAsync(qrScanResult, cell, ConnectionStateUpdate, cancellationToken);
                    if (isWaitingForGatewayConnection())
                        await Task.Delay(LoopOperationsDelayMs, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                _sessionService.RecordDevicePaired(DEVICE_TYPE.ACCESSORY_GATEWAY, null, qrScanResult.Name);
                ConnectionStateUpdate(ConnectionState.Connected, cell);
                cell.ConnectionResult = new AccessoryConnectionResult(ConnectionStep.Pairing);
                _sessionService.AddLogEntry(deviceType, SessionState.Connected);
            }
        }

        private async Task<RvGatewayMyRvLinkConnectionBle?> ConnectToRvAsync(GatewayQrScanResult qrScanResult, PairDeviceCellModel deviceCell, Action<ConnectionState, PairDeviceCellModel> progressCallBack, CancellationToken cancellationToken)
        {
            var waitForConnectionTimer = Stopwatch.StartNew();
            IBleScanResult? bleScanResult = null;
            var isWaitingForDevice = new Func<bool>(() => bleScanResult == null && !cancellationToken.IsCancellationRequested && waitForConnectionTimer.ElapsedMilliseconds < MaxRvConnectionWaitTimeMs);
            while (isWaitingForDevice())
            {
                bleScanResult = await BleScannerService.Instance.TryGetDeviceAsync<IBleScanResult>(qrScanResult.Name, cancellationToken);
                if (isWaitingForDevice())
                    await Task.Delay(LoopOperationsDelayMs, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return null;

            if (bleScanResult == null)
            {
                progressCallBack.Invoke(ConnectionState.Error, deviceCell);
                return null;
            }

            var connection = new RvGatewayMyRvLinkConnectionBle(bleScanResult.DeviceId, bleScanResult.DeviceName);
            AppSettings.Instance.SetSelectedRvGatewayConnection(connection, false);

            waitForConnectionTimer = Stopwatch.StartNew();
            while (waitForConnectionTimer.ElapsedMilliseconds < MaxRvConnectionWaitTimeMs &&
                   AppDirectConnectionService.Instance.RvConnectionStatus != ConnectionManagerStatus.Connected &&
                   !cancellationToken.IsCancellationRequested)
            {
                await TaskExtension.TryDelay(CheckForConnectionCompletedMs, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return null;

            if (AppDirectConnectionService.Instance.RvConnectionStatus != ConnectionManagerStatus.Connected)
            {
                progressCallBack.Invoke(ConnectionState.Error, deviceCell);
                return null;
            }

            return connection;
        }

        private async Task<ILogicalDeviceAccessoryGateway?> GetAccessoryGateway(CancellationToken cancellationToken, Action checkIfDeviceStillDiscoverable)
        {
            PairDeviceCellModel? rvCell = null;
            if (PairDevices.SingleOrDefault(x => x.DeviceType == DEVICE_TYPE.BLUETOOTH_GATEWAY) is PairDeviceCellModel cell)
            {
                rvCell = cell;
            }
            
            void UpdateRvCellConnectionState(ConnectionState state)
            {
                if (rvCell?.State != ConnectionState.Selected && rvCell?.ConnectionResult?.Step == ConnectionStep.Pairing)
                    ConnectionStateUpdate(state, rvCell);
            }

            List<ILogicalDeviceAccessoryGateway>? accessoryGateways = null;
            var waitForConnectionTimer = Stopwatch.StartNew();

            var isWaitingForGateway = new Func<bool>(() => (accessoryGateways == null || !accessoryGateways.Any()) && !cancellationToken.IsCancellationRequested);
            while (isWaitingForGateway())
            {
                accessoryGateways = _logicalDeviceService.DeviceManager?.FindLogicalDevices<ILogicalDeviceAccessoryGateway>(logicalDevice => logicalDevice.ActiveConnection != LogicalDeviceActiveConnection.Offline && AppCollectionSyncContainer.FilterForSelectedRv(logicalDevice, SelectedRvDeviceOptions.AllDevices))
                    .OrderBy(it => it.Product?.MacAddress)
                    .ToList();

                if (cancellationToken.IsCancellationRequested)
                    break;

                if (isWaitingForGateway())
                {
                    if (rvCell?.ConnectionResult?.Step != ConnectionStep.Pairing)
                        waitForConnectionTimer.Restart();
                    
                    if (waitForConnectionTimer.ElapsedMilliseconds > MaxGetAccessoryGatewayWaitTimeBeforeShowingErrorMs)
                        UpdateRvCellConnectionState(ConnectionState.Error);

                    checkIfDeviceStillDiscoverable.Invoke();
                    await Task.Delay(CheckForGatewayAvailableDelayMs, cancellationToken);
                }
            }

            UpdateRvCellConnectionState(ConnectionState.Connected);
            return accessoryGateways?.FirstOrDefault();
        }

        #endregion
    }
}