using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.Portable.Services.ConnectionServices;
using OneControl.Direct.MyRvLinkBle;

namespace SmartPower.Connections.Rv
{
    public static class DirectConnectionExtension
    {
        /// <summary>
        /// Creates a Tag source for the Given gateway connection.  A tag source is useful for logical devices and for comparing connection sources.
        /// </summary>
        /// <param name="gatewayConnection">A gateway connection</param>
        /// <returns>An appropriate ILogicalDeviceTag for the given connection or NULL if there was no compatible tag type.</returns>
        public static ILogicalDeviceTag MakeLogicalDeviceTagSource(this IDirectConnection gatewayConnection)
        {
            switch( gatewayConnection )
            {
                case IDirectIdsCanConnectionBle bleConnection:
                    return (ILogicalDeviceTag)new LogicalDeviceTagSourceBle(bleConnection.ConnectionId ?? $"BleV{bleConnection.GatewayVersion}");

                case IDirectIdsCanConnectionTcpIpWifi wifiConnection:
                    return new LogicalDeviceTagSourceWifi(wifiConnection.ConnectionSsid ?? $"WiFi");

                case IDirectIdsCanConnectionTcpIpWired wiredConnection:
                    return new LogicalDeviceTagSourceDirect(wiredConnection.ConnectionIpAddress ?? $"Direct");

                case IDirectMyRvLinkConnectionBle myRvLinkBleConnection:
                    return new LogicalDeviceTagSourceMyRvLinkBle(myRvLinkBleConnection.ConnectionId, myRvLinkBleConnection.ConnectionGuid);

                default:
                    return null;
            }
        }
    }
}