#nullable enable
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Devices.AwningSensor;
using OneControl.Devices.BatteryMonitor;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OneControl
{
    public static class AppDemoMode
    {
        public static readonly LogicalDeviceTagSourceDemo DefaultDemoTag = new LogicalDeviceTagSourceDemo();

        public static readonly Guid DefaultDemoDeviceSourceGuid = new Guid("d7d0a305-c542-4e56-8ced-30ed71ec033d");

        private static readonly List<string> _registeredDemoDeviceTokens = new List<string>();

        public class LogicalDeviceSourceDemo : ILogicalDeviceSourceDirectDemo, ILogicalDeviceSourceDirectRename, ILogicalDeviceSourceDirectMetadata
        {
            private const string LogTag = nameof(LogicalDeviceSourceDemo);

            public IEnumerable<ILogicalDeviceTag> DeviceSourceTags { get; } = new[] { DefaultDemoTag };

            public IEnumerable<ILogicalDeviceTag> MakeDeviceSourceTags(ILogicalDevice? logicalDevice) => DeviceSourceTags;

            public ILogicalDeviceService DeviceService { get; }

            public IN_MOTION_LOCKOUT_LEVEL InTransitLockoutLevel { get; } = IN_MOTION_LOCKOUT_LEVEL.LEVEL_0_NO_LOCKOUT;

            public MAC DemoMac { get; }  // "FAKE" demo MAC address
            public MAC LevelerDemoMac { get; }  // "FAKE" demo MAC address

            public string DeviceSourceToken { get; }

            public bool IsDeviceSourceActive => true;

            public bool AllowAutoOfflineLogicalDeviceRemoval => false;

            public LogicalDeviceSourceDemo(ILogicalDeviceService logicalDeviceService, Guid sourceGuid)
            {
                DeviceService = logicalDeviceService ?? throw new ArgumentNullException(nameof(logicalDeviceService));
                DeviceSourceToken = sourceGuid.ToString();

                var sourceGuidBytes = sourceGuid.ToByteArray();
                var demoMacBytes = sourceGuidBytes.ToNewArray(sourceGuidBytes.Length - 6, 6);  // Take the last 6 bytes of the sourceGuid and use them as a fake "MAC"
                DemoMac = new MAC(demoMacBytes);

                // Create a different Mac for the leveler and associated light to live on.
                Array.Reverse(demoMacBytes);
                LevelerDemoMac = new MAC(demoMacBytes);
            }

            public virtual bool RegisterDevices()
            {
                lock (_registeredDemoDeviceTokens)
                {
                    if (DemoDevices.Count > 0)
                    {
                        TaggedLog.Warning(LogTag, $"Demo devices already registered for {DeviceSourceToken}");
                        return false;
                    }

                    // This is the first time Register Devices was called so setup the device source token
                    if (!_registeredDemoDeviceTokens.Contains(DeviceSourceToken))
                        _registeredDemoDeviceTokens.Add(DeviceSourceToken);

                    var deviceManager = DeviceService.DeviceManager;
                    if (deviceManager == null)
                    {
                        TaggedLog.Warning(LogTag, $"Unable to setup Demo devices because DeviceManager is NULL");
                        return false;
                    }

                    #region Battery Monitor
                    var batteryMonitorId = new LogicalDeviceId(DEVICE_TYPE.BATTERY_MONITOR, 0, FUNCTION_NAME.MAIN_BATTERY, 0, PRODUCT_ID.UNKNOWN, DemoMac);
                    var batteryMonitor2Id = new LogicalDeviceId(DEVICE_TYPE.BATTERY_MONITOR, 0, FUNCTION_NAME.AUXILIARY_BATTERY, 0, PRODUCT_ID.UNKNOWN, DemoMac);
                    var batteryMonitor3Id = new LogicalDeviceId(DEVICE_TYPE.BATTERY_MONITOR, 0, FUNCTION_NAME.RV_BATTERY, 0, PRODUCT_ID.UNKNOWN, DemoMac);
                    var batteryMonitor4Id = new LogicalDeviceId(DEVICE_TYPE.BATTERY_MONITOR, 0, FUNCTION_NAME.KITCHEN_BATTERY, 0, PRODUCT_ID.UNKNOWN, DemoMac);
                    deviceManager.RegisterLogicalDevice(new LogicalDeviceBatteryMonitorSim(batteryMonitorId, new LogicalDeviceBatteryMonitorCapability(BatteryMonitorCapabilityFlag.SupportsAllBatteries), DeviceService), this);
                    deviceManager.RegisterLogicalDevice(new LogicalDeviceBatteryMonitorSim(batteryMonitor2Id, new LogicalDeviceBatteryMonitorCapability(BatteryMonitorCapabilityFlag.SupportsAllBatteries), DeviceService), this);
                    deviceManager.RegisterLogicalDevice(new LogicalDeviceBatteryMonitorSim(batteryMonitor3Id, new LogicalDeviceBatteryMonitorCapability(BatteryMonitorCapabilityFlag.SupportsAllBatteries), DeviceService), this);
                    deviceManager.RegisterLogicalDevice(new LogicalDeviceBatteryMonitorSim(batteryMonitor4Id, new LogicalDeviceBatteryMonitorCapability(BatteryMonitorCapabilityFlag.SupportsAllBatteries), DeviceService), this);
                    #endregion
                    
                    // Remember which demo devices are ours
                    //
                    DemoDevices = DeviceService.DeviceManager?.FindLogicalDevices<ILogicalDevice>((ld) => ld.IsAssociatedWithDeviceSource(this) && ld is ILogicalDeviceSimulated) ?? new List<ILogicalDevice>();

                    TaggedLog.Warning(LogTag, $"Demo devices Registered {DemoDevices.Count} Device With DeviceSourceToken {DeviceSourceToken}");

                    return true;
                }
            }

            public List<ILogicalDevice> DemoDevices = new List<ILogicalDevice>();

            public void RemoveDemoDevices()
            {
                lock (_registeredDemoDeviceTokens)
                {
                    DeviceService.DeviceManager?.RemoveLogicalDevice((ld) => DemoDevices.Contains(ld));
                    DemoDevices.Clear();
                }
            }

            public void TurnOnDevices()
            {
                foreach (var demoDevice in DemoDevices)
                {
                    demoDevice.UpdateDeviceOnline(true);
                }

                //Resync devices 
                DeviceService.DeviceManager?.ContainerDataSourceSync(false);
            }

            public void TurnOffDevices()
            {
                foreach (var demoDevice in DemoDevices)
                {
                    demoDevice.UpdateDeviceOnline(false);
                }

                //Resync devices 
                DeviceService.DeviceManager?.ContainerDataSourceSync(false);
            }

            public bool IsLogicalDeviceSupported(ILogicalDevice? logicalDevice) => logicalDevice?.IsAssociatedWithDeviceSource(DeviceSourceToken) ?? false;

            public bool IsLogicalDeviceOnline(ILogicalDevice? logicalDevice) => true;  // Demo devices always report as online (or they can override if needed)

            public IN_MOTION_LOCKOUT_LEVEL GetLogicalDeviceInTransitLockoutLevel(ILogicalDevice? logicalDevice) => InTransitLockoutLevel;

            public bool IsLogicalDeviceHazardous(ILogicalDevice? logicalDevice) => false;  // Demo devices are never Hazardous unless they want to override

            public override string ToString() => LogTag;

            public bool IsLogicalDeviceRenameSupported(ILogicalDevice? logicalDevice) => IsLogicalDeviceSupported(logicalDevice);

            public Task RenameLogicalDevice(ILogicalDevice? logicalDevice, FUNCTION_NAME toName, byte toFunctionInstance, CancellationToken cancellationToken)
            {
                if (!IsLogicalDeviceRenameSupported(logicalDevice))
                    return Task.FromResult(CommandResult.ErrorOther);

                if (logicalDevice?.Rename(toName, toFunctionInstance) != true)
                    throw new LogicalDeviceSourceDirectRenameException($"Rename failed");

                return Task.FromResult(CommandResult.Completed);
            }

            #region ILogicalDeviceSourceDirectMetadata
            public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken)
            {
                return Task.FromResult("Demo_1");
            }

            public Task<string> GetSoftwarePartNumberAsync(ILogicalDevice logicalDevice, CancellationToken cancelToken, TimeSpan timeout)
            {
                throw new NotImplementedException();
            }

            public Version? GetDeviceProtocolVersion(ILogicalDevice logicalDevice)
            {
                return new Version(1, 0, 0);
            }
            #endregion
        }

        private static LogicalDeviceSourceDemo? _defaultDemoDeviceSource = null;
        public static LogicalDeviceSourceDemo DefaultDemoDeviceSource
        {
            get
            {
                lock (_registeredDemoDeviceTokens)
                {
                    if (_defaultDemoDeviceSource != null)
                        return _defaultDemoDeviceSource;

                    var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
                    _defaultDemoDeviceSource = new LogicalDeviceSourceDemo(logicalDeviceService, DefaultDemoDeviceSourceGuid);
                    return _defaultDemoDeviceSource;
                }
            }
        }

    }
}