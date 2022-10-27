using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;

namespace SmartPower.Services
{
    /// <summary>
    /// This implementation leverages the GeolocatorPlugin.  This plugin requires additional setup (even more setup is required if background updates are needed):
    /// 
    /// Android: 
    ///     See https://jamesmontemagno.github.io/GeolocatorPlugin/GettingStarted.html
    ///
    ///     The following must be added to the MainActivity OnCreate method    
    ///         CrossCurrentActivity.Current.Init(this, bundle)
    ///
    ///     OnRequestPermissionsResult must also include
    ///             Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    /// 
    ///     Permissions also must be specified
    ///         [assembly: UsesFeature("android.hardware.location", Required = false)]
    ///         [assembly: UsesFeature("android.hardware.location.gps", Required = false)]
    ///         [assembly: UsesFeature("android.hardware.location.network", Required = false)]
    /// 
    /// iOS (Setup for iOS11 and greater):
    ///     The GeolocatorPlugin uses the plist definitions to determine if background vs. foreground option is used.  Current, the GeolocatorPlugin
    ///     can run into deadlock issues if doing background updates.  We are currently only doing foreground updates (see the plugin documentation for
    ///     more details).
    /// 
    ///     The following plist entries must be present
    ///         <key>NSLocationWhenInUseUsageDescription</key>
    ///         <string>Location information allows LCI to provide a better customer support experience and improve our products.</string>
    ///         <key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
    ///         <string>Location information allows LCI to provide a better customer support experience and improve our products.</string>
    /// 
    ///     If NSLocationAlwaysUsageDescription is included the GeolocatorPlugin will request always usage.  If NSLocationWhenInUseUsageDescription is
    ///     included then it will request when in use usage.  One or the other must be specified.
    /// 
    /// </summary>
    public class AppLocationService : Singleton<AppLocationService>
    {
        private const string LogTag = nameof(AppLocationService);

        private const int PositionTimoutInMs = 20 * 1000;
        private const int UpdateIntervalInMs = 30 * 1000;
        private const int DesiredAccuracyInMeters = 100;

        // Required for singleton pattern.
        private AppLocationService()
        {
        }

        private static CancellationTokenSource? _monitoringPositionCancellationTokenSource;
        public Position? LastKnowLocation { get; private set; } = null;

        public void StartMonitoringLocation()
        {
            _monitoringPositionCancellationTokenSource?.TryCancelAndDispose();
            _monitoringPositionCancellationTokenSource = new CancellationTokenSource();
            var cancelToken = _monitoringPositionCancellationTokenSource.Token;

            Task.Run(async () =>
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        var position = await GetCurrentPosition(cancelToken);
                        if (position != null)
                        {
                            LastKnowLocation = position;
                        }
                    }
                    catch
                    {
                        /* ignored */
                    }

                    await TaskExtension.TryDelay(UpdateIntervalInMs, cancelToken);
                }
            }, cancelToken);
        }

        public void StopMonitoringLocation()
        {
            _monitoringPositionCancellationTokenSource?.TryCancelAndDispose();
            _monitoringPositionCancellationTokenSource = null;
        }

        public static async Task<Position?> GetCurrentPosition(CancellationToken cancellationToken)
        {
            Position? position = null;

            try
            {
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = DesiredAccuracyInMeters;

                if (!CrossGeolocator.IsSupported || !CrossGeolocator.Current.IsGeolocationAvailable)
                    return null;

                position = await locator.GetLastKnownLocationAsync();
                if (position != null)
                    return position;

                if (!CrossGeolocator.IsSupported || !CrossGeolocator.Current.IsGeolocationAvailable)
                    return null;

                position = await locator.GetPositionAsync(TimeSpan.FromMilliseconds(PositionTimoutInMs), cancellationToken, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to get location: " + ex);
            }
            //finally {
            //if( position != null )
            //    TaggedLog.Debug(LogTag, $"GetCurrentPosition Time: {position.Timestamp} \nLat: {position.Latitude} \nLong: {position.Longitude} \nAltitude: {position.Altitude} \nAltitude Accuracy: {position.AltitudeAccuracy} \nAccuracy: {position.Accuracy} \nHeading: {position.Heading} \nSpeed: {position.Speed}");
            //}

            return position;
        }

    }
}
