using IDS.Portable.Common;
using SmartPower.Connections.Rv;
using SmartPower.Services;
using OneControl;

namespace SmartPower
{
    public interface IAppSettingsConnectionAbs
    {
        IRvGatewayConnection SelectedBrakingSystemGatewayConnection { get; }  // Used for both SWAY and ABS
        void SetSelectedBrakingSystemGatewayConnection(IRvGatewayConnection selectedAbs, bool saveSelectedAbs);
    }
    
    public partial class AppSettings : IAppSettingsConnectionAbs
    {
        public IRvGatewayConnection SelectedBrakingSystemGatewayConnection { get; private set; } = DefaultRvDirectConnectionNone;
        public void SetSelectedBrakingSystemGatewayConnection(IRvGatewayConnection selectedAbs, bool saveSelectedAbs) => SetSelectedBrakingSystemGatewayConnection(selectedAbs, saveSelectedAbs, notifyChanged: true);

        private void SetSelectedBrakingSystemGatewayConnection(IRvGatewayConnection? selectedAbs, bool saveSelectedAbs, bool notifyChanged)
        {
            var newSelectedAbs = selectedAbs ?? DefaultRvDirectConnectionNone;

            if (SelectedBrakingSystemGatewayConnection == newSelectedAbs)
                return;

            TaggedLog.Information(LogTag, $"{nameof(SetSelectedBrakingSystemGatewayConnection)} selecting {newSelectedAbs}");

            SelectedBrakingSystemGatewayConnection = newSelectedAbs;

            if (newSelectedAbs is RvGatewayCanConnectionDemo)
                AppDemoMode.DefaultDemoDeviceSource.RegisterDevices();

            AppDirectConnectionService.Instance.Start();

            if (saveSelectedAbs)
                Save(SelectedRvGatewayConnection, newSelectedAbs);

            if (notifyChanged)
                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
        }

    }
}
