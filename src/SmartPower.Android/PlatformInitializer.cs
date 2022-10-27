using System;
using SmartPower.Droid.Services;
using SmartPower.Services;
using Prism;
using Prism.Ioc;

namespace SmartPower.Droid
{
    public class PlatformInitializer : IPlatformInitializer
    {
        public PlatformInitializer()
        {
        }

        public void RegisterTypes(IContainerRegistry container)
        {
            // Register any platform specific implementations
            container.RegisterSingleton(typeof(IDeviceSettingsService), typeof(DeviceSettingsService));
            //container.RegisterSingleton(typeof(IPushNotificationService), typeof(PushNotificationService));
            //container.RegisterSingleton(typeof(INativeBackgroundService), typeof(NativeBackgroundService));
        }
    }
}