using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;
using IDS.Portable.LogicalDevice;
using SmartPower.Services;
using OneControl.Devices;

namespace SmartPower.Services
{
    /// <summary>
    /// This sync container is used to monitor changes to devices to perform operations after the Connection has stabilized.
    /// Currently, the following operations are performed:
    /// 
    ///     1. Take a snapshot if needed
    ///     2. See if we need to update the YMMF info 
    /// 
    /// This SyncContainer is managed by the AppGatewayManagedConnection and is created and destroyed with each CAN Gateway
    /// connection.
    /// </summary>
    class AppDirectConnectionDevicesSyncContainer : AppCollectionSyncContainer<Collection<ILogicalDevice>, ILogicalDevice, ILogicalDevice>
    {
        private readonly IAccessoryGatewayPairingService _accessoryGatewayPairingService;
        public static readonly LogicalDeviceTagFavorite FavoriteTag = new LogicalDeviceTagFavorite();

        private const string LogTag = nameof(AppDirectConnectionDevicesSyncContainer);

        private const int SaveSnapshotDeviceDelayMs = 5000;         // Minimum bus stabilization time
        private const int SaveSnapshotDeviceMaxDelayMs = 20000;     // Maximum bus stabilization time
        private readonly Watchdog _saveSnapshotWatchDog;

        public AppDirectConnectionDevicesSyncContainer(
            IContainerDataSource dataSource,
            IAccessoryGatewayPairingService accessoryGatewayPairingService) : base(dataSource)
        {
            _accessoryGatewayPairingService = accessoryGatewayPairingService;
            _saveSnapshotWatchDog = new Watchdog(SaveSnapshotDeviceDelayMs, SaveSnapshotDeviceMaxDelayMs, CanBusStabilized, autoStartOnFirstPet: true);
        }

        protected override SelectedRvDeviceOptions FilterForSelectedRvDeviceOptions => SelectedRvDeviceOptions.AllDevices;

        protected override Func<ILogicalDevice, bool> CurrentDataSourceFilter => FilterForSelectedRv;

        protected override Func<ILogicalDevice, ILogicalDevice> CurrentViewModelFactory => (logicalDevice) => logicalDevice;

        public override void OnSyncEnd(Collection<ILogicalDevice> collection)
        {
            if (IsDisposed)
                return;

            _saveSnapshotWatchDog?.TryPet(autoReset: true);

            base.OnSyncEnd(collection);
        }

        private void CanBusStabilized()
        {
            // When an accessory device goes offline, it could be because it was unpaired with from the RV from another instance
            // of OneControl or on OCTP. Therefore, check if we have an accessory no-longer paired with the accessory gateway and
            // remove any remnant device source associations. This also should happen on app launch when devices turn online, so
            // accessories that also have direct connection (not through accessory gateway) will also be able to resync.
            Task.Run(async () =>
            {
                try
                {
                    await _accessoryGatewayPairingService.ResyncAccessoryGatewayDevices(CancellationToken.None);
                }
                catch (Exception e)
                {
                    TaggedLog.Warning(LogTag, $"Failed to re-sync with accessory gateway: {e.Message}\n{e.StackTrace}");
                }
            });

            // Go ahead and tank a snapshot because the bus is now stabilized
            //
            CanBusStabilizedTakeSnapshot();
        }

        private void CanBusStabilizedTakeSnapshot()
        {
            if (IsDisposed)
                return;

            AppDirectServices.Instance.TakeSnapshot();
        }

        #region Connection Management
        private bool _connectionStarted = false;

        public void StartConnection()
        {
            _connectionStarted = true;
            _saveSnapshotWatchDog.TryPet(autoReset: true);
        }

        public void StopConnection()
        {
            _connectionStarted = false;
        }
        #endregion

        public override void Dispose(bool disposing)
        {
            _saveSnapshotWatchDog.Dispose();   // We do not null the watchdog as we should never create a new one, and we don't want it restarted!
            base.Dispose(disposing);
        }
    }
}
