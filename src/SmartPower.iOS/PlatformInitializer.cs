using System;
using SmartPower.iOS.Services;
using SmartPower.Services;
using Prism;
using Prism.Ioc;

namespace SmartPower.iOS
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