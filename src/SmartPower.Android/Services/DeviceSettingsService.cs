using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Provider;
using SmartPower.Services;
using Plugin.CurrentActivity;

namespace SmartPower.Droid.Services
{
    public class DeviceSettingsService : IDeviceSettingsService
    {
        public void NavigateToLocationSourceSettings()
            => CrossCurrentActivity.Current.Activity.StartActivityForResult(new Intent(Settings.ActionLocationSourceSettings), 0);

        public void NavigateToBluetoothSettings()
            => CrossCurrentActivity.Current.Activity.StartActivityForResult(new Intent(Settings.ActionBluetoothSettings), 0);

        public void EnableBluetoothAdapter()
        {
            if (IsBluetoothEnabled)
                return;
            BluetoothManager.Adapter?.Enable();
        }

        private BluetoothManager BluetoothManager
            => (BluetoothManager)Application.Context.GetSystemService(Context.BluetoothService);

        public bool IsBluetoothEnabled
            => BluetoothManager.Adapter?.IsEnabled ?? false;

        public bool AreLocationServicesEnabled
        {
            get
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                    return (Application.Context.GetSystemService(Context.LocationService) as LocationManager)?.IsLocationEnabled ?? false;

                #pragma warning disable 618
                // This is only called on devices running API 27 or lower.
                // 
                return Settings.Secure.GetInt(CrossCurrentActivity.Current.AppContext.ContentResolver, Settings.Secure.LocationMode) > 0;
                #pragma warning restore 618
            }
        }
    }
}