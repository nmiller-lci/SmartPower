using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using SmartPower.Connections.Rv;
using SmartPower.Extensions;
using PrismExtensions.Services;
using Xamarin.Forms;

namespace SmartPower.Services
{
    public class DirectConnectionChangedMessage : MessagingCenterMessage<DirectConnectionChangedMessage>
    {
        public static DirectConnectionChangedMessage DefaultMessage { get; } = new DirectConnectionChangedMessage();
        public static void SendMessage() => MessagingCenter.Instance.Send(DefaultMessage);
    }

    public class AppMyRvCloudServiceAvailabilityChangedMessage : MessagingCenterMessage<AppMyRvCloudServiceAvailabilityChangedMessage>
    {
        public static AppMyRvCloudServiceAvailabilityChangedMessage DefaultMessage { get; } = new AppMyRvCloudServiceAvailabilityChangedMessage();
        public static void SendMessage() => MessagingCenter.Instance.Send(DefaultMessage);
    }

    public interface ISdlConnection
    {
        void OnSdlConnected();
        void OnSdlDisconnected();
    }

    public class AppDirectConnectionService : ISdlConnection, IBackgroundOperation
    {
        private const string LogTag = nameof(AppDirectConnectionService);

        private const string UniqueCoachIdentifierKey = "uniqueCoachIdentifier";

        private readonly object _lock = new object();

        private ILogicalDeviceServiceIdsCan LogicalDeviceService => Resolver<ILogicalDeviceServiceIdsCan>.Resolve;

        #region Singleton Pattern
        private static readonly AppDirectConnectionService? _instance = null;
        public static AppDirectConnectionService Instance = _instance ??= new AppDirectConnectionService();

        private AppDirectConnectionService()
        {
            TaggedLog.Debug(LogTag, $"Create {nameof(AppDirectConnectionService)}");
            /* Needed for Singleton Implementation */
        }
        #endregion

        #region Connections
        private AppDirectConnection? _rvDirectConnection = null;
        private AppDirectConnection? _absDirectConnection = null;

        private AppDirectConnectionMonitorPairBond? _rvDirectConnectionMonitorPairBond = null;
        private AppDirectConnectionMonitorPairBond? _absDirectConnectionMonitorPairBond = null;

        private IEnumerable<AppDirectConnection> AllDirectConnection()
        {
            if (_rvDirectConnection != null)
                yield return _rvDirectConnection;

            if (_absDirectConnection != null)
                yield return _absDirectConnection;
        }

        public IRvGatewayConnection? RvCurrentConnection => _rvDirectConnection?.Connection;

        public ILogicalDeviceSource? RvDeviceSource => _rvDirectConnection?.DeviceSource;

        public ConnectionManagerStatus RvConnectionStatus
        {
            get
            {
                if (IsStarted && _rvDirectConnection?.ConnectionStatus == ConnectionManagerStatus.Disconnected)
                    return ConnectionManagerStatus.Connecting;

                return _rvDirectConnection?.ConnectionStatus ?? ConnectionManagerStatus.Disconnected;
            }
        }

        public IRvGatewayConnection? BrakingSystemCurrentConnection => _absDirectConnection?.Connection;
        public ConnectionManagerStatus BrakingSystemConnectionStatus   // Supports both Sway and ABS
        {
            get
            {
                if (IsStarted && _absDirectConnection?.ConnectionStatus == ConnectionManagerStatus.Disconnected)
                    return ConnectionManagerStatus.Connecting;

                return _absDirectConnection?.ConnectionStatus ?? ConnectionManagerStatus.Disconnected;
            }
        }

        public bool IsDisconnect => AllDirectConnection().Any(dc => dc.ConnectionStatus != ConnectionManagerStatus.Disconnected);

        public string RvConnectionId => _rvDirectConnection?.ConnectionId ?? String.Empty;

        private bool IsSameConnection(IRvGatewayConnection? currentConnection, IRvGatewayConnection selectedConnection)
        {
            if (ReferenceEquals(currentConnection, selectedConnection))
                return true;

            if (currentConnection == null && selectedConnection is IDirectConnectionNone and not IRvDirectConnectionDemo)
                return true;

            if (selectedConnection.CompareTo(currentConnection) == 0)
                return true;

            return false;
        }
        #endregion

        #region Background Operation
        public bool IsStarted { get; private set; } = false;

        public void Start()
        {
            lock (_lock)
            {
                IsStarted = true;  // We allow start to happen multiple times because we can start multiple things!

                // If we have a connection to something other then None/Demo, then the connection's can't be the same.  We auto remove one of the connections.
                if (AppSettings.Instance.SelectedRvGatewayConnection is not IRvDirectConnectionNone &&
                    IsSameConnection(AppSettings.Instance.SelectedRvGatewayConnection, AppSettings.Instance.SelectedBrakingSystemGatewayConnection))
                {
                    TaggedLog.Error(LogTag, $"RV and ABS connection can't point to the same connection, auto removing the ABS connection {AppSettings.Instance.SelectedRvGatewayConnection} == {AppSettings.Instance.SelectedBrakingSystemGatewayConnection}.");
                    AppSettings.Instance.SetSelectedBrakingSystemGatewayConnection(AppSettings.DefaultRvDirectConnectionNone, saveSelectedAbs: true);
                    Debug.Assert(false, "RV and ABS connections can't be the same!");
                }

                if (!IsSameConnection(_rvDirectConnection?.Connection, AppSettings.Instance.SelectedRvGatewayConnection))
                {
                    _rvDirectConnection = new(LogicalDeviceService, AppSettings.Instance.SelectedRvGatewayConnection);
                }

                if (!IsSameConnection(_absDirectConnection?.Connection, AppSettings.Instance.SelectedBrakingSystemGatewayConnection))
                {
                    _absDirectConnection = new(LogicalDeviceService, AppSettings.Instance.SelectedBrakingSystemGatewayConnection);
                }

                // Reset the start/stop monitoring as we may need to adjust what we are monitoring
                //
                MonitorConnectionStop();
                MonitorConnectionStart();

                // We reset the PairBond monitor on Start so we can retrieve new PairBond messages. The pair bond
                // monitor will only output ONE pair bond warning per start/stop cycle.
                //
                _rvDirectConnectionMonitorPairBond?.TryDispose();
                _absDirectConnectionMonitorPairBond?.TryDispose();
                _rvDirectConnectionMonitorPairBond = (_rvDirectConnection?.Connection is IEndPointConnectionBle rvBleConnection ? new AppDirectConnectionMonitorPairBond(rvBleConnection) : null);
                _absDirectConnectionMonitorPairBond = (_rvDirectConnection?.Connection is IEndPointConnectionBle absBleConnection ? new AppDirectConnectionMonitorPairBond(absBleConnection) : null);

                // We always include the sensors as they are connectionless
                //
                var newDeviceSources = new List<ILogicalDeviceSourceDirect>(AppSettings.Instance.StandardSharedSensorSources);

                // Add Rv if needed
                //
                if (_rvDirectConnection?.DeviceSource is not null)
                    newDeviceSources.Add(_rvDirectConnection.DeviceSource);

                // Add ABS if needed
                //
                if (_absDirectConnection?.DeviceSource is not null)
                    newDeviceSources.Add(_absDirectConnection.DeviceSource);

                // Start the services that need to be started
                //
                LogicalDeviceService.Start(newDeviceSources);
            }

            // Notify connections may have changed.
            //
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }

        public void Stop()
        {
            lock (_lock)
            {
                // If SDL is connected we ignore STOP requests as we want to keep running
                //
                if (SdlConnected)
                    return;

                StopImpl();
            }

            // Notify connections may have changed.
            //
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }

        private void StopImpl()
        {
            IsStarted = false;

            AppDirectServices.Instance.Stop();

            MonitorConnectionStop();

            LogicalDeviceService.Stop();

            _rvDirectConnectionMonitorPairBond?.TryDispose();
            _rvDirectConnectionMonitorPairBond = null;

            _absDirectConnectionMonitorPairBond?.TryDispose();
            _absDirectConnectionMonitorPairBond = null;
        }

        private void MonitorConnectionStart()
        {
            // Monitor connection changes
            //
            if (_rvDirectConnection?.DeviceSource is ILogicalDeviceSourceDirectConnection rvDirectConnection)
            {
                rvDirectConnection.DidConnectEvent += OnRvDidConnectEvent;
                rvDirectConnection.DidDisconnectEvent += OnRvDidDisconnectEvent;
            }

            if (_absDirectConnection?.DeviceSource is ILogicalDeviceSourceDirectConnection absDirectConnection)
            {
                absDirectConnection.DidConnectEvent += OnAbsDidConnectEvent;
                absDirectConnection.DidDisconnectEvent += OnAbsDidDisconnectEvent;
            }
        }

        private void MonitorConnectionStop()
        {
            // Stop monitoring connection changes  as we are stopping
            //
            if (_rvDirectConnection?.DeviceSource is ILogicalDeviceSourceDirectConnection rvDirectConnection)
            {
                rvDirectConnection.DidConnectEvent -= OnRvDidConnectEvent;
                rvDirectConnection.DidDisconnectEvent -= OnRvDidDisconnectEvent;
            }

            if (_absDirectConnection?.DeviceSource is ILogicalDeviceSourceDirectConnection absDirectConnection)
            {
                absDirectConnection.DidConnectEvent -= OnAbsDidConnectEvent;
                absDirectConnection.DidDisconnectEvent -= OnAbsDidDisconnectEvent;
            }
        }
        #endregion

        #region ISdlConnection
        public bool SdlConnected { get; set; } = false;

        public bool AppSleep { get; set; } = false;


        public void OnSdlConnected()
        {
            SdlConnected = true;

            if (IsDisconnect)
                AppSleep = true;

            Start();

        }

        public void OnSdlDisconnected()
        {
            SdlConnected = false;
            if (AppSleep)
                Stop();
        }
        #endregion

        #region RV Connection Changes
        private void OnRvDidConnectEvent(ILogicalDeviceSourceDirectConnection deviceSource)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_rvDirectConnection?.DeviceSource, deviceSource))
                {
                    TaggedLog.Debug(LogTag, $"{nameof(OnRvDidDisconnectEvent)} IGNORED for {deviceSource} because doesn't match current device source");
                    AppDirectServices.Instance.Stop();
                    return;
                }

                if (!IsStarted)
                {
                    TaggedLog.Debug(LogTag, $"{nameof(OnRvDidDisconnectEvent)} IGNORED for {deviceSource} because Connection Service is now stopped");
                    AppDirectServices.Instance.Stop();
                    return;
                }

                if (_rvDirectConnection?.Connection is null)
                {
                    TaggedLog.Debug(LogTag, $"{nameof(OnRvDidDisconnectEvent)} IGNORED for {deviceSource} because Configured connection is NULL");
                    AppDirectServices.Instance.Stop();
                    return;
                }

                if (_rvDirectConnection?.Connection is IDirectConnectionNone)
                {
                    TaggedLog.Debug(LogTag, $"{nameof(OnRvDidDisconnectEvent)} IGNORED for {deviceSource} because Connection is None");
                    AppDirectServices.Instance.Stop();
                    return;
                }

                TaggedLog.Debug(LogTag, $"RV Connected Event Received for {deviceSource}");
                AppDirectServices.Instance.Start();
            }
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }

        private void OnRvDidDisconnectEvent(ILogicalDeviceSourceDirectConnection deviceSource)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_rvDirectConnection?.DeviceSource, deviceSource))
                {
                    TaggedLog.Debug(LogTag, $"{nameof(OnRvDidDisconnectEvent)} IGNORED for {deviceSource} because doesn't match current device source");
                    return;
                }

                TaggedLog.Debug(LogTag, $"RV Disconnected Event Received for {deviceSource}");
                AppDirectServices.Instance.Stop();
            }
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }
        #endregion

        #region ABS Connection Changes
        private void OnAbsDidConnectEvent(ILogicalDeviceSourceDirectConnection deviceSource)
        {
            TaggedLog.Debug(LogTag, $"ABS Connected Event Received for {deviceSource}");
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }

        private void OnAbsDidDisconnectEvent(ILogicalDeviceSourceDirectConnection deviceSource)
        {
            TaggedLog.Debug(LogTag, $"ABS Disconnected Event Received for {deviceSource}");
            MainThread.RequestMainThreadAction(DirectConnectionChangedMessage.SendMessage);
        }
        #endregion

    }
}
