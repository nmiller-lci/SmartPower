using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using IDS.Portable.Common;

namespace SmartPower.Droid
{
    [Activity(
        Label = "SmartPower",
        Name = "SmartPower.android." + nameof(MainActivity),
        Icon = "@mipmap/icon",
        Theme = "@style/SplashTheme",
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.UiMode)]
    public class MainActivity : Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            AndroidX.Core.SplashScreen.SplashScreen.InstallSplashScreen(this);
            
            MainThread.UpdateMainThreadContext();

            base.OnCreate(savedInstanceState);

            PrismExtensions.Android.Platform.Init(this, savedInstanceState, null);

            Xamarin.Forms.Forms.Init(this, savedInstanceState);

            LoadApplication(new App(new PlatformInitializer()));

            ZXing.Net.Mobile.Forms.Android.Platform.Init();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        
        public override void OnBackPressed()
        {
            if (PrismExtensions.Android.Platform.OnBackPressed())
                return;

            base.OnBackPressed();
        }
    }
}
