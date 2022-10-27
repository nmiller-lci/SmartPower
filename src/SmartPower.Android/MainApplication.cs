using System;
using Android.App;
using Android.Runtime;
using IDS.Portable.Common;
using Microsoft.Extensions.Logging;
using Plugin.CurrentActivity;
using Serilog;

namespace SmartPower.Droid
{
    [Application]
    public class MainApplication : Application
    {
        private ILoggerFactory _loggerFactory;

        public MainApplication(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
            // Setup Main Thread Framework
            //
            SetupMainThreadDispatcher();
            MainThread.UpdateMainThreadContext(); // CALL ASAP (We know/depend we are on the main thread!)

            SetupSerilog();

            _loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());

            IDS.Portable.BLE.Platforms.Android.PlatformInitializer.Init();
            IDS.UI.Android.Platform.Init(this, _loggerFactory);
        }

        public override void OnCreate()
        {
            base.OnCreate();

            Xamarin.Essentials.Platform.Init(this);
            CrossCurrentActivity.Current.Init(this);
        }

        public static bool IsFormsInit { get; set; }

        #region Main Thread
        // <summary>
        // Setup ids.portable.common main thread factory.  This allows us to have a cross framework main thread dispatcher, and is
        // used by modules that are dependent on ids.portable.common.
        // </summary>
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
                // There is an issue where during early startup forms is not init yet and we ended up calling Xamarin.Forms.Device.BeginInvokeOnMainThread
                // which was causing an exception.  So we now fallback to use Xamarin.Essentials.MainThread.BeginInvokeOnMainThread while forms isn't setup.
                // The reason we don't always use the Xamarin.Essentials version is that it doesn't support force dispatching to the main thread.  There are
                // some UI workflows that require that functionality.
                //
                if (IsFormsInit)
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(action);
                else
                    Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(action);
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
                        a.AndroidLog(outputTemplate: AppLogConstants.LogcatConsoleOutputTemplate);
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
