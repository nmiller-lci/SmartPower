using IDS.Portable.LogicalDevice;
using Newtonsoft.Json;

namespace SmartPower.Connections.Rv
{
    public interface IRvDirectConnectionDemo : IRvDirectConnectionNone
    {
    }
    
    [JsonObject(MemberSerialization.OptIn)]
    public class RvGatewayCanConnectionDemo : RvGatewayConnectionBase<RvGatewayCanConnectionDemo>, IRvDirectConnectionDemo
    {
        static RvGatewayCanConnectionDemo() => RegisterJsonSerializer();
        
        [JsonIgnore]
        public override string ConnectionNameFriendly { get; } = "Demo";

        [JsonIgnore]
        public override ILogicalDeviceTag LogicalDeviceTagConnection { get; } = new LogicalDeviceTagSourceDemo();

        [JsonIgnore] public override string DeviceSourceToken { get; }

        public RvGatewayCanConnectionDemo()
        {
            // Make a GUID that is fixed/constant for a "demo" connection.
            //
            DeviceSourceToken = $"Demo-Connection";
        }
    }
}
