using PrismExtensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.Pairing;

[XamlCompilation(XamlCompilationOptions.Compile)]
[RegisterForNavigation(typeof(PairDeviceViewModel))]
public partial class PairDevicePage : ContentPage
{
    public PairDevicePage() => InitializeComponent();
}