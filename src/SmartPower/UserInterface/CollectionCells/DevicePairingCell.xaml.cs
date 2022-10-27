using System;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SmartPower.UserInterface.CollectionCells
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DevicePairingCell
    {
        #region Content Attached Property
        public static readonly BindableProperty ContentProperty = BindableProperty.CreateAttached(
            propertyName: "Content",
            returnType: typeof(View),
            declaringType: typeof(DevicePairingCell),
            defaultValue: default(View),
            defaultBindingMode: BindingMode.OneWayToSource,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var devicePairingCell = (DevicePairingCell) bindable;
                var newContent = newValue as View;

                if (oldValue is View oldContent)
                    devicePairingCell.Children.Remove(oldContent);

                if (newContent is not null)
                    devicePairingCell.Children.Insert(0, newContent);
            });

        public static View GetContent(BindableObject view)
            => (View)view.GetValue(ContentProperty);

        protected static void SetContent(BindableObject view, View value)
            => view.SetValue(ContentProperty, value);
        #endregion
        
        #region Title Property
        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            propertyName: nameof(Title),
            returnType: typeof(string),
            declaringType: typeof(DevicePairingCell),
            defaultValue: default(string),
            defaultBindingMode: BindingMode.OneWay);

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion

        #region Description Property
        public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
            propertyName: nameof(Description),
            returnType: typeof(string),
            declaringType: typeof(DevicePairingCell),
            defaultValue: default(string),
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, _, newValue) =>
            {
                var devicePairingCell = (DevicePairingCell) bindable;
                var newDescription = (string) newValue;

                devicePairingCell.ShowDescription = !string.IsNullOrWhiteSpace(newDescription);
            });

        public string Description
        {
            get => ( string )GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        #endregion
        
        #region ConnectionState Property
        public static readonly BindableProperty StateProperty = BindableProperty.Create(
            propertyName: nameof(State),
            returnType: typeof(ConnectionState),
            declaringType: typeof(DevicePairingCell),
            defaultValueCreator: (bindable) =>
            {
                var devicePairingCell = (DevicePairingCell)bindable;
                devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateNotConnected;
                devicePairingCell.ShowSpinner = false;
                devicePairingCell.ShowError = false;
                return ConnectionState.Selected;
            },
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, _, newValue) =>
            {
                var devicePairingCell = (DevicePairingCell)bindable;
                var newConnectionState = (ConnectionState)newValue;

                switch (newConnectionState)
                {
                    case ConnectionState.NotSelected:
                    case ConnectionState.Selected:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateNotConnected;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Connecting:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateConnecting;
                        devicePairingCell.ShowSpinner = true;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Connected:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateConnected;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Pairing:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStatePairing;
                        devicePairingCell.ShowSpinner = true;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Paired:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStatePaired;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Verifying:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateVerifying;
                        devicePairingCell.ShowSpinner = true;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Verified:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateVerified;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = false;
                        break;
                    case ConnectionState.Error:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateError;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = true;
                        break;
                    case ConnectionState.Skipped:
                        devicePairingCell.StateDescription = SmartPower.Resources.Strings.ConnectionStateSkipped;
                        devicePairingCell.ShowSpinner = false;
                        devicePairingCell.ShowError = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

        public ConnectionState State
        {
            get => (ConnectionState)GetValue(StateProperty);
            set => SetValue(StateProperty, value);
        }
        #endregion

        #region Command Property
        public static readonly BindableProperty CommandProperty = BindableProperty.Create(
            propertyName: nameof(Command),
            returnType: typeof(ICommand),
            declaringType: typeof(DevicePairingCell),
            defaultValue: default(ICommand),
            defaultBindingMode: BindingMode.OneWay);

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }
        #endregion

        #region CommandParameter Property
        public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
            propertyName: nameof(CommandParameter),
            returnType: typeof(object),
            declaringType: typeof(DevicePairingCell),
            defaultValue: default(object),
            defaultBindingMode: BindingMode.OneWay);

        public object CommandParameter
        {
            get => ( object )GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
        #endregion
        
        #region StateDescription Property
        private string? _stateDescription;
        public string? StateDescription
        {
            get => _stateDescription;
            private set
            {
                if (string.Equals(_stateDescription, value, StringComparison.Ordinal)) return;
                _stateDescription = value;
                OnPropertyChanged(nameof(StateDescription));
            }
        }
        #endregion

        #region ShowDesciption
        private bool _showDescription;
        public bool ShowDescription
        {
            get => _showDescription;
            set
            {
                if (value == _showDescription) return;
                _showDescription = value;
                OnPropertyChanged(nameof(ShowDescription));
            }
        }
        #endregion

        #region ShowStateDescription Property
        private bool _showStateDescription;
        public bool ShowStateDescription
        {
            get => _showStateDescription;
            private set
            {
                if (_showStateDescription == value) return;
                _showStateDescription = value;
                OnPropertyChanged(nameof(ShowStateDescription));
            }
        }
        #endregion

        #region ShowSpinner Property
        private bool _showSpinner;
        public bool ShowSpinner
        {
            get => _showSpinner;
            private set
            {
                if (_showSpinner == value) return;
                _showSpinner = value;
                OnPropertyChanged(nameof(ShowSpinner));
            }
        }
        #endregion

        #region ShowError Property
        private bool _showError;
        public bool ShowError
        {
            get => _showError;
            private set
            {
                if (_showError == value) return;
                _showError = value;
                OnPropertyChanged(nameof(ShowError));
            }
        }
        #endregion
        
        public DevicePairingCell() => InitializeComponent();
        
        protected override void OnChildAdded(Element child)
        {
            // Check the stack trace to see if it is coming from InitializeComponent. This
            // can only happen when the layout is constructed or during Hot Reload. We want
            // to prevent direct content from being added to this layout; but, don't want
            // to prevent Hot Reload from working.
            // 
            if (!Environment.StackTrace.Contains(nameof(InitializeComponent)))
                throw new InvalidOperationException($"{nameof(DevicePairingCell)} does not support direct content!");

            base.OnChildAdded(child);
        }

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
            => Children.First().Measure(widthConstraint, heightConstraint, MeasureFlags.IncludeMargins);

        protected override void LayoutChildren(double x, double y, double width, double height)
            => LayoutChildIntoBoundingRegion(Children.First(), new Rectangle(x, y, width, height));
    }
}