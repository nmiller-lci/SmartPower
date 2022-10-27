using IDS.Portable.Common;
using IDS.Portable.LogicalDevice;
using IDS.UI.Core.Models;
using OneControl.ViewModels.Base;
using Prism.Navigation;
using System.Collections.ObjectModel;
using System.Windows.Input;
using IDS.UI.Shared.Pages;
using Xamarin.Forms;

namespace OneControl.ViewModels
{
    public class SystemDiagnosticsDeviceReportPageViewModel : BaseViewModelPage
    {
        private SystemDiagnosticsDeviceReportSyncContainer _deviceReportSyncContainer;
        private readonly ILogicalDeviceManager _logicalDeviceManager;

        public SystemDiagnosticsDeviceReportPageViewModel(INavigationService navigationService) : base(navigationService)
        {
            var logicalDeviceService = Resolver<ILogicalDeviceService>.Resolve;
            _logicalDeviceManager = logicalDeviceService?.DeviceManager;

        }
        
        #region HeaderLandscapeChild
        public string Title => "System Diagnostics";

        public string Description => "Device Summary";
        #endregion

        #region LifeCycle
        protected override void OnResumeViewModel()
        {

            _deviceReportSyncContainer?.TryDispose();
            _deviceReportSyncContainer = new SystemDiagnosticsDeviceReportSyncContainer(_diagnosticHtmlSource, new OrderedObservableCollection<ILogicalDevice>(), _logicalDeviceManager);
        }

        protected override void OnPauseViewModel()
        {
            _deviceReportSyncContainer?.TryDispose();
            _deviceReportSyncContainer = null;
        }
        #endregion

        public override RotationPage.DeviceOrientationMask Orientation => RotationPage.DeviceOrientationMask.Landscape;

        #region LandscapeMenu
        public ObservableCollection<ISingleSelectionCellWithImageViewModel> MenuOptions => DeviceSettings.CreateBackMenuCells();

        public ICommand BackCommand => new Command(async () => await GoBackAsync());
        #endregion

        #region FourColumnPaginatedView
         
        private readonly HtmlWebViewSource _diagnosticHtmlSource = new HtmlWebViewSource { Html = SystemDiagnosticsDeviceReportSyncContainer.HtmlEmptyTemplate };
        public WebViewSource DiagnosticHtmlSource => _diagnosticHtmlSource;
        #endregion
    }
}
