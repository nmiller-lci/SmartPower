using System;
using IDS.Core.Collections;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using System.Collections.Generic;
using System.Linq;
using SmartPower.Services;
using OneControl.Direct.IdsCanAccessoryBle;
using OneControl.Direct.IdsCanAccessoryBle.Connections;
using OneControl.Direct.IdsCanAccessoryBle.ScanResults;

namespace SmartPower
{
    public interface IAppSettingsConnectionSensor
    {
        IReadOnlyList<ILogicalDeviceSourceDirect> StandardSharedSensorSources { get; }

        public IEnumerable<ISensorConnection> SensorConnectionsAll { get; }

        bool HasSensorConnection { get; }

        bool IsSensorConnectionKnown(ISensorConnection sensorConnection);
        
        bool TryAddSensorConnection(ISensorConnection sensorConnection, bool requestSave = true);

        bool TryRemoveSensorConnection(ISensorConnection sensorConnection, bool requestSave = true);

        void SetSensorConnections(IReadOnlyList<ISensorConnection> sensorConnections, bool requestSave, bool notifyChanged);
    }

    public partial class AppSettings : IAppSettingsConnectionSensor
    {
        private IAccessoryRegistration? _accessoryRegistration = null;
        public IAccessoryRegistration AccessoryRegistration => _accessoryRegistration ??= Resolver<IAccessoryRegistration>.Resolve ?? new AccessoryRegistration();

        public IReadOnlyList<ILogicalDeviceSourceDirect> StandardSharedSensorSources => AccessoryRegistration.StandardSharedSensorSources;

        public IEnumerable<ISensorConnection> SensorConnectionsAll
        {
            get
            {
                foreach (var sensor in AccessoryRegistration.SensorConnectionsAll)
                    yield return sensor;
            }
        }

        public IEnumerable<TSensorConnection> SensorConnections<TSensorConnection>()
            where TSensorConnection : ISensorConnection => SensorConnectionsAll.OfType<TSensorConnection>();

        public bool HasSensorConnection => SensorConnectionsAll.Any();

        public bool IsSensorConnectionKnown(ISensorConnection sensorConnection) => SensorConnectionsAll.Contains(sensorConnection);

        public bool TryAddSensorConnection(IdsCanAccessoryScanResult accessoryScanResult, bool requestSave) =>
            AccessoryRegistration.TryAddSensorConnection(accessoryScanResult, requestSave);

        public bool TryAddSensorConnection(ISensorConnection sensorConnection, bool requestSave) =>
            AccessoryRegistration.TryAddSensorConnection(sensorConnection, requestSave);

        public bool TryRemoveSensorConnection(ISensorConnection sensorConnection, bool requestSave) =>
            AccessoryRegistration.TryRemoveSensorConnection(sensorConnection, requestSave);

        /// <summary>
        /// </summary>
        /// <param name="sensorConnections"></param>
        /// <param name="autoSave">Iff true and there was a modification to the sensors added or removed a save will be performed</param>
        public void SetSensorConnections(IReadOnlyList<ISensorConnection> sensorConnections, bool autoSave, bool notifyChanged)
        {
            int numModification = 0;

            var removeConnections = AccessoryRegistration.SensorConnectionsAll.ToList().Where(connection => !sensorConnections.Contains(connection));
            foreach (var connection in removeConnections)
            {
                numModification += 1;
                TryRemoveSensorConnection(connection, requestSave: false);
            }

            // Add the connections if they aren't already in the list
            //
            foreach (var connection in sensorConnections)
                numModification += TryAddSensorConnection(connection, requestSave: false) ? 1 : 0;

            if (autoSave && numModification > 0)
                Save();

            // Sensors don't have their own update notification they just piggy back on the AppSelectedRvUpdateMessage
            //
            if (notifyChanged && numModification > 0)
                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
        }

        private void AccessoryRegistrationOnDoSensorConnectionAdded(ISensorConnection sensorConnection, bool newlyLinked, bool requestSave)
        {
            if (requestSave)
            {
                Save();
                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
            }
        }

        private void AccessoryRegistrationOnDoSensorConnectionRemoved(ISensorConnection sensorConnection, bool newRemoval, bool requestSave)
        {
            if (requestSave)
            {
                Save();

                // Persists the removal of the device source from the logical device.
                AppDirectServices.Instance.TakeSnapshot();

                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
            }
        }
    }
}
