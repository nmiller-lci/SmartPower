using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.BLE.Platforms.Shared.BleScanner;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using SmartPower;
using SmartPower.Services;
using OneControl.Devices;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using Exception = System.Exception;

namespace OneControl.UserInterface.AddAndManageDevices.LiquidPropane.Services
{
    public interface ILPSensorPairingService
    {
        /// <summary>
        /// Pairs the next liquid propane sensor found that is not already paired.
        /// If the operation cannot be completed due to not having access to external
        /// resources (e.g. bluetooth is unavailable), then the operation will fault.
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task<ILogicalDeviceTankSensor> Pair(CancellationToken cancelToken);

        Task Unpair(ILogicalDeviceTankSensor device);

        Task<bool> IsPairedOverBle(ILogicalDeviceTankSensor device);
    }

    public class LPSensorPairingService : ILPSensorPairingService
    {
        private readonly object ScanLock = new();

        private readonly IDeviceSettingsService _deviceSettingsService;
        private readonly AppDirectServices _appDirectServices;
        private const string LogTag = nameof(LPSensorPairingService);

        private readonly BleScannerService _bleScannerService;
        private readonly IMopekaBleDeviceSource _deviceSource;
        private readonly ILPSettingsRepository _lpSettingsRepository;

        public LPSensorPairingService(
            IDeviceSettingsService deviceSettingsService,
            AppDirectServices appDirectServices,
            IMopekaBleDeviceSource mopekaBleDeviceSource,
            ILPSettingsRepository lpSettingsRepository)
        {
            _deviceSettingsService = deviceSettingsService;
            _appDirectServices = appDirectServices;
            _bleScannerService = BleScannerService.Instance;
            _deviceSource = mopekaBleDeviceSource;
            _lpSettingsRepository = lpSettingsRepository;
        }

        public async Task<ILogicalDeviceTankSensor> Pair(CancellationToken cancelToken)
        {
            var tcs = new TaskCompletionSource<ILogicalDeviceTankSensor>();

            Action<IBleScanResult> scanAction = scanResult =>
            {
                lock (ScanLock)
                {
                    if (!(scanResult is MopekaScanResult { IsSyncPressed: true } mopekaScanResult))
                        return;

                    if (_deviceSource == null || _deviceSource.IsMopekaSensorLinked(mopekaScanResult.ShortMAC))
                        return;

                    if (cancelToken.IsCancellationRequested)
                        return;

                    var sensorConnection = new SensorConnectionMopeka(scanResult.DeviceName,
                        scanResult.DeviceId, mopekaScanResult.ShortMAC,
                        LPTankName.Rv1.FunctionName,
                        LPTankName.Rv1.FunctionInstance,
                        LPTankSizes.Lp20lbVertical.Id,
                        LPTankSizes.Lp20lbVertical.TankHeightInMm,
                        true,
                        25,
                        defaultPreferredUnits: TankHeightUnits.Centimeters);

                    if (AppSettings.Instance.AccessoryRegistration.TryAddSensorConnection(sensorConnection, requestSave: true))
                    {
                        _appDirectServices.TakeSnapshot();
                        var device = (ILogicalDeviceTankSensor)_deviceSource.DeviceService.DeviceManager.FindLogicalDevice(IsMopekaSensor);
                        tcs.SetResult(device);
                    }
                }
            };

            _bleScannerService.DidReceiveScanResult += scanAction;

            var cancellationTokenRegistration = cancelToken.Register(() =>
            {
                lock (ScanLock)
                {
                    tcs.TrySetCanceled();
                }
            });

            try
            {
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                TaggedLog.Error(LogTag, ex.Message);
                throw;
            }
            finally
            {
                _bleScannerService.DidReceiveScanResult -= scanAction;
                cancellationTokenRegistration.Dispose();
            }
        }

        public Task Unpair(ILogicalDeviceTankSensor device)
        {
            var sensorConnection = AppSettings.Instance.SensorConnections<SensorConnectionMopeka>().First(x => x.MacAddress.Equals(device.LogicalId.ProductMacAddress));
            AppSettings.Instance.AccessoryRegistration.TryRemoveSensorConnection(sensorConnection, requestSave: true);
            
            _lpSettingsRepository.DeleteSettings(device);

            return Task.CompletedTask;
        }

        public Task<bool> IsPairedOverBle(ILogicalDeviceTankSensor device)
        {
            return Task.FromResult(IsMopekaSensor(device));
        }

        private bool IsMopekaSensor(ILogicalDevice device)
        {
            if (!(device is ILogicalDeviceTankSensor))
                return false;

            if (device.LogicalId.ProductMacAddress == null)
                return false;

            return _deviceSource.IsMopekaSensorLinked(device.LogicalId.ProductMacAddress);
        }
    }
}