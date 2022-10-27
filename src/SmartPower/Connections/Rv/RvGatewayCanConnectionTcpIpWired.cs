using IDS.Portable.CAN;
using IDS.Portable.Services.ConnectionServices;
using Newtonsoft.Json;
using System;

namespace SmartPower.Connections.Rv
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayCanConnectionTcpIpWired : RvGatewayConnectionBase<RvGatewayCanConnectionTcpIpWired>, IDirectIdsCanConnectionTcpIpWired, IRvGatewayIdsCanConnection
    {
        static RvGatewayCanConnectionTcpIpWired() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly => "Direct Connection";

        [JsonProperty]
        public string ConnectionIpAddress { get; }

        [JsonIgnore]
        public override string DeviceSourceToken { get; }

        [JsonConstructor]
        public RvGatewayCanConnectionTcpIpWired(string connectionIpAddress)
        {
            ConnectionIpAddress = connectionIpAddress ?? CanAdapterFactory.DefaultLocalhostIpAddress.ToString();

            // Make a GUID that is based on the string "Wired{connectionIpAddress}".
            //
            DeviceSourceToken = $"Wired{connectionIpAddress ?? String.Empty}";
        }
    }
}