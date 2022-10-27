using System;
using IDS.Portable.Common;

namespace SmartPower
{
    public interface IAppSettings : IAppSettingsConnection, IAppSettingsConnectionAbs, IAppSettingsConnectionSensor, IAppSettingsLoadSave
    {
        bool IsDebug { get; }
    }

    /// <summary>
    /// AppSettings currently assumes the presence of a single coach.  However, the underlying serialization structures support multiple
    /// coaches.  It is assumed that AppSettings will be refactored when the app supports multiple coaches.
    /// </summary>
    public partial class AppSettings : Singleton<AppSettings>, IAppSettings
    {
        public const string LogTag = nameof(AppSettings);

        private AppSettings()
        {
            /* required for Singleton */
            /* Be careful as AppSettings may be used VERY early in app setup before many components are fully initialized */
            /* DO NOT TRY TO START CAN OR SET THE SELECTED RV HERE AS THINGS WON't BE REGISTERED PROPERLY YET TO DO THIS */
            /* See the LOAD Method */

            // Configure Sensor Registration
            //
            AccessoryRegistration.DoSensorConnectionAdded += AccessoryRegistrationOnDoSensorConnectionAdded;
            AccessoryRegistration.DoSensorConnectionRemoved += AccessoryRegistrationOnDoSensorConnectionRemoved;
        }

#if DEBUG
        public bool IsDebug => true;
        #else
        public bool IsDebug => false;
        #endif
    }
}