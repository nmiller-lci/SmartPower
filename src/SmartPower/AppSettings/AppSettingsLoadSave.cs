using System;
using System.Collections.Generic;
using SmartPower.Connections.Rv;
using OneControl.Direct.IdsCanAccessoryBle.Connections;

namespace SmartPower
{
    public interface IAppSettingsLoadSave
    {
        void Load(bool force = false);
        void Save();
    }

    public partial class AppSettings : IAppSettingsLoadSave
    {
        public void Load(bool force = false)
        {
            /* Not Implemented */
        }

        public void Save()
        {
            /* Not Implemented */
        }

        private void Save(IRvGatewayConnection rvDirectConnection, IRvGatewayConnection absGatewayCanConnection, IEnumerable<ISensorConnection>? sensorConnections)
        {
            /* Not Implemented */
        }

        private void Save(IRvGatewayConnection rvDirectConnection, IRvGatewayConnection absDirectConnection) => Save(rvDirectConnection, absDirectConnection, SensorConnectionsAll);
    }
}
