using System;
using DryIoc;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using SmartPower.Connections.Rv;
using OneControl;
using OneControl.Direct.Can;
using OneControl.Direct.MyRvLinkBle;

namespace SmartPower.Services
{
    public class AppDirectConnection
    {
        private const string LogTag = nameof(AppDirectConnection);

        public IRvGatewayConnection Connection { get; }

        public ILogicalDeviceSourceDirect? DeviceSource { get; }

        public ConnectionManagerStatus ConnectionStatus
        {
            get
            {
                if (DeviceSource == null)
                    return ConnectionManagerStatus.Disconnected;

                if (!(DeviceSource is ILogicalDeviceSourceDirectConnection directConnection))
                    return ConnectionManagerStatus.Connected;

                if (directConnection?.IsConnected == true)
                    return ConnectionManagerStatus.Connected;

                return ConnectionManagerStatus.Disconnected;
            }
        }

        public string ConnectionId
        {
            get
            {
                return Connection switch
                {
                    IDirectMyRvLinkConnectionBle rvDirectMyRvLinkConnection => rvDirectMyRvLinkConnection.ConnectionId,
                    null => string.Empty,
                    _ => Connection.ConnectionNameFriendly
                };
            }
        }

        public AppDirectConnection(ILogicalDeviceServiceIdsCan logicalDeviceService, IRvGatewayConnection connection)
        {
            Connection = connection;

            TaggedLog.Information(LogTag, $"Configuring Direct Connection for {connection ?? AppSettings.DefaultRvDirectConnectionNone}");
            switch (connection)
            {
                case IRvDirectConnectionDemo demoConnection:
                    DeviceSource = AppDemoMode.DefaultDemoDeviceSource;
                    break;

                case IRvDirectConnectionNone _:
                    DeviceSource = null;
                    break;

                case IRvGatewayIdsCanConnection idsCanConnection:
                    {
                        var logicalDeviceTag = idsCanConnection.MakeLogicalDeviceTagSource();
                        DeviceSource = new DirectConnectionIdsCan(logicalDeviceService, AppCanDeviceInfo.Instance, idsCanConnection, idsCanConnection.DeviceSourceToken, logicalDeviceTag);
                        break;
                    }

                case IDirectMyRvLinkConnectionBle myRvLinkConnection:
                    {
                        var logicalDeviceTag = myRvLinkConnection.MakeLogicalDeviceTagSource();
                        DeviceSource = new DirectConnectionMyRvLinkBle(logicalDeviceService, myRvLinkConnection.ConnectionGuid, myRvLinkConnection.ConnectionId, myRvLinkConnection.ConnectionGuid.ToString(), logicalDeviceTag);
                        break;
                    }

                //case IDirectMyRvLinkConnectionTcpIp myRvLinkConnection:
                //    {
                //        var logicalDeviceTag = myRvLinkConnection.MakeLogicalDeviceTagSource();
                //        DeviceSource = new DirectConnectionMyRvLinkTcpIp(logicalDeviceService, myRvLinkConnection, myRvLinkConnection.DeviceSourceToken, logicalDeviceTag);
                //        break;
                //    }

                default:
                    {
                        TaggedLog.Information(LogTag, $"Unable to Start Direct Connection for {connection}");
                        DeviceSource = null;
                        break;
                    }
            }

            if (Connection is IEndPointConnectionBle)
            {
                var deviceSettingsService = App.AppContainer?.Resolve<IDeviceSettingsService>(IfUnresolved.ReturnDefault);
                TaggedLog.Debug(LogTag, $"Enable ble for {Connection}");
                deviceSettingsService?.EnableBluetoothAdapter();
            }
        }
    }
}
