using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using SmartPower.Model;
using SmartPower.Services;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;

namespace SmartPower.UserInterface.Pairing
{
    public class PairWindSensorCellModel: BaseViewModel, IPairMultipleDevicesCell
    {
        private readonly ISessionService _sessionService;
        
        public ConnectionState State { get; set; }
        public DEVICE_TYPE DeviceType => DEVICE_TYPE.AWNING_SENSOR;
        public string DeviceName => DEVICE_TYPE.AWNING_SENSOR.ToString();
        
        public PairWindSensorCellModel(INavigationService navigationService, ISessionService sessionService, List<IPairableDeviceCell> windSensors) : base(navigationService)
        {
            _sessionService = sessionService;
            Devices = new ObservableCollection<IPairableDeviceCell>(windSensors);
            CanSkip = windSensors.Count > 1;
            SetNextWindSensor();
        }
        
        private bool _canSkip;
        public bool CanSkip
        {
            get => _canSkip;
            set => SetProperty(ref _canSkip, value);
        }
        
        private IPairableDeviceCell? _selectedDevice;
        public IPairableDeviceCell? SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        private IList<IPairableDeviceCell> _devices;
        public IList<IPairableDeviceCell> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }
        private FUNCTION_NAME? _functionName;
        public FUNCTION_NAME? FunctionName
        {
            get => _functionName;
            set => SetProperty(ref _functionName, value);
        }

        private void SetSelectedDevice(IPairableDeviceCell device)
        {
            SelectedDevice = device;
        }
        
        public void SetNextWindSensor()
        {
            if (SelectedDevice == null)
            {
                SetSelectedDevice(Devices[0]);
                SelectedDevice.State = ConnectionState.Selected;
                CanSkip = Devices.Count > 1;
                return;
            }
            
            var currentDeviceIndex = Devices.IndexOf(SelectedDevice);
            var nextDeviceIndex = currentDeviceIndex + 1;
            if (nextDeviceIndex <= Devices.Count - 1)
            {
                SetSelectedDevice(Devices[nextDeviceIndex]);
                SelectedDevice.State = ConnectionState.Selected;
                CanSkip = nextDeviceIndex < Devices.Count - 1;
            }
        }
        
        public ICommand SkipWindSensorCommand => new AsyncCommand(async () =>
        {
            if (SelectedDevice == null)
                SetNextWindSensor();
            
            var currentDeviceIndex = Devices.IndexOf(SelectedDevice);
            if (Devices[currentDeviceIndex] == Devices.Last())
                return;
            
            _sessionService.AddLogEntry(DeviceType, SessionState.Skipped, $"Awning sensor '{SelectedDevice.DeviceName}' skipped");
            SelectedDevice.State = ConnectionState.Skipped;
            var nextDeviceIndex = currentDeviceIndex + 1;
            SetSelectedDevice(Devices[nextDeviceIndex]);
            SelectedDevice.State = ConnectionState.Selected;
            CanSkip = nextDeviceIndex < Devices.Count - 1;
        });
    }
}