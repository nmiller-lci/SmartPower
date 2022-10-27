using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.UI.UserDialogs;
using OneControl.Devices.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.BatteryMonitor;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using SmartPower.Resources;
using SmartPower.Services;
using SmartPower.UserInterface.Settings;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace SmartPower.UserInterface.Pages;

public class MainPageViewModel : BaseViewModel, IViewModelResumePause
{
    private readonly ISessionService _sessionService;
    private readonly IUserDialogService _userDialogService;
    private readonly IBundledDataService _bundledDataService;

    private const string LogTag = nameof(MainPageViewModel);

    public MainPageViewModel(INavigationService navigationService, ISessionService sessionService, IBundledDataService bundledDataService, IUserDialogService userDialogService) : base(navigationService)
    {
        _sessionService = sessionService;
        _bundledDataService = bundledDataService;
        _userDialogService = userDialogService;
    }

    public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
    {
        var permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        var HasLocationPermissions = (permissionStatus == PermissionStatus.Granted);
    }

    public void OnPause(PauseReason reason) { }

    private AsyncCommand? _gotoSettingsCommand;
    public AsyncCommand GoToSettingsCommand => _gotoSettingsCommand ??= new AsyncCommand(async () =>
        await NavigationService.NavigateAsync(nameof(SettingsPageViewModel)));

    private async Task<bool> RegisterBatteryMonitor(MAC accessoryMacAddress, CancellationToken cancellationToken)
    {
        // We can't tell if we have the right device until we decode it with the mac address from the user, so we just
        // take the first IdsCanAccessoryScan result with all the required advertisements and finish.
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
                        var accessoryStatus = scanResult.GetAccessoryStatus(accessoryMacAddress);
                        if (accessoryStatus is null)
                        {
                            TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as missing accessory status");
                            return BleScannerCommandControl.Skip;
                        }

                        if (accessoryStatus.Value.DeviceType != DEVICE_TYPE.BATTERY_MONITOR)
                        {
                            TaggedLog.Information(LogTag, $"Skipping device with name `{scanResult.DeviceName}` as device isn't a {DEVICE_TYPE.BATTERY_MONITOR}: {accessoryStatus.Value.DeviceType}");
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


        // Report a problem if we weren't able to find the sensor.
        //
        if (idsAccessoryScanResult == null)
        {
            TaggedLog.Information(LogTag, $"Could not find the battery monitor with a scan.");
            return false;
        }

        TaggedLog.Information(LogTag, $"Found Battery Monitor `{idsAccessoryScanResult.DeviceName}`");

        var directBatteryMonitorBle = Resolver<IDirectBatteryMonitorBle>.Resolve;
        if (directBatteryMonitorBle == null)
        {
            return false;
        }

        if (directBatteryMonitorBle.IsBatteryMonitorRegistered(idsAccessoryScanResult.DeviceId)) 
            return true;

        // If our battery monitor isn't already registered we need to register it.
        directBatteryMonitorBle.RegisterBatteryMonitor(idsAccessoryScanResult.DeviceId, accessoryMacAddress, 
            idsAccessoryScanResult.SoftwarePartNumber ?? string.Empty, idsAccessoryScanResult.DeviceName);

        var linkVerify = await idsAccessoryScanResult.TryLinkVerificationAsync(requireLinkMode: false, cancellationToken: cancellationToken);
        if (linkVerify != BleDeviceKeySeedExchangeResult.Succeeded)
        {
            TaggedLog.Error(LogTag, $"Link verification failed, we can't verify the link connection to the battery monitor.");
            return false;
        }

        var sensorConnection = new SensorConnectionBatteryMonitor(idsAccessoryScanResult.DeviceName, idsAccessoryScanResult.DeviceId, accessoryMacAddress, idsAccessoryScanResult.SoftwarePartNumber);
        AppSettings.Instance.AccessoryRegistration.TryAddSensorConnection(sensorConnection, requestSave: true);
        return true;

    }

}