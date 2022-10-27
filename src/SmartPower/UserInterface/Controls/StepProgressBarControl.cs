using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using IDS.Portable.Common;
using IDS.UI.Shared.Views.Controls;
using Lottie.Forms;
using Xamarin.Forms;
using SmartPower.Extensions;
using Xamarin.Forms.Internals;

namespace SmartPower.UserInterface.Controls
{
    public class StepProgressBarControl: Grid
    {
        private readonly Color _disableTextColor = Color.FromHex("#9e9e9e");
        
        private const string SeparatorColorResource = "Outline";
        private const string TextColorResource = "OnSurface";
        private const string BackgroundColorResource = "Surface";
        private const string StyleButtonUnselected = "Button.UnSelectedStyle";
        private const string StyleTitleUnselected = "Title.UnSelectedStyle";
        private const string StyleButtonSelected = "Button.SelectedStyle";
        private const string StyleTitleSelected = "Title.SelectedStyle";
        private const string SpinnerAnimationResource = "Resources.Lottie.spinner.json";
        private const string ConnectedIconResource = "SmartPower.Resources.Images.connected.svg";
        private const string ErrorIconResource = "SmartPower.Resources.Images.error.svg";
        private const int ButtonSize = 50;
        
        public interface IProgressBarStep: INotifyPropertyChanged
        {
            string Title { get; } 
            string SubTitle { get; }
            ProgressBarState State { get; }

            public enum ProgressBarState
            {
                Selected,
                Unselected,
                InProgress,
                Skipped,
                Completed,
                Error
            }
        }
        
        private StackLayout _lastStepSelected;
        private readonly List<IDisposable> _subscribers = new();
        
        public static readonly BindableProperty StepsProperty = BindableProperty.Create(
            propertyName: nameof(Steps),
            returnType: typeof(ObservableCollection<IProgressBarStep>),
            declaringType: typeof(StepProgressBarControl),
            defaultValue: null,
            defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, _, newValue) =>
            {
                var progressBarControl = (StepProgressBarControl) bindable;
                var steps = (ObservableCollection<IProgressBarStep>) newValue;
                progressBarControl.SetSteps(steps);
            });

        private void SetSteps(IReadOnlyList<IProgressBarStep> steps)
        {
            DisposePropertyChangeSubscribers();

            if (steps.Any())
            {
                var separatorLine = new BoxView()
                {
                    HeightRequest = 1,
                    WidthRequest = 5,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Margin = new Thickness(5, -45, 5, 0)
                };
                separatorLine.SetDynamicResource(BackgroundColorProperty, SeparatorColorResource);
                Children.Add(separatorLine);

                var rootLayout = new StackLayout
                {
                    Orientation = StackOrientation.Horizontal,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };
                Children.Add(rootLayout);
                
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];

                    var stackLayout = new StackLayout
                    {
                        Orientation = StackOrientation.Vertical,
                        BackgroundColor = Color.Transparent,
                        ClassId = $"{i + 1}",
                        Spacing = 3
                    };

                    var buttonPaddingWrapper = new Grid
                    {
                        Padding = new Thickness(5),
                        HeightRequest = ButtonSize,
                        WidthRequest = ButtonSize,
                    };
                    buttonPaddingWrapper.SetDynamicResource(BackgroundColorProperty, BackgroundColorResource);
                    
                    var numberButton = new Button()
                    {
                        Text = $"{i + 1}",
                        HorizontalOptions = LayoutOptions.Center,
                        Style = Resources[StyleButtonUnselected] as Style
                    };
                    numberButton.SetDynamicResource(BackgroundColorProperty, BackgroundColorResource);
                    
                    var titleLabel = new Label
                    {
                        Text = step.Title,
                        FontSize = 16,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Style = Resources[StyleTitleUnselected] as Style
                    };

                    var subTitleLabel = new Label
                    {
                        Text = step.SubTitle,
                        FontSize = 12,
                        HorizontalOptions = LayoutOptions.CenterAndExpand,
                        HorizontalTextAlignment = TextAlignment.Center
                    };
                    subTitleLabel.SetDynamicResource(Label.TextColorProperty, TextColorResource);

                    var animationView = new AnimationView
                    {
                        HeightRequest = ButtonSize-2,
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        RepeatMode = RepeatMode.Infinite
                    };
                    animationView.SetAnimationFromEmbeddedResource(SpinnerAnimationResource);

                    var connectedImageView = new SvgImageView
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        HeightRequest = ButtonSize,
                        WidthRequest = ButtonSize,
                        ImageSource = ImageSource.FromResource(ConnectedIconResource)
                    };

                    var errorImageView = new SvgImageView
                    {
                        HorizontalOptions = LayoutOptions.Center,
                        VerticalOptions = LayoutOptions.Center,
                        HeightRequest = ButtonSize,
                        WidthRequest = ButtonSize,
                        ImageSource = ImageSource.FromResource(ErrorIconResource)
                    };
                    
                    buttonPaddingWrapper.Children.Add(numberButton);
                    buttonPaddingWrapper.Children.Add(animationView);
                    buttonPaddingWrapper.Children.Add(connectedImageView);
                    buttonPaddingWrapper.Children.Add(errorImageView);

                    var onStepConfigurationChanged = new Action<IProgressBarStep>(changedStep =>
                    {
                        subTitleLabel.Text = changedStep.SubTitle;
                        animationView.IsVisible = false;
                        connectedImageView.IsVisible = false;
                        errorImageView.IsVisible = false;
                        numberButton.IsVisible = false;

                        switch (changedStep.State)
                        {
                            case IProgressBarStep.ProgressBarState.Selected:
                                numberButton.IsVisible = true;
                                numberButton.Style = Resources[StyleButtonSelected] as Style;
                                titleLabel.Style = Resources[StyleTitleSelected] as Style;
                                subTitleLabel.Style = Resources[StyleTitleSelected] as Style;
                                break;
                            case IProgressBarStep.ProgressBarState.Unselected:
                            case IProgressBarStep.ProgressBarState.Skipped:
                                numberButton.IsVisible = true;
                                numberButton.Style = Resources[StyleButtonUnselected] as Style;
                                titleLabel.Style = Resources[StyleTitleUnselected] as Style;
                                subTitleLabel.Style = Resources[StyleTitleUnselected] as Style;
                                subTitleLabel.TextColor = _disableTextColor;
                                break;
                            case IProgressBarStep.ProgressBarState.InProgress:
                                animationView.IsVisible = true;
                                titleLabel.Style = Resources[StyleTitleSelected] as Style;
                                subTitleLabel.Style = Resources[StyleTitleSelected] as Style;
                                break;
                            case IProgressBarStep.ProgressBarState.Completed:
                                connectedImageView.IsVisible = true;
                                titleLabel.Style = Resources[StyleTitleSelected] as Style;
                                subTitleLabel.Style = Resources[StyleTitleSelected] as Style;
                                break;
                            case IProgressBarStep.ProgressBarState.Error:
                                errorImageView.IsVisible = true;
                                titleLabel.Style = Resources[StyleTitleSelected] as Style;
                                subTitleLabel.Style = Resources[StyleTitleSelected] as Style;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    });
                    onStepConfigurationChanged(step);

                    var stepPropertyChangesListener = step.OnAnyPropertyChanged().Subscribe(changedStep =>
                    {
                        onStepConfigurationChanged(changedStep);
                    });
                    _subscribers.Add(stepPropertyChangesListener);

                    stackLayout.Children.Add(buttonPaddingWrapper);
                    stackLayout.Children.Add(titleLabel);
                    stackLayout.Children.Add(subTitleLabel);

                    rootLayout.Children.Add(stackLayout);

                    if (i < steps.Count - 1)
                    {
                        var delimiter = new BoxView()
                        {
                            BackgroundColor = Color.Transparent,
                            HeightRequest = 1,
                            WidthRequest = 5,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalOptions = LayoutOptions.FillAndExpand,
                            Margin = new Thickness(5, 0, 5, 0)
                        };
                        rootLayout.Children.Add(delimiter);
                    }
                }
            }
            else
            {
                Children.Clear();
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
        
        public ObservableCollection<IProgressBarStep>? Steps
        {
            get => (ObservableCollection<IProgressBarStep>) GetValue(StepsProperty);
            set => SetValue(StepsProperty, value);
        }

        public StepProgressBarControl()
        {
            HorizontalOptions = LayoutOptions.FillAndExpand;
            AddStyles();
        }
        
        void AddStyles()
        {
            Resources = new ResourceDictionary
            {
                {
                    "Button.UnSelectedStyle", new Style(typeof(Button))
                    {
                        Setters =
                        {
                            new Setter { Property = BackgroundColorProperty, Value = Color.Transparent },
                            new Setter { Property = Button.TextColorProperty, Value = _disableTextColor },
                            new Setter { Property = Button.BorderColorProperty, Value = _disableTextColor },
                            new Setter { Property = Button.FontSizeProperty, Value = 17 },
                            new Setter { Property = Button.BorderWidthProperty, Value = 1 },
                            new Setter { Property = Button.CornerRadiusProperty, Value = ButtonSize },
                            new Setter { Property = HeightRequestProperty, Value = ButtonSize },
                            new Setter { Property = WidthRequestProperty, Value = ButtonSize },
                        }
                    }
                },
                {
                    "Button.SelectedStyle", new Style(typeof(Button))
                    {
                        Setters =
                        {
                            new Setter { Property = BackgroundColorProperty, Value = Color.Transparent },
                            new Setter { Property = Button.TextColorProperty, Value = new DynamicResource(TextColorResource) },
                            new Setter { Property = Button.BorderColorProperty, Value = new DynamicResource(TextColorResource) },
                            new Setter { Property = Button.FontSizeProperty, Value = 17 },
                            new Setter { Property = Button.BorderWidthProperty, Value = 1 },
                            new Setter { Property = Button.CornerRadiusProperty, Value = ButtonSize },
                            new Setter { Property = HeightRequestProperty, Value = ButtonSize },
                            new Setter { Property = WidthRequestProperty, Value = ButtonSize },
                        }
                    }
                },
                {
                    "Title.UnSelectedStyle", new Style(typeof(Label))
                    {
                        Setters =
                        {
                            new Setter { Property = Label.TextColorProperty, Value = _disableTextColor },
                        }
                    }
                },
                {
                    "Title.SelectedStyle", new Style(typeof(Label))
                    {
                        Setters =
                        {
                            new Setter { Property = Label.TextColorProperty, Value = new DynamicResource(TextColorResource) },
                        }
                    }
                }
            };
        }
    }
}