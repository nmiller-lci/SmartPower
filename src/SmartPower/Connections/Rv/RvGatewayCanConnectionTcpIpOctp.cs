using IDS.Portable.CAN;
using IDS.Portable.Services.ConnectionServices;
using Newtonsoft.Json;

namespace SmartPower.Connections.Rv
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayCanConnectionTcpIpOctp : RvGatewayConnectionBase<RvGatewayCanConnectionTcpIpOctp>, IDirectIdsCanConnectionTcpIpWired, IRvGatewayIdsCanConnection
    {
        static RvGatewayCanConnectionTcpIpOctp() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly => "OCTP";

        [JsonIgnore]
        public string ConnectionIpAddress { get; }

        [JsonIgnore]
        public override string DeviceSourceToken { get; }

        [JsonConstructor]
        public RvGatewayCanConnectionTcpIpOctp()
        {
            ConnectionIpAddress = CanAdapterFactory.DefaultLocalhostIpAddress.ToString();

            // Make a GUID that is fixed/constant for the OCTP.
            //
            DeviceSourceToken = "OCTP-TCP-IP";
        }
    }
}