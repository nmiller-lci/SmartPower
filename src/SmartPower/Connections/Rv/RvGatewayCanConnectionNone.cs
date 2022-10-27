using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using Newtonsoft.Json;

namespace SmartPower.Connections.Rv
{
    public interface IRvDirectConnectionNone : IDirectConnectionNone
    {
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayCanConnectionNone : RvGatewayConnectionBase<RvGatewayCanConnectionNone>, IRvDirectConnectionNone
    {
        static RvGatewayCanConnectionNone() => RegisterJsonSerializer();

        [JsonIgnore]
        public override string ConnectionNameFriendly { get; } = "No Connection";

        [JsonIgnore]
        public override ILogicalDeviceTag LogicalDeviceTagConnection { get; } = new LogicalDeviceTagSourceNone();

        [JsonIgnore]
        public override string DeviceSourceToken { get; }

        public RvGatewayCanConnectionNone()
        {
            DeviceSourceToken = $"None-Connection";
        }
    }
}