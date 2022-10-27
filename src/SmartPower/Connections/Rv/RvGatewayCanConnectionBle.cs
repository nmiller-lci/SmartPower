using IDS.Portable.BLE.Platforms.Shared;
using IDS.Portable.Services.ConnectionServices;
using Newtonsoft.Json;
using System;

namespace SmartPower.Connections.Rv
{
    public class RvGatewayCanConnectionBle : RvGatewayConnectionBase<RvGatewayCanConnectionBle>, IDirectIdsCanConnectionBle, IRvGatewayIdsCanConnection
    {
        static RvGatewayCanConnectionBle() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly => "BLE";

        [JsonProperty]
        public string ConnectionId { get; }

        [JsonProperty]
        public string ConnectionPassword { get; }

        [JsonProperty]
        public Guid ConnectionGuid { get; }

        [JsonProperty]
        public BleGatewayInfo.GatewayVersion GatewayVersion { get; }

        [JsonIgnore] 
        public override string DeviceSourceToken => ConnectionGuid.ToString();

        [JsonConstructor]
        public RvGatewayCanConnectionBle(string connectionId, string connectionPassword, Guid connectionGuid, BleGatewayInfo.GatewayVersion gatewayVersion)
        {
            ConnectionId = connectionId;
            ConnectionPassword = connectionPassword;
            ConnectionGuid = connectionGuid;
            GatewayVersion = gatewayVersion;
        }
    }
}
