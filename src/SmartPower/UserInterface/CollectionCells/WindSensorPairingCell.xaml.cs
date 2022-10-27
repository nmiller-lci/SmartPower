using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IDS.Portable.Common;
using SmartPower.UserInterface.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SmartPower.Extensions;

namespace SmartPower.UserInterface.CollectionCells
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WindSensorPairingCell
    {
        private readonly List<IDisposable> _subscribers;
        
        private class WindSensorPairingStep : StepProgressBarControl.IProgressBarStep
        {
            public string Title { get; set; }

            private string _subTitle;
            public string SubTitle
            {
                get => _subTitle;
                set
                {
                    _subTitle = value;
                    OnPropertyChanged(nameof(SubTitle));
                }
            }
            
            private StepProgressBarControl.IProgressBarStep.ProgressBarState _state;
            public StepProgressBarControl.IProgressBarStep.ProgressBarState State
            {
                get => _state;
                set
                {
                    _state = value;
                    OnPropertyChanged(nameof(SubTitle));
                }
            }
            
            public event PropertyChangedEventHandler? PropertyChanged;
            
            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        
        public WindSensorPairingCell()
        {
            InitializeComponent();
            _subscribers = new List<IDisposable>();
        }

        public static readonly BindableProperty WindSensorsProperty = BindableProperty.Create(
            propertyName: nameof(WindSensors),
            returnType: typeof(IEnumerable<IPairableDeviceCell>),
            declaringType: typeof(WindSensorPairingCell),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, _, newValue) =>
            {
                var windSensorPairingCell = (WindSensorPairingCell) bindable;
                var windSensors = (ObservableCollection<IPairableDeviceCell>) newValue;

                windSensorPairingCell.DisposePropertyChangeSubscribers();
                
                var steps = new ObservableCollection<WindSensorPairingStep>();
                if (windSensors != null)
                {
                    foreach (var windSensor in windSensors)
                    {
                        var step = new WindSensorPairingStep
                        {
                            Title = windSensor.DeviceName.ToUpper(),
                            SubTitle = windSensorPairingCell.GetProgressBarSubTitleForConnectionState(windSensor.State),
                            State = windSensorPairingCell.GetProgressBarStateForConnectionState(windSensor.State)
                        };
                        steps.Add(step);

                        var windSensorPropertyChangesListener = windSensor.OnAnyPropertyChanged().Subscribe(changedWindSensor =>
                        {
                            MainThread.RequestMainThreadAction(() =>
                            {
                                step.State = windSensorPairingCell.GetProgressBarStateForConnectionState(changedWindSensor.State);
                                step.SubTitle = windSensorPairingCell.GetProgressBarSubTitleForConnectionState(windSensor.State);
                            });
                        });
                        windSensorPairingCell._subscribers.Add(windSensorPropertyChangesListener);
                    }
                }
                windSensorPairingCell.Steps = new ObservableCollection<StepProgressBarControl.IProgressBarStep>(steps);
            });
        
        private string GetProgressBarSubTitleForConnectionState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.NotSelected:
                case ConnectionState.Selected:
                    return string.Empty;
                case ConnectionState.Pairing:
                    return SmartPower.Resources.Strings.ConnectionStatePairing;
                case ConnectionState.Skipped:
                    return SmartPower.Resources.Strings.ConnectionStateSkipped;
                case ConnectionState.Connecting:
                    return SmartPower.Resources.Strings.ConnectionStateConnecting;
                case ConnectionState.Connected:
                    return SmartPower.Resources.Strings.ConnectionStateConnected;
                case ConnectionState.Paired:
                    return SmartPower.Resources.Strings.ConnectionStatePaired;
                case ConnectionState.Verifying:
                    return SmartPower.Resources.Strings.ConnectionStateVerifying;
                case ConnectionState.Verified:
                    return SmartPower.Resources.Strings.ConnectionStateVerified;
                case ConnectionState.Error:
                    return SmartPower.Resources.Strings.ConnectionStateError;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private StepProgressBarControl.IProgressBarStep.ProgressBarState GetProgressBarStateForConnectionState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Skipped:
                case ConnectionState.NotSelected:
                    return StepProgressBarControl.IProgressBarStep.ProgressBarState.Unselected;
                case ConnectionState.Selected:
                    return StepProgressBarControl.IProgressBarStep.ProgressBarState.Selected;
                case ConnectionState.Pairing:
                case ConnectionState.Connecting:
                case ConnectionState.Verifying:
                    return StepProgressBarControl.IProgressBarStep.ProgressBarState.InProgress;
                case ConnectionState.Connected:
                case ConnectionState.Paired:
                case ConnectionState.Verified:
                    return StepProgressBarControl.IProgressBarStep.ProgressBarState.Completed;
                case ConnectionState.Error:
                    return StepProgressBarControl.IProgressBarStep.ProgressBarState.Error;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void DisposePropertyChangeSubscribers()
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.TryDispose();
            }
            _subscribers.Clear();
        }

        public IEnumerable<IPairableDeviceCell> WindSensors
        {
            get => (IEnumerable<IPairableDeviceCell>)GetValue(WindSensorsProperty);
            set => SetValue(WindSensorsProperty, value);
        }

        private ObservableCollection<StepProgressBarControl.IProgressBarStep> _steps;
        public ObservableCollection<StepProgressBarControl.IProgressBarStep> Steps
        {
            get => _steps;
            set
            {
                _steps = value;
                OnPropertyChanged(nameof(Steps));
            }
        }
        
        public static readonly BindableProperty SkipCommandProperty = BindableProperty.Create(
            propertyName: nameof(SkipCommand),
            returnType: typeof(ICommand),
            declaringType: typeof(WindSensorPairingCell),
            defaultValue: default(ICommand),
            defaultBindingMode: BindingMode.OneWay);

        public ICommand SkipCommand
        {
            get => (ICommand)GetValue(SkipCommandProperty);
            set => SetValue(SkipCommandProperty, value);
        }
        
        public static readonly BindableProperty CanSkipProperty = BindableProperty.Create(
            propertyName: nameof(CanSkip),
            returnType: typeof(bool),
            declaringType: typeof(WindSensorPairingCell),
            defaultValue: false,
            defaultBindingMode: BindingMode.OneWay);

        public bool CanSkip
        {
            get => (bool)GetValue(CanSkipProperty);
            set => SetValue(CanSkipProperty, value);
        }
    }
}