using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using IDS.UI.UserDialogs;

using SmartPower.Resources;
using SmartPower.Services;

using SmartPower.UserInterface.Pairing;
using SmartPower.UserInterface.ScanVin;
using SmartPower.UserInterface.Settings;

using Prism.Commands;
using Prism.Navigation;

using PrismExtensions.Enums;
using PrismExtensions.ViewModels;

using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace SmartPower.UserInterface.VIN
{
    public class VinAndFloorPlanViewModel : BaseViewModel, IViewModelResumePause
    {
        private const int VinLength = 17;
        public static readonly string FloorPlanKey = "FPK";

        private readonly ISessionService _sessionService;
        private readonly IUserDialogService _userDialogService;
        private readonly IBundledDataService _bundledDataService;

        private string? _vin;
        private IEnumerable<string> _models = Enumerable.Empty<string>();
        private IEnumerable<string> _floorPlans = Enumerable.Empty<string>();
        private DelegateCommand? _startCommand;
        private DelegateCommand? _scanCommand;
        private DelegateCommand? _createFloorPlanListCommand;
        private string? _selectedModel;
        private string? _selectedFloorPlan;

        public VinAndFloorPlanViewModel(INavigationService navigationService, ISessionService sessionService, IBundledDataService bundledDataService, IUserDialogService userDialogService) : base(navigationService)
        {
            _sessionService = sessionService;
            _bundledDataService = bundledDataService;
            _userDialogService = userDialogService;
        }

        public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
        {
            var permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            var HasLocationPermissions = (permissionStatus == PermissionStatus.Granted);

            var result = await Permissions.RequestAsync<Permissions.Camera>();
            var HasCameraPermissions = (result == PermissionStatus.Granted);
            
            if (reason == ResumeReason.BackNavigation)
            {
                // Populate VIN from parameters only if it has been set.
                if (parameters?.ContainsKey(ScanVinViewModel.VinParameterKey) == true)
                {
                    Vin = parameters[ScanVinViewModel.VinParameterKey].ToString();
                }
            }

            if (!_models.Any())
            {
                Models = _bundledDataService.GetModels() ?? Enumerable.Empty<string>();
            }
        }

        public void OnPause(PauseReason reason) { }
        public int MaxLength => VinLength;

        public string? Vin
        {
            get => _vin;
            set
            {
                if (SetProperty(ref _vin, value))
                {
                    OnPropertyChanged(nameof(VinError));
                    OnPropertyChanged(nameof(IsValidVin));
                    OnPropertyChanged(nameof(CanEditFloorplan));
                    OnPropertyChanged(nameof(StartButtonEnabled));
                };
            }
        }
        public bool IsValidVin => Vin?.Length == VinLength && Regex.IsMatch(Vin, "^[a-zA-Z0-9]*$");
        public bool StartButtonEnabled => IsValidVin && SelectedModel is not null && !string.IsNullOrWhiteSpace(SelectedFloorPlan);

        public string? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value))
                {
                    OnPropertyChanged(nameof(IsValidVin));
                    OnPropertyChanged(nameof(CanEditFloorplan));
                    OnPropertyChanged(nameof(StartButtonEnabled));
                };
            }
        }

        public bool CanEditFloorplan => IsValidVin && SelectedModel is not null;

        public IEnumerable<string> Models
        {
            get => _models;
            private set => SetProperty(ref _models, value);
        }

        public string? SelectedFloorPlan
        {
            get => _selectedFloorPlan;
            set
            {
                if (SetProperty(ref _selectedFloorPlan, value))
                {
                    OnPropertyChanged(nameof(StartButtonEnabled));
                }
            }
        }

        public IEnumerable<string> FloorPlans
        {
            get => _floorPlans;
            private set => SetProperty(ref _floorPlans, value);
        }

        public ICommand ScanCommand => _scanCommand ??= new DelegateCommand(async () =>
            await NavigationService.NavigateAsync(nameof(ScanVinViewModel)));

        public ICommand StartCommand => _startCommand ??= new DelegateCommand(
            async () => await CheckIfDuplicateVin(),
            () => IsValidVin && FloorPlans.Contains(SelectedFloorPlan)).ObservesProperty(() => IsValidVin).ObservesProperty(() => SelectedFloorPlan);

        private async Task CheckIfDuplicateVin()
        {
            if (Vin == null) return;
            var lastSession = _sessionService.GetLastSessionForVin(Vin);
            if (lastSession != null)
            {
                if (!await _userDialogService.ConfirmAsync(
                        Strings.ConnectionStateVerified,
                        Strings.you_have_already_paired_using_this_vin,
                        Strings.continue_text,
                        Strings.cancel,
                        StartStopCancellationToken))
                {
                    return;
                }
            }

            await StartPairingProcess();
        }

        private async Task StartPairingProcess()
        {
            if (Vin == null) return;
            _sessionService.RecordNewSession(Vin);

            var parameters = new NavigationParameters
            {
                { FloorPlanKey, SelectedFloorPlan },
                { ScanVinViewModel.VinParameterKey, Vin }
            };

            await NavigationService.NavigateAsync(nameof(PairDeviceViewModel), parameters);

            // Clear the previously entered values
            Vin = null;
            SelectedFloorPlan = null;
            SelectedModel = null;
        }

        public ICommand CreateFloorPlanListCommand => _createFloorPlanListCommand ??= new DelegateCommand(() =>
        {
            FloorPlans = Enumerable.Empty<string>();
            if (SelectedModel is not null)
            {
                FloorPlans = _bundledDataService.GetFloorPlans(SelectedModel) ?? Enumerable.Empty<string>();
            }
        });

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (SetProperty(ref _isEditing, value))
                {
                    OnPropertyChanged(nameof(VinError));
                }
            }
        }

        public string? VinError => (!string.IsNullOrEmpty(Vin) && !IsEditing && !IsValidVin) ? Strings.invalid_vin : null;

        private AsyncCommand? _gotoSettingsCommand;
        public AsyncCommand GoToSettingsCommand => _gotoSettingsCommand ??= new AsyncCommand(async () =>
            await NavigationService.NavigateAsync(nameof(SettingsPageViewModel)));
    }
}