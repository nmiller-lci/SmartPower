using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;
using IDS.Portable.LogicalDevice.Json;
using IDS.Portable.Services.ConnectionServices;
using Newtonsoft.Json;
using System;
using System.Text;
using OneControl.Direct.MyRvLink;

namespace SmartPower.Connections.Rv
{
    public interface IRvGatewayConnection : IComparable, IJsonSerializable, IEndPointConnectionWithSerialization, IDirectConnection
    {
        ILogicalDeviceTag LogicalDeviceTagConnection { get; }

        string DeviceSourceToken { get; }  // Unique Device Source Id
    }

    public interface IRvGatewayIdsCanConnection : IRvGatewayConnection, IDirectIdsCanConnection
    {
    }

    public interface IRvGatewayMyRvLinkConnection : IRvGatewayConnection, IDirectMyRvLinkConnection
    {
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class RvGatewayConnectionBase<TSerializable> : JsonSerializable<TSerializable>, IRvGatewayConnection
        where TSerializable : class
    {
        public const string LogTag = nameof(RvGatewayConnectionBase<TSerializable>);

        [JsonProperty]
        public string SerializerClass => GetType().Name;        // Use short name because we did a TypeRegistry.Register

        [JsonIgnore]
        public abstract string ConnectionNameFriendly { get; }

        [JsonIgnore]
        public virtual ILogicalDeviceTag LogicalDeviceTagConnection => this.MakeLogicalDeviceTagSource();

        [JsonIgnore]
        public abstract string DeviceSourceToken { get; }

        public virtual int CompareTo(object obj)
        {
            if( obj == null )
                return 1;

            if( ReferenceEquals(this, obj) )
                return 0;

            if( !(obj is IRvGatewayIdsCanConnection objConfiguration) )
                return 1;

            var result = String.CompareOrdinal(ConnectionNameFriendly, objConfiguration.ConnectionNameFriendly);
            if( result != 0 )
                return result;

            if( !LogicalDeviceTagConnection.Equals(objConfiguration.LogicalDeviceTagConnection) )
                return 1;

            // Best we can tell they are the same!
            return 0;
        }

        public override string ToString() => $"'{ConnectionNameFriendly}'";

        // This is used to register/associate a short name with this type.  It's primary purpose is for JSON serialization/deserialization
        // so we can use indirect mappings as opposed to fully qualified names.  It will attempt to auto register, but may also be
        // registered in advance.
        //
        // In theory, we should be able to create a single static class initializer in this base implementation that does the registration for
        // all derived types.  However, this isn't sufficient for the way we are initializing these static constructors.  We are using
        // runtime type info to pre-register these classes so they are available for use by JSON during deserialization.  We do this by calling
        // RuntimeHelpers.RunClassConstructor.  However, that method doesn't seem to invoke the derived classes' static constructor unless the
        // derived class has its own static constructor.  So we make this reusable implementation that can be called by each derived classes'
        // static class constructor.
        //
        protected static void RegisterJsonSerializer()
        {
            Type serializerType = typeof(TSerializable);
            TypeRegistry.Register(serializerType.Name, serializerType);
        }

        /// <summary>
        /// In general, one wouldn't want to generate a guid from a string.  However, this is being done to support backward compatibility
        /// between tags (old way) and DeviceSource (new way) of identifying/grouping a set of Logical Devices.  This is being used in a
        /// way we will have unique Guids within the scope of how this is being used.
        /// </summary>
        /// <param name="rawString">The first 16 bytes of the string will be ASCII encoded into the </param>
        /// <returns>The guid generated from the string</returns>
        protected Guid StringToGuid(string rawString)
        {
            var rawGuid = new byte[ArrayExtension.GuidSize];
            var rawStringBytes = Encoding.ASCII.GetBytes(rawString);
            Buffer.BlockCopy(rawStringBytes, 0, rawGuid, 0, Math.Min(ArrayExtension.GuidSize, rawStringBytes.Length));
            var guid = new Guid(rawGuid);
            return guid;
        }

    }
}