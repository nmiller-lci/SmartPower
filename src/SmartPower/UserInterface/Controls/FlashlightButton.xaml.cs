using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.Controls;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class FlashlightButton
{
    #region IsTorchOn Property
    public static readonly BindableProperty IsTorchOnProperty = BindableProperty.Create(
        propertyName: nameof(IsTorchOn),
        returnType: typeof(bool),
        declaringType: typeof(FlashlightButton),
        defaultValue: default(bool),
        defaultBindingMode: BindingMode.TwoWay);

    public bool IsTorchOn
    {
        get => ( bool )GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }
    #endregion
    
    public FlashlightButton() => InitializeComponent();

    private void TapGestureRecognizer_OnTapped(object sender, EventArgs e)
    {
        IsTorchOn = !IsTorchOn;
        HapticFeedback.Perform();
    }
}