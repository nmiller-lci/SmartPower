using PrismExtensions;
using Xamarin.Forms;

namespace SmartPower.UserInterface.Settings
{
    [RegisterForNavigation(typeof(SettingsPageViewModel))]
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}