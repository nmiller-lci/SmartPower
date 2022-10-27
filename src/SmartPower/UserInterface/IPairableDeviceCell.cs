using System.ComponentModel;
using IDS.Core.IDS_CAN;

namespace SmartPower.UserInterface
{
    public interface IPairableDeviceCell: INotifyPropertyChanged
    {
        ConnectionState State { get; set; }
        DEVICE_TYPE DeviceType { get; }
        public FUNCTION_NAME? FunctionName { get; set; }
        string DeviceName { get; }
    }
}