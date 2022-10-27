using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SmartPower.UserInterface.Pairing
{
    public interface IPairMultipleDevicesCell: IPairableDeviceCell
    {
        IList<IPairableDeviceCell> Devices { get; }
        IPairableDeviceCell SelectedDevice { get; }
    }
}