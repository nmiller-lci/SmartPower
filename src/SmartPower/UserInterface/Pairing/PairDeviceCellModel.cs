using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using IDS.Core.IDS_CAN;
using IDS.Portable.LogicalDevice;
using IDS.UI.UserDialogs;
using SmartPower.UserInterface.CollectionCells;
using Prism.Navigation;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.Pairing
{
    public class PairDeviceCellModel : BaseViewModel, IPairableDeviceCell
    {
        private readonly IUserDialogService _dialogService;

        public PairDeviceCellModel(INavigationService navigationService, IUserDialogService dialogService) : base(navigationService)
        {
            _dialogService = dialogService;
        }

        public ICommand ShowErrorCommand => new Command<PairDeviceCellModel>( ShowErrorAsync, CanExecuteShowError);

        #region Properties

        private ILogicalDevice? _device;
        public ILogicalDevice? Device
        {
            get => _device;
            set => SetProperty(ref _device, value);
        }
        
        private string _deviceName;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }
        
        private DEVICE_TYPE _deviceType;
        public DEVICE_TYPE DeviceType
        {
            get => _deviceType;
            set => SetProperty(ref _deviceType, value);
        }
        
        private ConnectionState _state;
        public ConnectionState State
        {
            get => _state;
            set => SetProperty(ref _state, value);
        }

        private AccessoryConnectionResult? _connectionResult;
        public AccessoryConnectionResult? ConnectionResult
        {
            get => _connectionResult;
            set => SetProperty(ref _connectionResult, value);
        }
        
        private FUNCTION_NAME? _functionName;
        public FUNCTION_NAME? FunctionName
        {
            get { return _functionName; }
            set => SetProperty(ref _functionName, value);
        }


        #endregion

        private void ShowErrorAsync(PairDeviceCellModel pairDeviceCell)
        {
            string errorMessage;
            string title;
            
            if (pairDeviceCell.ConnectionResult != null)
            {
                switch (pairDeviceCell.ConnectionResult.Step)
                {
                    case ConnectionStep.Connecting:
                        errorMessage = Resources.Strings.device_connecting_error;
                        title = Resources.Strings.device_connecting_error_title;
                        break;
                    case ConnectionStep.Pairing:
                        errorMessage = Resources.Strings.device_pairing_error;
                        title = Resources.Strings.device_pairing_error_title;
                        break;
                    default:
                        return;
                }
            }
            else
            {
                errorMessage = Resources.Strings.device_connecting_error;
                title = Resources.Strings.device_connecting_error_title;
            }

            _dialogService.AlertAsync(title, errorMessage, Resources.Strings.ok, ResumePauseCancellationToken);
        }

        private bool CanExecuteShowError(object? obj)
        {
            var pairDeviceCell = obj as PairDeviceCellModel;
            return pairDeviceCell is { State: ConnectionState.Error };
        }
    }
}