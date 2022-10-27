using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneControl;
using SmartPower.Resources;
using SmartPower.Services;
using SmartPower.UserInterface.Common.ActionSheet;
using Prism.Navigation;
using PrismExtensions.Enums;
using PrismExtensions.ViewModels;
using Serilog.Events;
using Xamarin.CommunityToolkit.ObjectModel;

namespace SmartPower.UserInterface.Settings
{
    public class SettingsPageViewModel : BaseViewModel, IViewModelResumePause
    {
        protected virtual string LogTag => GetType().Name;
        public SettingsPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            UploadStatusDescription = Strings.upload_logs;

            LogLevelStatus = AppLogConstants.LogLevelCurrent.MinimumLevel.ToString();
        }

        #region Diagnostic Report

        private string? _uploadStatusDescription;
        public string? UploadStatusDescription
        {
            get => _uploadStatusDescription;
            set => SetProperty(ref _uploadStatusDescription, value);
        }

        private AsyncCommand? _uploadLogsCommand;
        public AsyncCommand UploadLogsCommand => _uploadLogsCommand ??= new AsyncCommand(async () =>
        {
            await DiagnosticReportManager.Instance.UploadLogAsync((state, percent, ex) =>
            {
                if (ex != null)
                {
                    // todo: Shouldn't expose raw ex.Message to users, but this is for developer mode only right now
                    UploadStatusDescription = Strings.unable_to_send;
                    return;
                }

                switch (state)
                {
                    case DiagnosticReportManager.State.Ready:
                        UploadStatusDescription = percent >= 100 ? Strings.upload_complete : Strings.upload_logs;
                        break;

                    case DiagnosticReportManager.State.GeneratingLog:
                        UploadStatusDescription = Strings.generating_log_file;
                        break;

                    case DiagnosticReportManager.State.SendingLog:
                        UploadStatusDescription = $"{Strings.sending_logs} {percent}%";
                        break;
                }
            });
        });

        #endregion

        #region Log Level

        private string? _logLevelStatus;
        public string? LogLevelStatus
        {
            get => _logLevelStatus;
            set => SetProperty(ref _logLevelStatus, value);
        }


        private AsyncCommand? _changeLogLevelCommand;

        public AsyncCommand ChangeLogLevelCommand => _changeLogLevelCommand ??= new AsyncCommand(async () =>
        {
            List<object> logLevelChoices = new List<object>()
            {
                LogEventLevel.Verbose,
                LogEventLevel.Debug,
                LogEventLevel.Information,
                LogEventLevel.Warning,
                LogEventLevel.Error,
                LogEventLevel.Fatal
            };

            var currentLogLevel = AppLogConstants.LogLevelCurrent.MinimumLevel;

            var config = new UserInterface.Common.ActionSheet.ActionSheetConfig(
                "Levels", logLevelChoices, currentLogLevel, Strings.events_below_this_level);

            var parameters = new NavigationParameters
            {
                { ActionSheetPageViewModel.ActionSheetConfigKey, config }
            };

            await NavigationService.NavigateAsync(nameof(ActionSheetPageViewModel), parameters);
        });

        private void ProcessResults(ActionSheetResult? result)
        {
            if (result is { Canceled: false, SelectedOption: LogEventLevel newEventLevel })
            {
                AppLogConstants.LogLevelCurrent.MinimumLevel = newEventLevel;

                LogLevelStatus = AppLogConstants.LogLevelCurrent.MinimumLevel.ToString();
            }
        }

        #endregion

        #region Demo Mode

        private bool _isToggled;
        public bool IsToggled
        {
            get => _isToggled;
            set
            {
                SetProperty(ref _isToggled, value);
                if (!_isToggled) //Demo mode is being switched off
                    AppDemoMode.DefaultDemoDeviceSource.RemoveDemoDevices();
                AppSettings.Instance.SetSelectedRvGatewayConnection(_isToggled ? AppSettings.DefaultRvDirectConnectionDemo : null, saveSelectedRv: true);
            } 
        }
        #endregion

        public Task OnResumeAsync(ResumeReason reason, INavigationParameters? parameters,
            CancellationToken resumePauseCancellationToken)
        {
            ActionSheetResult? result = null;
            if (reason == ResumeReason.BackNavigation && parameters?.TryGetValue(ActionSheetPageViewModel.ActionSheetResultKey, out result) == true)
            {
                ProcessResults(result);
            }

            return Task.CompletedTask;
        }

        public void OnPause(PauseReason reason)
        {

        }
    }
}
