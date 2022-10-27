using PrismExtensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.Common.ActionSheet
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [RegisterForNavigation(typeof(ActionSheetPageViewModel))]
    public partial class ActionSheetPage : ContentPage
    {
        public ActionSheetPage()
        {
            InitializeComponent();
        }
    }
}