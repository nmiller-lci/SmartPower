using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.ObservableCollection;
using IDS.Portable.Devices.TPMS;
using IDS.Portable.LogicalDevice;
using SmartPower.Connections.Rv;
using SmartPower.Extensions;
using OneControl;
using OneControl.Devices;
using OneControl.Devices.AccessoryGateway;
using OneControl.Devices.AwningSensor;
using OneControl.Devices.BrakingSystem;
using OneControl.Devices.TemperatureSensor;
using OneControl.Devices.TextDevice;
using Xamarin.Forms;

namespace SmartPower
{
    [Flags]
    public enum SelectedRvDeviceOptions
    {
        WithNames = 0x01,
        WithCategories = 0x02,
        WithValidConfiguration = 0x04,
        WithoutDemoDevices = 0x08,
        WithoutCanDevices = 0x10,
        WithoutRemoteDevices = 0x20,
        WithoutMiscellaneousDevices = 0x40, // such as phones

        AllDevices = 0x00,                                                                                      // All devices that match the selected RV
        UserVisibleDevices = WithNames | WithCategories | WithValidConfiguration | WithoutMiscellaneousDevices, // All devices that match the selected RV with names, categories, and valid configurations (aka user visible devices)
        ManifestDevices = AllDevices,                                                                           // Devices included in the Manifest
    }

    // We use to call this CollectionSyncContainerOneControl, but with the V3 refactor it fit the App* naming pattern used by other App level functionality.
    //
    public class AppCollectionSyncContainer<TCollection, TDataModel, TViewModel> : CollectionSyncContainer<TCollection, TDataModel, TViewModel>
        where TCollection : ICollection<TViewModel>, new()
        where TViewModel : class
    {
        private const string LogTag = "AppCollectionSyncContainer";

        private ILogicalDeviceTag? _canTag = null;
        private readonly HashSet<ILogicalDeviceTag> _filterTagList = new HashSet<ILogicalDeviceTag>();

        private readonly object _lockObject = new object();

        // These constructors allow the sync container to be used w/o having to create a custom derived class.  The factory and filter can be passed in as lambda expressions
        //
        #region Constructors - That don't require a derived class 
        public AppCollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter) :
            base(collection, dataSource, viewModelFactory, dataSourceFilter)
        {
            Init();
        }

        public AppCollectionSyncContainer(IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter) :
            base(dataSource, viewModelFactory, dataSourceFilter)
        {
            Init();
        }

        public AppCollectionSyncContainer(TCollection collection, IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter) :
            base(collection, dataSource, viewModelFactory, dataSourceFilter)
        {
            Init();
        }

        public AppCollectionSyncContainer(IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory, Func<TDataModel, bool> dataSourceFilter) :
            base(dataSource, viewModelFactory, dataSourceFilter)
        {
            Init();
        }
        #endregion

        // These constructors are intended to be used when a derived class is defining their own CurrentDataSourceFilter override so they don't need to pass in a dataSourceFilter.
        //
        #region Constructors  - Require Override of CurrentDataSourceFilter
        protected AppCollectionSyncContainer(TCollection collection, IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory) : base(collection, dataSource, viewModelFactory)
        {
            Init();
        }

        protected AppCollectionSyncContainer(IContainerDataSource dataSource, Func<TDataModel, TViewModel> viewModelFactory) : base(dataSource, viewModelFactory)
        {
            Init();
        }

        protected AppCollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory) : base(collection, dataSource, viewModelFactory)
        {
            Init();
        }

        protected AppCollectionSyncContainer(IContainerDataSource<TDataModel> dataSource, Func<TDataModel, TViewModel> viewModelFactory) : base(dataSource, viewModelFactory)
        {
            Init();
        }
        #endregion

        // These constructors are intended to be used when a derived class is defining their own CurrentDataSourceFilter AND CurrentViewModelFactory override so they don't need
        // to pass in a dataSourceFilter or viewModelFactory.
        //
        #region Constructors - Require Override of CurrentDataSourceFilter AND CurrentViewModelFactory
        protected AppCollectionSyncContainer(TCollection collection, IContainerDataSource dataSource) : base(collection, dataSource)
        {
            Init();
        }

        protected AppCollectionSyncContainer(IContainerDataSource dataSource) : base(dataSource)
        {
            Init();
        }

        protected AppCollectionSyncContainer(TCollection collection, IContainerDataSource<TDataModel> dataSource) : base(collection, dataSource)
        {
            Init();
        }

        protected AppCollectionSyncContainer(IContainerDataSource<TDataModel> dataSource) : base(dataSource)
        {
            Init();
        }
        #endregion

        // Should only ever call this one!
        private void Init()
        {
            UpdateFilterTags();
            if (AutoDataSourceSyncOnConstruction)
                DataSourceSync();

            MessagingCenter.Instance.SubscribeOnMainThread<AppSelectedRvUpdateMessage>(this, (sender) =>
            {
                UpdateFilterTags();
                DataSourceSync();
            });
        }

        public override void Dispose(bool disposing)
        {
            MessagingCenter.Instance.TryUnsubscribe<AppSelectedRvUpdateMessage>(this);

            lock (_lockObject)
            {
                _filterTagList.Clear();
            }

            base.Dispose(disposing);
        }

        private void UpdateFilterTags()
        {
            lock (_lockObject)
            {
                _filterTagList.Clear();

                _canTag = AppSettings.Instance.SelectedRvGatewayConnection?.LogicalDeviceTagConnection;

                if (_canTag != null && !AppSettings.Instance.SelectedRvHideDevicesFromGatewayCan)
                {
                    //TaggedLog.Debug(LogTag, $"Setup RV filter for canTag: {_canTag}");
                    _filterTagList.Add(_canTag);
                }
            }
        }

        protected virtual SelectedRvDeviceOptions FilterForSelectedRvDeviceOptions => SelectedRvDeviceOptions.UserVisibleDevices;

        public bool FilterForSelectedRv(ILogicalDevice? logicalDevice)
        {
            var deviceOptions = FilterForSelectedRvDeviceOptions;

            Debug.Assert(!deviceOptions.HasFlag(SelectedRvDeviceOptions.WithoutCanDevices), $"{nameof(SelectedRvDeviceOptions.WithoutCanDevices)} not supported by {nameof(FilterForSelectedRv)}");
            Debug.Assert(!deviceOptions.HasFlag(SelectedRvDeviceOptions.WithoutRemoteDevices), $"{nameof(SelectedRvDeviceOptions.WithoutCanDevices)} not supported by {nameof(FilterForSelectedRv)}");

            if (logicalDevice == null)
                return false;

            var logicalDeviceService = logicalDevice.DeviceService;

            if (!AppCollectionSyncContainer.PassesBasicFilterChecks(logicalDevice, deviceOptions))
                return false;

            // If the is associated with one of the active Device Sources then we allow the device
            //
            // Currently, OneControl doesn't really support multiple RV.  This filter check is a short circuit on having to check the
            // individual tags.  However, for sensors such as temperature and mopeka they all share the same device source so ANY
            // Mopeka or Temperature sensor will be shown (very similar to Tag Any items).
            // 
            if (logicalDevice.IsAssociatedWithDeviceSource(logicalDeviceService.DeviceSourceManager.DeviceSources))
                return true;

            // Get the tag manager so we can see if the device should be included because of tags.  This is for backward compatibility, before
            // we have devices sources and the ability to have sensors as device sources.
            //
            var tagManager = logicalDeviceService.DeviceManager?.TagManager;
            if (tagManager == null)
                return false;

            lock (_lockObject)
            {
                // If there are no filter tags then there is nothing to match
                //
                if (_filterTagList.Count == 0)
                    return false;

                // Show only devices that are tagged for our selected tags or tagged with any
                //
                var includeLogicalDevice = tagManager.ContainsAnyMatchingTag(_filterTagList, logicalDevice) || tagManager.GetTags<ILogicalDeviceTagSourceAny>(logicalDevice).Any();

                return includeLogicalDevice;
            }

        }
        // Is the device supported (can we make a cell representation for it)?
        public virtual bool IsDeviceSupported(ILogicalDevice logicalDevice)
        {
            switch (logicalDevice)
            {
                case ILogicalDeviceSwitchableLight _:
                //case ILogicalDeviceGeneratorGenie _:
                case ILogicalDeviceRelayHBridge _:
                //case ILogicalDeviceClimateZone _:
                //case LogicalDeviceLevelerType1 _:
                //case LogicalDeviceLevelerType3 _:
                //case LogicalDeviceLevelerType4 _:
                //case LogicalDeviceTankSensor _:
                case ILogicalDeviceSwitchable _:
                //case ILogicalDeviceRouterWifi _:
                //case ILogicalDeviceCamera _:
                case ILogicalDeviceTirePressureMonitor _:
                case ILogicalDevicePowerMonitor _:         // Such as ILogicalDeviceBatteryMonitor
                case ILogicalDeviceAwningSensor _:
                //case ILogicalDeviceTemperatureSensor _:
                case ILogicalDeviceBrakingSystem _:
                //case ILogicalDeviceText _:
                case ILogicalDeviceAccessoryGateway _:
                    return true;

                default:
                    return false;
            }
        }
    }

    public static class AppCollectionSyncContainer
    {
        /// <summary>
        /// Checks to see if the given logicalDevice passes the "BASIC" filter checks as specified by the given deviceOptions.
        /// If it doesn't then the LogicalDevice shouldn't be included.  Not all SelectedRvDeviceOptions are checked by this
        /// method.
        /// </summary>
        /// <param name="logicalDevice"></param>
        /// <param name="deviceOptions"></param>
        /// <returns></returns>
        public static bool PassesBasicFilterChecks(ILogicalDevice? logicalDevice, SelectedRvDeviceOptions deviceOptions)
        {
            var tagManager = logicalDevice?.DeviceService?.DeviceManager?.TagManager;
            if (tagManager == null)
                return false;

            // If the device has been disposed then don't include it.
            //
            // This should not happen, as disposed logical devices should be removed by the data source, but as a safety we will filter them here
            // just in case as we don't want to access a disposed logical device.
            //
            if (logicalDevice?.IsDisposed ?? true)
            {
                TaggedLog.Error(nameof(FilterForSelectedRv), $"{nameof(FilterForSelectedRv)}: Logical Device is DISPOSED and is being ignored {logicalDevice}");
                return false;
            }

            // This device isn't named so we won't show it
            //
            if (deviceOptions.HasFlag(SelectedRvDeviceOptions.WithNames) && logicalDevice.LogicalId.FunctionName == FUNCTION_NAME.UNKNOWN)
            {
                if (!IsUnnamedDeviceAllowed(logicalDevice))
                    return false;
            }

            // This device doesn't have a know category so we won't show it
            //
            if (deviceOptions.HasFlag(SelectedRvDeviceOptions.WithCategories))
            {
                var appCategory = DeviceCategory.GetDeviceCategory(logicalDevice.LogicalId);
                if (appCategory == DeviceCategory.Unknown)
                    return false;
            }

            // We only show HVAC's that have a valid configuration
            //
            if (deviceOptions.HasFlag(SelectedRvDeviceOptions.WithValidConfiguration))
            {
                if (logicalDevice is LogicalDeviceClimateZone climateZone && !climateZone.DeviceCapability.IsValid)
                    return false;
            }

            // Is this a demo device
            //
            var isDemoDevice = logicalDevice is ILogicalDeviceSimulated;
            if (isDemoDevice)
            {
                if (!(AppSettings.Instance.SelectedRvGatewayConnection is IRvDirectConnectionDemo) || deviceOptions.HasFlag(SelectedRvDeviceOptions.WithoutDemoDevices))
                    return false;
                else
                {
                    var hasDemoTag = tagManager.ContainsTag(AppDemoMode.DefaultDemoTag, logicalDevice);
                    return hasDemoTag;
                }
            }

            // Is this a text device then go ahead and show it
            //
            if (logicalDevice is ILogicalDeviceText)
                return true;

            // If we are in demo mode, only show demo devices
            //
            return !(AppSettings.Instance.SelectedRvGatewayConnection is IRvDirectConnectionDemo);
        }

        private static bool IsUnnamedDeviceAllowed(ILogicalDevice logicalDevice)
        {
            return logicalDevice.LogicalId.DeviceType == DEVICE_TYPE.ACCESSORY_GATEWAY;
        }

        /// <summary>
        /// This is a less efficient version of FilterForSelectedRv, but can be used without having a CollectionSyncContainer and it contains
        /// more flexible filter options.
        /// </summary>
        /// <param name="logicalDevice"></param>
        /// <param name="deviceOptions"></param>
        /// <returns></returns>
        public static bool FilterForSelectedRv(ILogicalDevice? logicalDevice, SelectedRvDeviceOptions deviceOptions)
        {
            if (logicalDevice == null)
                return false;

            if (!PassesBasicFilterChecks(logicalDevice, deviceOptions))
                return false;

            var logicalDeviceService = logicalDevice.DeviceService;

            // If the is associated with one of the active Device Sources then we allow the device
            //
            // Currently, OneControl doesn't really support multiple RV.  This filter check is a short circuit on having to check the
            // individual tags.  However, for sensors such as temperature and mopeka they all share the same device source so ANY
            // Mopeka or Temperature sensor will be shown (very similar to Tag Any items).
            // 
            if (logicalDevice.IsAssociatedWithDeviceSource(logicalDeviceService.DeviceSourceManager.DeviceSources))
                return true;

            // Get the tag manager so we can see if the device should be included because of tags.  This is for backward compatibility, before
            // we have devices sources and the ability to have sensors as device sources.
            //
            var tagManager = logicalDeviceService.DeviceManager?.TagManager;
            if (tagManager is null)
                return false;

            // See if the device should be included because it has the any tag.
            //
            if (tagManager.GetTags<ILogicalDeviceTagSourceAny>(logicalDevice).Any())
                return true;

            // See if the device should be included because of a match CAN tag
            //
            if (!AppSettings.Instance.SelectedRvHideDevicesFromGatewayCan && !deviceOptions.HasFlag(SelectedRvDeviceOptions.WithoutCanDevices))
            {
                var canTag = AppSettings.Instance.SelectedRvGatewayConnection.LogicalDeviceTagConnection;
                if (canTag is null)
                    return false;
                var includeLogicalDevice = tagManager.ContainsTag(canTag, logicalDevice);
                return includeLogicalDevice;
            }

            // Phones, OCTPs, and PC Tool use a Miscellaneous function class type and we don't want those included in our 
            // snapshot.  This is because they can have variable MAC addresses which could cause them to make the snapshot
            // file grow indefinitely AND we currently don't need to fast load these devices as there is currently no visual
            // representation of these devices in OneControl.
            //
            if (deviceOptions.HasFlag(SelectedRvDeviceOptions.WithoutMiscellaneousDevices) && logicalDevice.LogicalId.FunctionClass == FUNCTION_CLASS.MISCELLANEOUS)
                return false;

            return false;
        }
    }
}
