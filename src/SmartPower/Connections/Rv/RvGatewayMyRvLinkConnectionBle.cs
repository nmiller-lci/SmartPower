using Newtonsoft.Json;
using OneControl.Direct.MyRvLinkBle;
using System;

namespace SmartPower.Connections.Rv
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayMyRvLinkConnectionBle : RvGatewayConnectionBase<RvGatewayMyRvLinkConnectionBle>, IDirectMyRvLinkConnectionBle, IRvGatewayMyRvLinkConnection
    {
        static RvGatewayMyRvLinkConnectionBle() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly
        {
            get
            {
                switch( GatewayType )
                {
                    case RvLinkGatewayType.AntiLockBraking:
                        return "ABS";

                    case RvLinkGatewayType.Sway:
                        return "SWAY";

                    case RvLinkGatewayType.Unknown:
                    case RvLinkGatewayType.Gateway:
                        return "BLE";

                    default:
                        return "BLE-UNKNOWN";
                }
            }
        }

        [JsonProperty]
        public string ConnectionId { get; }

        [JsonProperty]
        public Guid ConnectionGuid { get; }

        [JsonIgnore]
        public RvLinkGatewayType GatewayType => MyRvLinkBleGatewayScanResult.GatewayTypeFromDeviceName(ConnectionId);

        [JsonIgnore]
        public override string DeviceSourceToken => ConnectionGuid.ToString();

        [JsonConstructor]
        public RvGatewayMyRvLinkConnectionBle(Guid connectionGuid, string connectionId)
        {
            ConnectionId = connectionId;
            ConnectionGuid = connectionGuid;
        }
    }
}