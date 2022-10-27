using System;
using CoreBluetooth;
using CoreFoundation;
using SmartPower.Services;

namespace SmartPower.iOS.Services
{
    public class DeviceSettingsService : IDeviceSettingsService
    {
        private readonly CBCentralManager _bluetoothManager;  // NOSONAR (csharpsquid:S4487)

        public DeviceSettingsService()
        {
            var checkBluetoothStateCBCentralManagerDelegate = new CheckBluetoothStateCBCentralManagerDelegate(this);
            // this object only exists to hold onto and trigger the delegate for
            // bluetooth state changes. this is the correct use of this api. not assigning
            // it at all results in a static code analysis failure, and could trigger garbage 
            // collection on the object.
            _bluetoothManager = new CBCentralManager(checkBluetoothStateCBCentralManagerDelegate, DispatchQueue.MainQueue);
        }

        public bool IsBluetoothEnabled { get; private set; }

        public bool AreLocationServicesEnabled => true;

        public void EnableBluetoothAdapter()
        {
            // FUTURE: there doesn't appear to be any way to do this on iOS
            // even though iOS makes reference to setting the power
            // to "on" for the CBCentralManager.
        }

        public void NavigateToBluetoothSettings()
        {
            // You can only go to your app-level settings, which could be very
            // confusing to the end-user, so we don't navigate to settings at all.
            // iOS itself will prompt to turn on bluetooth and provide a link to settings.
        }

        public void NavigateToLocationSourceSettings() { }

        private class CheckBluetoothStateCBCentralManagerDelegate : CBCentralManagerDelegate
        {
            private readonly DeviceSettingsService _deviceSettingsService;
            public CheckBluetoothStateCBCentralManagerDelegate(DeviceSettingsService deviceSettingsService)
            {
                _deviceSettingsService = deviceSettingsService;
            }
            public override void UpdatedState(CBCentralManager central)
            {
                _deviceSettingsService.IsBluetoothEnabled = (central.State != CBCentralManagerState.PoweredOff && central.State != CBCentralManagerState.Unauthorized);
            }
        }
    }
}