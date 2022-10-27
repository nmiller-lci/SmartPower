using System.Collections.Generic;
using System.Reflection;
using System.Windows.Input;
using Plugin.SimpleAudioPlayer;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing;

namespace SmartPower.UserInterface.Controls;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class CameraView
{
    private static readonly ISimpleAudioPlayer _player;
    
    #region IsScanning Property
    public static readonly BindableProperty IsScanningProperty = BindableProperty.Create(
        propertyName: nameof(IsScanning),
        returnType: typeof(bool),
        declaringType: typeof(CameraView),
        defaultValue: true,
        defaultBindingMode: BindingMode.OneWay,
        propertyChanged: (bindable, _, newValue) =>
        {
            if (!(bool) newValue)
                ((CameraView) bindable).IsTorchOn = false;
        });

    public bool IsScanning
    {
        get => ( bool )GetValue(IsScanningProperty);
        set => SetValue(IsScanningProperty, value);
    }
    #endregion

    #region PossibleFormats Property
    public static readonly BindableProperty PossibleFormatsProperty = BindableProperty.Create(
        propertyName: nameof(PossibleFormats),
        returnType: typeof(IEnumerable<BarcodeFormat>),
        declaringType: typeof(CameraView),
        defaultValue: default(IEnumerable<BarcodeFormat>?),
        defaultBindingMode: BindingMode.OneWay,
        propertyChanged: (bindable, _, newValue) =>
        {
            var cameraView = (CameraView) bindable;
            if (cameraView.ScannerView.Options is not null)
                cameraView.ScannerView.Options.PossibleFormats = newValue as IEnumerable<BarcodeFormat>;
        });
    public IEnumerable<BarcodeFormat> PossibleFormats
    {
        get => ( IEnumerable<BarcodeFormat> )GetValue(PossibleFormatsProperty);
        set => SetValue(PossibleFormatsProperty, value);
    }
    #endregion

    #region ScanCommand Property
    public static readonly BindableProperty ScanCommandProperty = BindableProperty.Create(
        propertyName: nameof(ScanCommand),
        returnType: typeof(ICommand),
        declaringType: typeof(CameraView),
        defaultValue: default(ICommand?),
        defaultBindingMode: BindingMode.OneWay);

    public ICommand? ScanCommand
    {
        get => ( ICommand? )GetValue(ScanCommandProperty);
        set => SetValue(ScanCommandProperty, value);
    }
    #endregion
    
    #region IsTorchOn Property
    public static readonly BindableProperty IsTorchOnProperty = BindableProperty.Create(
        propertyName: nameof(IsTorchOn),
        returnType: typeof(bool),
        declaringType: typeof(CameraView),
        defaultValue: default(bool),
        defaultBindingMode: BindingMode.TwoWay);

    public bool IsTorchOn
    {
        get => ( bool )GetValue(IsTorchOnProperty);
        set => SetValue(IsTorchOnProperty, value);
    }
    #endregion

    #region ShowScanLine Property
    public static readonly BindableProperty ShowScanLineProperty = BindableProperty.Create(
        propertyName: nameof(ShowScanLine),
        returnType: typeof(bool),
        declaringType: typeof(CameraView),
        defaultValue: default(bool),
        defaultBindingMode: BindingMode.OneWay);

    public bool ShowScanLine
    {
        get => ( bool )GetValue(ShowScanLineProperty);
        set => SetValue(ShowScanLineProperty, value);
    }
    #endregion
    
    static CameraView()
    {
        _player = CrossSimpleAudioPlayer.Current;
        using var stream = typeof(App).GetTypeInfo().Assembly.GetManifestResourceStream("SmartPower.Resources.Sounds.scan.mp3");
        _player.Load(stream);
    }
    
    public CameraView() => InitializeComponent();

    private void OnOnScanResult(Result result)
    {
        if (!ScanCommand?.CanExecute(result) ?? false) return;
        ScanCommand.Execute(result);
        _player.Play();
        AnimateScanLine();
    }
    
    private void AnimateScanLine()
    {
        var startHeight = ScanLine.Height;
        var endHeight = 2 * startHeight;
        var startOpacity = 0.5;
        var endOpacity = 1.0;
        var startScaleX = 1.0;
        var endScaleX = 1.1;

        var parentAnimation = new Animation()
        {
            { 0, 0.5, new Animation(height => ScanLine.HeightRequest = height, startHeight, endHeight, easing: Easing.SpringOut) },
            { 0.5, 1, new Animation(height => ScanLine.HeightRequest = height, endHeight, startHeight, easing: Easing.SpringOut) },
            { 0, 0.5, new Animation(opacity => ScanLine.Opacity = opacity, startOpacity, endOpacity, easing: Easing.SpringOut) },
            { 0.5, 1, new Animation(opacity => ScanLine.Opacity = opacity, endOpacity, startOpacity, easing: Easing.SpringOut) },
            { 0, 0.5, new Animation(scale => ScanLine.ScaleX = scale, startScaleX, endScaleX, easing: Easing.SpringOut) },
            { 0.5, 1, new Animation(scale => ScanLine.ScaleX = scale, endScaleX, startScaleX, easing: Easing.SpringOut) },
        };

        parentAnimation.Commit(ScanLine, "FlashLine", length: 200);
    }
}