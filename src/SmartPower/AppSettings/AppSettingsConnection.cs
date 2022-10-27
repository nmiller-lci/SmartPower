using System;
using IDS.Portable.Common;
using SmartPower.Connections.Rv;
using SmartPower.Services;
using OneControl;

namespace SmartPower
{
    public interface IAppSettingsConnection
    {
        IRvGatewayConnection SelectedRvGatewayConnection { get; }
        void SetSelectedRvGatewayConnection(IRvGatewayConnection? selectedRv, bool saveSelectedRv);

        bool SelectedRvHideDevicesFromGatewayCan { get; }
        void SetSelectedRvHideDevicesFromGatewayCan(bool hideDevices, bool autoSave = true);

        bool SelectedRvHideDevicesFromRemote { get; }
        void SelectedRvSetHideDevicesFromRemote(bool hideDevices, bool autoSave = true);
    }

    public partial class AppSettings
    {
        public static readonly RvGatewayCanConnectionTcpIpOctp DefaultRvDirectIdsCanConnectionOctp = new RvGatewayCanConnectionTcpIpOctp();

        public static readonly RvGatewayCanConnectionDemo DefaultRvDirectConnectionDemo = new RvGatewayCanConnectionDemo();

        public static readonly RvGatewayCanConnectionNone DefaultRvDirectConnectionNone = new RvGatewayCanConnectionNone();

        public IRvGatewayConnection SelectedRvGatewayConnection { get; private set; } = DefaultRvDirectConnectionNone;
        public void SetSelectedRvGatewayConnection(IRvGatewayConnection? selectedRv, bool saveSelectedRv) => SetSelectedRvGatewayConnection(selectedRv, saveSelectedRv, notifyChanged: true);

        private void SetSelectedRvGatewayConnection(IRvGatewayConnection? selectedRv, bool saveSelectedRv, bool notifyChanged)
        {
            var newSelectedRv = selectedRv ?? DefaultRvDirectConnectionNone;

            if (SelectedRvGatewayConnection == newSelectedRv)
                return;

            TaggedLog.Information(LogTag, $"{nameof(SetSelectedRvGatewayConnection)} selecting {newSelectedRv}");

            SelectedRvGatewayConnection = newSelectedRv;

            if (newSelectedRv is RvGatewayCanConnectionDemo)
                AppDemoMode.DefaultDemoDeviceSource.RegisterDevices();

            AppDirectConnectionService.Instance.Start();

            if (saveSelectedRv)
                Save(newSelectedRv, SelectedBrakingSystemGatewayConnection);

            if (notifyChanged)
                AppSelectedRvUpdateMessage.SendMessage(); // Lets others know the configuration changed
        }

        public bool SelectedRvHideDevicesFromGatewayCan { get; private set; }

        public void SetSelectedRvHideDevicesFromGatewayCan(bool hideDevices, bool autoSave = true)
        {
            if (SelectedRvHideDevicesFromGatewayCan == hideDevices)
                return;

            TaggedLog.Information(LogTag, $"{nameof(SetSelectedRvHideDevicesFromGatewayCan)} hide devices = {hideDevices}");

            SelectedRvHideDevicesFromGatewayCan = hideDevices;
            if (autoSave)
                Save();
        }

        public bool SelectedRvHideDevicesFromRemote { get; private set; }

        public void SelectedRvSetHideDevicesFromRemote(bool hideDevices, bool autoSave = true)
        {
            if (SelectedRvHideDevicesFromRemote == hideDevices)
                return;

            TaggedLog.Information(LogTag, $"{nameof(SelectedRvSetHideDevicesFromRemote)} hide devices = {hideDevices}");

            SelectedRvHideDevicesFromRemote = hideDevices;
            if (autoSave)
                Save();
        }
    }
}
