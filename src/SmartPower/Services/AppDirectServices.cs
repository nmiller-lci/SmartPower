using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Utils;
using IDS.Portable.LogicalDevice;
using OneControl.Devices;

namespace SmartPower.Services
{
    /// <summary>
    /// Use of this class requires that an ILogicalDeviceService has been registered via the Resolver.  For example:
    /// 
    ///    Resolver<ILogicalDeviceService>.LazyConstructAndRegister(() => {
    ///        var logicalDeviceService = new LogicalDeviceService();
    ///        logicalDeviceService.RegisterAllLogicalDeviceFactories();
    ///        return logicalDeviceService;
    ///    });
    /// 
    /// </summary>
    public class AppDirectServices : Singleton<AppDirectServices>
    {
        private const string LogTag = nameof(AppDirectServices);

        private readonly AppDirectConnectionDevicesSyncContainer _appSelectedGatewayManagedSyncContainer;

        private AppDirectServices()
        {
            /* Required For Singleton */
            var deviceManager = Resolver<ILogicalDeviceServiceIdsCan>.Resolve?.DeviceManager;
            Debug.Assert(deviceManager != null, $"{nameof(ILogicalDeviceServiceIdsCan)} Not registered");

            _appSelectedGatewayManagedSyncContainer = new AppDirectConnectionDevicesSyncContainer(
                deviceManager,
                new AccessoryGatewayPairingService(deviceManager, this));
        }

        private readonly IReadOnlyCollection<ILogicalDevice> _emptyCollection = new Collection<ILogicalDevice>();

        public IReadOnlyCollection<ILogicalDevice> AppSelectedGatewayDevices => _appSelectedGatewayManagedSyncContainer?.Collection ?? _emptyCollection;

        public bool ManifestFilter(ILogicalDevice logicalDevice)
        {
            var tagManager = logicalDevice?.DeviceService?.DeviceManager?.TagManager;
            if (tagManager == null)
                return false;

            var logicalId = logicalDevice.LogicalId;

            // We currently only include online devices.  We support sending manifest data for offline devices, but
            // this is currently not supported by the cloud as we currently don't have a way for IoT granite to
            // get the online/offline state of a product.
            //
            if (logicalDevice.ActiveConnection == LogicalDeviceActiveConnection.Offline)
                return false;

            // Simulated devices will have a ProductId of unknown, and we don't want to save or load them to the snapshot
            //
            if (logicalDevice is ILogicalDeviceSimulated || logicalId.ProductId == PRODUCT_ID.UNKNOWN)
                return false;

            // We don't ever serialize demo devices, so make sure this isn't a demo device
            //
            if (tagManager.GetTags<ILogicalDeviceTagSourceDemo>(logicalDevice).Any())
                return false;

            if (!AppCollectionSyncContainer.FilterForSelectedRv(logicalDevice, SelectedRvDeviceOptions.ManifestDevices))
                return false;

            return true;
        }

        public void Start()
        {
            AppLocationService.Instance.StartMonitoringLocation();

            var manifestExtension = LogicalDeviceExManifest.SharedExtension;
            if (manifestExtension is null)
                return;

            // Do not auto generate manifest data
            //
            manifestExtension.AutoSaveManifest = false;
            manifestExtension.DeviceFilter = null;          // ManifestFilter
            manifestExtension.EnableDtc = false;

            _appSelectedGatewayManagedSyncContainer.StartConnection();
        }

        public void Stop()
        {
            _appSelectedGatewayManagedSyncContainer.StopConnection();

            var manifestExtension = LogicalDeviceExManifest.SharedExtension;
            if (manifestExtension is not null)
            {
                manifestExtension.DeviceFilter = null;
                manifestExtension.EnableDtc = false;
            }

            AppLocationService.Instance.StopMonitoringLocation();
        }

        #region Snapshot
        public const int MaxSnapshotWarningTimeMs = 250;

        private readonly object _snapshotLock = new object();

        public void TakeSnapshot()
        {
            lock (_snapshotLock)
            {
                using (new PerformanceTimer(LogTag, $"Snapshot Generation", TimeSpan.FromMilliseconds(MaxSnapshotWarningTimeMs), PerformanceTimerOption.AutoStartOnCreate | PerformanceTimerOption.OnShowStopTotalTimeInMs))
                {
                    var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
                    var snapshot = logicalDeviceService.DeviceManager?.TakeSnapshot(SnapshotFilter);
                    if (snapshot == null)
                    {
                        TaggedLog.Warning(LogTag, $"Snapshot unable to be generated!");
                        return;
                    }

                    // We currently don't save the snapshot
                    //AppSettings.Instance.SetDeviceSnapshot(snapshot, autoSave: true);
                }
            }
        }

        // We provide this default SnapshotFilter so we can reuse a single filter.  This snapshot is NOT limited
        // to devices that are part of this current connection.
        //
        public static bool SnapshotFilter(ILogicalDevice logicalDevice)
        {
            var logicalId = logicalDevice.LogicalId;

            // If the device has been disposed then don't include it.
            //
            // This should not happen, as disposed logical devices should be removed by the data source, but as a safety we will filter them here
            // just in case as we don't want to access a disposed logical device.
            //
            if (logicalDevice is null || logicalDevice.IsDisposed)
                return false;

            // Don't include unknown devices in the snapshot.
            //
            if (logicalId.FunctionName == FUNCTION_NAME.UNKNOWN)
                return false;

            // Don't include unknown function classes.
            //
            if (logicalId.FunctionClass == FUNCTION_CLASS.UNKNOWN)
                return false;

            // Phones, OCTPs, and PC Tool use a Miscellaneous function class type and we don't want those included in our 
            // snapshot.  This is because they can have variable MAC addresses which could cause them to make the snapshot
            // file grow indefinitely AND we currently don't need to fast load these devices as there is currently no visual
            // representation of these devices in OneControl.
            //
            if (logicalId.FunctionClass == FUNCTION_CLASS.MISCELLANEOUS)
                return false;

            // Simulated devices will have a ProductId of unknown, and we don't want to save or load them to the snapshot
            //
            if (logicalDevice is ILogicalDeviceSimulated || logicalId.ProductId == PRODUCT_ID.UNKNOWN)
            {
                //TaggedLog.Debug(LogTag, $"SnapshotFilter {logicalId} FILTERED (unknown product_id)" );
                return false;
            }

            // Don't allow demo devices
            //
            var tagManager = logicalDevice?.DeviceService?.DeviceManager?.TagManager;
            if (tagManager != null)
            {
                // We don't ever serialize demo devices, so make sure this isn't a demo device
                //
                if (tagManager.GetTags<ILogicalDeviceTagSourceDemo>(logicalDevice).Any())
                    return false;
            }

            return true;
        }
        #endregion

    }

}
