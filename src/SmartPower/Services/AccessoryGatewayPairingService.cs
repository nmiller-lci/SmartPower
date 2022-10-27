using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.LogicalDevice;
using OneControl.Devices.AccessoryGateway;

namespace SmartPower.Services
{
    public interface IAccessoryGatewayPairingService
    {
        Task<bool> IsPairedWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);
        Task<bool> IsPairedOverBle(ILogicalDeviceAccessory? device, CancellationToken token);
        Task<bool> PairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);
        Task<bool> UnpairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token);

        /// <summary>
        /// The purpose of this function is to keep the app in-sync with other apps (including OCTP)
        /// that are connected to the accessory gateway.
        /// This function removes logical device source associations for accessories that are no longer
        /// linked with the accessory gateway.
        /// </summary>
        Task ResyncAccessoryGatewayDevices(CancellationToken token);
    }

    public class AccessoryGatewayPairingService : IAccessoryGatewayPairingService
    {
        public static readonly string LogTag = nameof(AccessoryGatewayPairingService);
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        // Wait long enough for accessory to be seen by accessory gateway and reported over IDS-CAN.  Some accessories such as
        // temperature sensor can be really slow to connect > 1 minute
        //
        public static readonly TimeSpan PairWithRvTimeout = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan UnpairWithRvTimeout = TimeSpan.FromSeconds(90);

        private readonly ILogicalDeviceManager _logicalDeviceManager;
        private readonly AppDirectServices _appDirectServices;

        public AccessoryGatewayPairingService(
            ILogicalDeviceManager logicalDeviceManager,
            AppDirectServices appDirectServices)
        {
            _logicalDeviceManager = logicalDeviceManager;
            _appDirectServices = appDirectServices;
        }

        public async Task<bool> IsPairedWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
        {
            if (device?.Product?.MacAddress is null)
                return false;

            if (!device.IsAccessoryGatewaySupported)
                return false;

            if (accessoryGateway is null)
                return false;

            // If the accessory is associated with any of the device sources associated with the Accessory
            // Gateway, then we know that the accessory is accessible over IDS-CAN or RvLink.
            //
            foreach (var targetSource in _logicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
            {
                if (device.IsAssociatedWithDeviceSource(targetSource) &&
                    accessoryGateway.IsAssociatedWithDeviceSource(targetSource))
                {
                    if (accessoryGateway.ActiveConnection != LogicalDeviceActiveConnection.Offline)
                    {
                        // If we are online, we should check if the MAC is still paired.
                        if (!await accessoryGateway.IsDeviceLinkedAsync(device.Product.MacAddress, token))
                        {
                            // Sensor is unlinked with the gateway, so we should update the database.
                            device.RemoveDeviceSource(targetSource);

                            // Persist device source change.
                            _appDirectServices.TakeSnapshot();
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // If we are offline, we just assume we are still paired. Syncing will occur next time it comes online.
                        return true;
                    }
                }
            }

            return false;
        }

        public Task<bool> IsPairedOverBle(ILogicalDeviceAccessory? device, CancellationToken token)
        {
            if (device is null || !device.IsAccessoryGatewaySupported)
                return Task.FromResult(false);

            foreach (var deviceSource in _logicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
            {
                switch (deviceSource)
                {
                    // Supports any of our accessories device sources such as IDirectTemperatureSensorBle and IDirectBatteryMonitorBle
                    //
                    case ILogicalDeviceSourceDirectIdsAccessory accessoryDeviceSource:
                        if (device.IsAssociatedWithDeviceSource(accessoryDeviceSource))
                            return Task.FromResult(true);
                        break;
                }
            }

            return Task.FromResult(false);
        }

        public async Task<bool> PairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
        {
            if (device?.Product?.MacAddress == null)
                return false;

            if (accessoryGateway is null)
                return false;

            if (!device.IsAccessoryGatewaySupported)
                return false;

            var isLinked = (await accessoryGateway.LinkDeviceAsync(device.Product.MacAddress, token)) == CommandResult.Completed;
            if (!isLinked)
                return false; // There's no point in waiting for the device to appear over IDS-CAN if linking failed.

            try
            {
                var isPaired = false;
                await Task.WhenAny(Task.Delay(PairWithRvTimeout, token), Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested && !isPaired)
                    {
                        await Task.Delay(250, token);
                        isPaired = await IsPairedWithRv(device, accessoryGateway, token);
                    }

                    // Persist device source change.
                    if (isPaired)
                        _appDirectServices.TakeSnapshot();

                }, token));

                return isPaired;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UnpairWithRv(ILogicalDeviceAccessory? device, ILogicalDeviceAccessoryGateway? accessoryGateway, CancellationToken token)
        {
            if (device?.Product?.MacAddress == null)
                throw new ArgumentException($"Accessory must have a MAC address {device}");

            if (accessoryGateway is null)
                return false;

            if (!device.IsAccessoryGatewaySupported)
                return false;

            var isUnlinked = (await accessoryGateway.UnlinkDeviceAsync(device.Product.MacAddress, token)) == CommandResult.Completed;
            if (!isUnlinked)
                return false; // There's no point in waiting for the device to appear over IDS-CAN if linking failed.

            // Find the device source in common with this sensor and the accessory gateway and remove it from the sensor.
            foreach (var targetSource in _logicalDeviceManager.DeviceService.DeviceSourceManager.DeviceSources)
            {
                if (device.IsAssociatedWithDeviceSource(targetSource) && accessoryGateway.IsAssociatedWithDeviceSource(targetSource))
                {
                    device.RemoveDeviceSource(targetSource);
                    break;
                }
            }

            // Persist device source change.
            _appDirectServices.TakeSnapshot();

            return true;
        }

        public async Task ResyncAccessoryGatewayDevices(CancellationToken token)
        {
            try
            {
                await _lock.WaitAsync(token);

                // We are assuming that there will never be more than one accessory gateway at a time.
                // If there is, then we need to re-design how syncing OCM + OCTP(s) works.
                var accessoryGateways = _logicalDeviceManager.FindLogicalDevices<ILogicalDeviceAccessoryGateway>(
                    it => it.ActiveConnection != LogicalDeviceActiveConnection.Offline && AppCollectionSyncContainer.FilterForSelectedRv(it, SelectedRvDeviceOptions.AllDevices))
                    .OrderBy(it => it.Product?.MacAddress).ToList();

                if (accessoryGateways.Count > 1)
                {
                    throw new Exception("Found more than 1 accessory gateway on the current RV connection. We do not support multiple accessory gateways");
                }

                var accessoryGateway = accessoryGateways.FirstOrDefault();
                if (accessoryGateway != null && await accessoryGateway.ResyncDevicesAsync(token))
                {
                    // If a device was dissociated with the accessory gateway's source, we should persist the change.
                    _appDirectServices.TakeSnapshot();
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
