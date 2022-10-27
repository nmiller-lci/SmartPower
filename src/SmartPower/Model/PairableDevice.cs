using IDS.Core.IDS_CAN;

namespace SmartPower.Model
{
    public class PairableDevice
    {
        public string? FriendlyName { get; set; }
        public DEVICE_TYPE DeviceType { get; set; }
        public FUNCTION_NAME? FunctionName { get; set; }

        public PairableDevice(DEVICE_TYPE deviceType, FUNCTION_NAME? functionName = null)
        {
            DeviceType = deviceType;
            FriendlyName = functionName?.Name;
            FunctionName = functionName;
        }
    }
}