using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using ZXing;

namespace SmartPower.UserInterface.ScanVin
{
    public class ScanVinViewModel : BaseViewModel, IViewModelResumePause
    {
        public static readonly string VinParameterKey = "VIN";

        public ScanVinViewModel(INavigationService navigationService): base(navigationService)
        {

        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }
        
        private string? _scanResult;
        public string? ScanResult
        {
            get => _scanResult;
            set
            {
                SetProperty(ref _scanResult, value);
                _confirmResult?.RaiseCanExecuteChanged();
            }
        }

        public IEnumerable<BarcodeFormat> PossibleFormats { get; } = new List<BarcodeFormat> { BarcodeFormat.CODE_128 };

        private AsyncCommand? _confirmResult;
        public ICommand ConfirmResult => _confirmResult ??= new AsyncCommand(async () =>
        {
            var parameters = new NavigationParameters
            {
                { VinParameterKey, ScanResult }
            };

            await NavigationService.GoBackAsync(parameters);
        }, _ => !string.IsNullOrEmpty(ScanResult), allowsMultipleExecutions: false);

        private bool _hasCameraPermissions;
        public bool HasCameraPermissions
        {
            get => _hasCameraPermissions;
            set => SetProperty(ref _hasCameraPermissions, value);
        }

        private ICommand? _scanResultCommand;
        public ICommand ScanResultCommand => _scanResultCommand ??= new Command<Result>(result => ScanResult = result.Text);

        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            var cameraPermission = await Permissions.CheckStatusAsync<Permissions.Camera>();

            if (cameraPermission is PermissionStatus.Denied)
            {
                // todo: decide what to do when user has denied permission
            }

            if (cameraPermission is not PermissionStatus.Granted)
            {
                cameraPermission = await Permissions.RequestAsync<Permissions.Camera>();
            }

            HasCameraPermissions = cameraPermission is PermissionStatus.Granted;

            IsScanning = true;
        }

        public void OnPause(PauseReason reason)
        {
            IsScanning = false;
        }
    }
}
