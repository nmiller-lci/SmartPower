using IDS.Portable.CAN;
using IDS.Portable.Services.ConnectionServices;
using Newtonsoft.Json;

namespace SmartPower.Connections.Rv
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayCanConnectionTcpIpWifiGateway : RvGatewayConnectionBase<RvGatewayCanConnectionTcpIpWifiGateway>, IDirectIdsCanConnectionTcpIpWifi, IRvGatewayIdsCanConnection
    {
        static RvGatewayCanConnectionTcpIpWifiGateway() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly => ConnectionSsid;

        [JsonIgnore]
        public string ConnectionIpAddress { get; }

        [JsonProperty]
        public string ConnectionSsid { get; }

        [JsonProperty]
        public string ConnectionPassword { get; }

        [JsonIgnore]
        public override string DeviceSourceToken { get; }

        [JsonConstructor]
        public RvGatewayCanConnectionTcpIpWifiGateway(string connectionSsid, string connectionPassword)
        {
            ConnectionIpAddress = CanAdapterFactory.DefaultMyrvGatewayIpAddress.ToString();
            ConnectionSsid = connectionSsid;
            ConnectionPassword = connectionPassword;

            // THE IP Address of our Wifi gateways tend to be fixed, so we use the SSID to make something
            // that is more unique.
            //
            DeviceSourceToken = $"{ConnectionSsid ?? ConnectionIpAddress ?? "DefaultWiFi"}";
        }
    }
}