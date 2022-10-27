using System;
using Foundation;
using IDS.Portable.BLE.Platforms.iOS;
using IDS.Portable.Common;
using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Serilog;
using UIKit;

namespace SmartPower.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            SetupMainThreadDispatcher();
            MainThread.UpdateMainThreadContext();   // We know we are on the main thread!

            SetupSerilog();
            
            BleImplementation.UseRestorationIdentifier("Smart Power");  //Set support for iOS BLE State restoration
            
            IDS.Portable.BLE.Platforms.iOS.PlatformInitializer.Init();
            IDS.UI.iOS.Platform.Init(LoggerFactory.Create(builder => builder.AddSerilog()));
            PrismExtensions.iOS.Platform.Init();

            Xamarin.Forms.Forms.Init();
            LoadApplication(new App(new PlatformInitializer()));

            ZXing.Net.Mobile.Forms.iOS.Platform.Init();

            return base.FinishedLaunching(app, options);
        }

        #region Main Thread
        /// <summary>
        /// Setup ids.portable.common main thread factory.  This allows us to have a cross framework main thread dispatcher, and is
        /// used by modules that are dependent on ids.portable.common.
        /// </summary>
        protected void SetupMainThreadDispatcher()
        {
            // Xamarin Forms has a cross platform thread dispatcher, but we need a slightly different signature for our
            // main thread dispatcher (it needs to return a boolean).  This method wraps Xamarin implementation so we
            // can call it through our main thread action factory.
            //
            static bool RequestMainThreadAction(Action action)
            {
                // NOTE: if this is called on the main thread we won't execute the action immediately.  The action will get queued
                // on the main event loop and processed later.  Ideally, we would want to execute the code immediately if we are already
                // on the main thread.  See https://forums.xamarin.com/discussion/24218/is-there-a-way-to-determine-if-you-are-on-the-main-thread
                //
                // We avoid this issue by using a feature of the MainThread library to establish our MainThread Context (UpdateMainThreadContext)
                // so we only need to use the this lambda when we aren't already on the main thread.
                //
                Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
                return true;
            }

            MainThread.RequestMainThreadActionFactory = () => RequestMainThreadAction;
        }
        #endregion

        private void SetupSerilog()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Output log location: {0}", AppLogConstants.LogFileNameFull);
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.ControlledBy(AppLogConstants.LogLevelCurrent)
                    // The File sink has an option to support multi-threaded writes (shared).  However, this option doesn't seem to work correctly on iOS (no log file is
                    // actually generated).  So we use the Async sink to do all file writes on a single separate thread.  This allows us to not need the shared option given
                    // all writes will occur on a single Async thread.
                    //
                    // Moved NSLOG inside the Async as the NSLog Serilog sink wasn't writing message atomically.  Threads were stomping on each others log output. 
                    //
                    .WriteTo.Async(a => {
                        a.NSLog(outputTemplate: AppLogConstants.LogConsoleOutputTemplate);
                        a.File(AppLogConstants.LogFileNameFull, fileSizeLimitBytes: AppLogConstants.LogFileSizeLimitBytes, rollingInterval: RollingInterval.Day, retainedFileCountLimit: AppLogConstants.LogFileRetainedCountLimit, outputTemplate: AppLogConstants.LogFileOutputTemplate, flushToDiskInterval: TimeSpan.FromMilliseconds(AppLogConstants.LogFlushIntervalMs));
                    })
                    .CreateLogger();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Unable to configure Serilog: {0}\n{1}", ex.Message, ex.StackTrace);
            }
        }
    }
}
