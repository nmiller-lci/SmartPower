using System;
using IDS.Portable.LogicalDevice;

namespace SmartPower.UserInterface.Pairing
{
    public enum ConnectionStep
    {
        Connecting,
        Pairing
    }

    public class AccessoryConnectionResult
    {
        public AccessoryConnectionResult(ILogicalDevice? device)
        {
            Step = ConnectionStep.Pairing;
            IsError = false;
            Device = device;
        }
        public AccessoryConnectionResult(ConnectionStep step)
        {
            Step = step;
            IsError = true;
            Device = null;
        }

        public ConnectionStep Step { get; private set; }
        public bool IsError { get; private set; }
        public ILogicalDevice? Device { get; set; }
    }
}
