using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IDS.Portable.Common;
using IDS.UI.UserDialogs;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using SmartPower.Services;
using SmartPower.UserInterface.ScanVin;
using SmartPower.UserInterface.Settings;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;

namespace SmartPower.UserInterface.Pages;

public class MainPageViewModel : BaseViewModel, IViewModelResumePause
{
    private readonly ISessionService _sessionService;
    private readonly IUserDialogService _userDialogService;
    private readonly IBundledDataService _bundledDataService;

    public MainPageViewModel(INavigationService navigationService, ISessionService sessionService, IBundledDataService bundledDataService, IUserDialogService userDialogService) : base(navigationService)
    {
        _sessionService = sessionService;
        _bundledDataService = bundledDataService;
        _userDialogService = userDialogService;
    }

    public async Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters, CancellationToken resumePauseCancellationToken)
    {
        var permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        var HasLocationPermissions = (permissionStatus == PermissionStatus.Granted);
    }

    public void OnPause(PauseReason reason) { }

    private AsyncCommand? _gotoSettingsCommand;
    public AsyncCommand GoToSettingsCommand => _gotoSettingsCommand ??= new AsyncCommand(async () =>
        await NavigationService.NavigateAsync(nameof(SettingsPageViewModel)));
}