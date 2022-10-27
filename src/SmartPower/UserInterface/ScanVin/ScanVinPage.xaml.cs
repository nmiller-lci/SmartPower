using PrismExtensions;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.ScanVin;

[XamlCompilation(XamlCompilationOptions.Compile)]
[RegisterForNavigation(typeof(ScanVinViewModel))]
public partial class ScanVinPage : ContentPage
{
    public ScanVinPage() => InitializeComponent();
}
