#nullable enable
using IDS.Portable.LogicalDevice;
using OneControl.Devices;
using OneControl.Devices.TankSensor.Mopeka;
using OneControl.Direct.IdsCanAccessoryBle.Mopeka;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartPower.Services;
using Xamarin.Forms;

namespace OneControl.UserInterface.AddAndManageDevices.LiquidPropane.Services
{
    /// <summary>
    /// This implementation of ILPSettingsRepository reads/writes settings onto
    /// the associated device's tags.
    /// Each time a setting is change, a snapshot is taken to ensure that the data is persisted.
    /// </summary>
    public class LPSettingsRepository: ILPSettingsRepository
    {
        private readonly AppDirectServices _appDirectServices;
        private readonly ILogicalDeviceTagManager _tagManager;

        public LPSettingsRepository(AppDirectServices appDirectServices,
            ILogicalDeviceTagManager tagManager)
        {
            _appDirectServices = appDirectServices;
            _tagManager = tagManager;
        }

        public Task CreateSettings(
            ILogicalDeviceTankSensor device,
            LPTankName name,
            ILPTankSize size,
            bool isNotificationEnabled,
            int notificationThreshold,
            float accelXOffset,
            float accelYOffset,
            TankHeightUnits preferredUnits,
            CancellationToken token)
        {
            if (GetPersistedTag(device) != null)
                return Task.FromException(new Exception($"Settings already exist for logical device {device.LogicalId.ProductMacAddress}"));

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                device.LogicalId.ProductMacAddress,
                size.Id,
                size.TankHeightInMm,
                isNotificationEnabled,
                notificationThreshold,
                accelXOffset,
                accelYOffset,
                false,
                false,
                preferredUnits), device);
            _appDirectServices.TakeSnapshot();
            return Task.CompletedTask;
        }

        public Task DeleteSettings(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            _tagManager.RemoveTag(tag, device);
            _appDirectServices.TakeSnapshot();
            return Task.CompletedTask;
        }

        public Task<bool> HasLPSettings(ILogicalDeviceTankSensor? device, CancellationToken token) => Task.FromResult(GetPersistedTag(device) != null);

        public Task<LPTankName> GetTankName(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            return Task.FromResult(LPTankName.GetByFunctionNameAndInstance(device.LogicalId.FunctionName.ToFunctionName(), device.LogicalId.FunctionInstance));
        }

        public Task SetTankName(ILogicalDeviceTankSensor device, LPTankName name, CancellationToken token)
        {
            device.Rename(name.FunctionName.ToFunctionName(), name.FunctionInstance);
            _appDirectServices.TakeSnapshot();

            return Task.CompletedTask;
        }

        public Task<ILPTankSize> GetTankSize(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<ILPTankSize>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            if (tag.TankSizeId == LPTankSizes.ArbitraryTankSizeId)
            {
                return Task.FromResult<ILPTankSize>(new ArbitraryTankSize(tag.TankHeightInMm));
            }
            else
            {
                return Task.FromResult<ILPTankSize>(LPTankSizes.GetById(tag.TankSizeId));
            }
        }

            public Task SetTankSize(ILogicalDeviceTankSensor device, ILPTankSize size, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<ILPTankSize>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));
            
            _tagManager.RemoveTag(tag, device);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                size.Id,
                size.TankHeightInMm,
                tag.IsNotificationEnabled,
                tag.NotificationThreshold,
                tag.AccelXOffset,
                tag.AccelYOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);

            _appDirectServices.TakeSnapshot();

            return Task.CompletedTask;
        }

        public Task<bool> IsThresholdNotificationEnabled(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<bool>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            return Task.FromResult(tag.IsNotificationEnabled);
        }

        public async Task EnableThresholdNotification(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                throw new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}");

            _tagManager.RemoveTag(tag, device);

            var changed = (tag.IsNotificationEnabled != true);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                tag.TankSizeId,
                tag.TankHeightInMm,
                true,
                tag.NotificationThreshold,
                tag.AccelXOffset,
                tag.AccelYOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);

            _appDirectServices.TakeSnapshot();

            if (changed)
            {
                await RaiseNotificationSettingsUpdated(device, token);
            }
        }

        public async Task DisableThresholdNotification(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                throw new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}");

            _tagManager.RemoveTag(tag, device);

            var changed = (tag.IsNotificationEnabled != false);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                tag.TankSizeId,
                tag.TankHeightInMm,
                false,
                tag.NotificationThreshold,
                tag.AccelXOffset,
                tag.AccelYOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);

            _appDirectServices.TakeSnapshot();

            if (changed)
            {
                await RaiseNotificationSettingsUpdated(device, token);
            }
        }

        public Task<int> GetNotificationThreshold(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<int>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            return Task.FromResult(tag.NotificationThreshold);
        }

        public async Task SetNotificationThreshold(ILogicalDeviceTankSensor device, int tankLevelPercent, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                throw new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}");

            _tagManager.RemoveTag(tag, device);

            var changed = (tag.NotificationThreshold != tankLevelPercent);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                tag.TankSizeId,
                tag.TankHeightInMm,
                tag.IsNotificationEnabled,
                tankLevelPercent,
                tag.AccelXOffset,
                tag.AccelYOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);

            _appDirectServices.TakeSnapshot();

            if (changed)
            {
                await RaiseNotificationSettingsUpdated(device, token);
            }
        }

        public event EventHandler<NotificationSettingsUpdatedEventArgs> NotificationSettingsUpdated;

        protected async Task RaiseNotificationSettingsUpdated(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            // Needed to use MessagingCenter because LPSettingsRepository cannot be injected to all depending objects.
            // Hence a CLR event will not work until the dependencies are sorted out.
            MessagingCenter.Instance.Send<ILPSettingsRepository, NotificationSettingsUpdatedEventArgs>(
                this, NotificationSettingsUpdatedEventArgs.MessageId, new NotificationSettingsUpdatedEventArgs(
                    device,
                    await IsThresholdNotificationEnabled(device, token),
                    await GetNotificationThreshold(device, token)));
        }

        public Task<CalibrationOffsets> GetPositionCalibrationOffsets(ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<CalibrationOffsets>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            return Task.FromResult(new CalibrationOffsets(tag.AccelXOffset, tag.AccelYOffset));
        }

        public Task SetPositionCalibrationOffsets(ILogicalDeviceTankSensor device, float xOffset, float yOffset, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            _tagManager.RemoveTag(tag, device);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                tag.TankSizeId,
                tag.TankHeightInMm,
                tag.IsNotificationEnabled,
                tag.NotificationThreshold,
                xOffset,
                yOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);

            _appDirectServices.TakeSnapshot();

            return Task.CompletedTask;
        }

        public Task SetFaulted(LPFaultType faultType, ILogicalDeviceTankSensor device, bool isFaulted, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            _tagManager.RemoveTag(tag, device);
            
            switch (faultType)
            {
                case LPFaultType.Tank:
                    _tagManager.AddTag(new MopekaLogicalDeviceTag(
                    tag.MacAddress,
                    tag.TankSizeId,
                    tag.TankHeightInMm,
                    tag.IsNotificationEnabled,
                    tag.NotificationThreshold,
                    tag.AccelXOffset,
                    tag.AccelYOffset,
                    isFaulted,
                    tag.IsBatteryLevelFaulted,
                tag.PreferredUnits), device);
                    break;
                case LPFaultType.Battery:
                    _tagManager.AddTag(new MopekaLogicalDeviceTag(
                    tag.MacAddress,
                    tag.TankSizeId,
                    tag.TankHeightInMm,
                    tag.IsNotificationEnabled,
                    tag.NotificationThreshold,
                    tag.AccelXOffset,
                    tag.AccelYOffset,
                    tag.IsTankLevelFaulted,
                    isFaulted,
                tag.PreferredUnits), device);
                    break;
                default:
                    return Task.FromException<bool>(new Exception($"Unknown faultType for logical device {device.LogicalId.ProductMacAddress}")); ;
            }

            _appDirectServices.TakeSnapshot();

            return Task.CompletedTask;
        }

        public Task<bool> IsFaulted(LPFaultType faultType, ILogicalDeviceTankSensor device, CancellationToken token)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<bool>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            switch (faultType)
            {
                case LPFaultType.Tank:
                    return Task.FromResult(tag.IsTankLevelFaulted);
                case LPFaultType.Battery:
                    return Task.FromResult(tag.IsBatteryLevelFaulted);
                default:
                    return Task.FromException<bool>(new Exception($"Unknown faultType for logical device {device.LogicalId.ProductMacAddress}")); ;
            }
            
        }

        public Task SetPreferredUnits(ILogicalDeviceTankSensor device, TankHeightUnits preferredUnits, CancellationToken token = default)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            _tagManager.RemoveTag(tag, device);

            _tagManager.AddTag(new MopekaLogicalDeviceTag(
                tag.MacAddress,
                tag.TankSizeId,
                tag.TankHeightInMm,
                tag.IsNotificationEnabled,
                tag.NotificationThreshold,
                tag.AccelXOffset,
                tag.AccelYOffset,
                tag.IsTankLevelFaulted,
                tag.IsBatteryLevelFaulted,
                preferredUnits), device);

            _appDirectServices.TakeSnapshot();

            return Task.CompletedTask;
        }

        public Task<TankHeightUnits> GetPreferredUnits(ILogicalDeviceTankSensor device, CancellationToken token = default)
        {
            var tag = GetPersistedTag(device);
            if (tag == null)
                return Task.FromException<TankHeightUnits>(new Exception($"Settings do not exist for logical device {device.LogicalId.ProductMacAddress}"));

            return Task.FromResult(tag.PreferredUnits);
        }

        private MopekaLogicalDeviceTag? GetPersistedTag(ILogicalDeviceTankSensor? device)
        {
            return device is null ? null : _tagManager.GetTags<MopekaLogicalDeviceTag>(device).FirstOrDefault();
        }
    }
}
