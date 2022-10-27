using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SmartPower.UserInterface.Controls;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;
using PlatformEffects = SmartPower.Droid.Effects;
using RoutingEffects = SmartPower.UserInterface.Effects;

[assembly: ExportEffect(typeof(PlatformEffects.FindLowestCameraResolutionEffect), nameof(RoutingEffects.FindLowestCameraResolutionEffect))]
namespace SmartPower.Droid.Effects
{
    internal class FindLowestCameraResolutionEffect : PlatformEffect
    {
        private static readonly CameraResolutionAspectComparer _cameraResolutionAspectComparer = new ();
        
        private ZXingScannerView? _scannerView;
        
        protected override void OnAttached()
            => _scannerView = Element as ZXingScannerView ??
                throw new NotImplementedException($"{nameof(RoutingEffects.FindLowestCameraResolutionEffect)} can only be applied to a {nameof(ZXingScannerView)}");

        private void UpdateResolutionSelector()
        {
            var cameraLayout = _scannerView.Parent as CameraLayout;
            
            var (width, height) = (cameraLayout is CameraLayout) switch
            {
                true => (cameraLayout.Width, cameraLayout.Height),
                _ => (_scannerView.Width, _scannerView.Height)
            };

            if (height < 0 || width < 0) return;

            _scannerView.Options.CameraResolutionSelector = availableResolutions =>
            {
                availableResolutions.Sort(_cameraResolutionAspectComparer);
                
                var result = availableResolutions.FirstOrDefault();

                var aspectTolerance = 0.1; // Allow aspect ratio to be off by a small amount.

                // Screens aspect ratio adjusted for the current orientation.
                var targetAspectRatio = height / width;
                var targetHeight = height;
                var minimumDifference = double.MaxValue;

                // Filter to only the resolutions with an aspect ratio within tolerance.
                var suitableResolutions = availableResolutions.Where(r => Math.Abs(GetAwareAspectRatio(DeviceDisplay.MainDisplayInfo.Orientation, r.Height, r.Width) - targetAspectRatio) < aspectTolerance);

                // Find the best fit.
                if (suitableResolutions.Any())
                {
                    foreach (var resolution in suitableResolutions)
                    {
                        if (Math.Abs(resolution.Height - targetHeight) < minimumDifference)
                        {
                            minimumDifference = Math.Abs(resolution.Height - targetHeight);
                            result = resolution;
                        }
                    }
                }
                
                ResizeElement(new Size(width, height), result);
                return result;
            };
        }
        
        private static double GetAwareAspectRatio(DisplayOrientation orientation, double height, double width)
        {
            return (orientation == DisplayOrientation.Portrait) ? height / width : width / height;
        }

        private void ResizeElement(Size viewSize, CameraResolution cameraResolution)
        {
            if (Element.Parent is not CameraLayout cameraLayout) return;
            if (Element is not View view) return;
            
            var cameraAspectRatio = (double)cameraResolution.Width / cameraResolution.Height;
            
            view.HorizontalOptions = LayoutOptions.Center;
            view.VerticalOptions = LayoutOptions.Center;

            // Calculate the height based off the camera aspect and the width of the layout. This is a guess. We are
            // trying to make the Content larger than the layout to prevent letter boxing.
            // 
            var width = viewSize.Width;
            var height = width / cameraAspectRatio;
            
            // If the calculated height is smaller than what is available, then recalculate suing the available height.
            // This guarantees the nee width, will exceed the available width.
            // 
            if (height < viewSize.Height)
            {
                height = viewSize.Height;
                width = height * cameraAspectRatio;
            }

            view.WidthRequest = width;
            view.HeightRequest = height;
        }

        protected override void OnDetached() { }

        protected override void OnElementPropertyChanged(PropertyChangedEventArgs args)
        {
            base.OnElementPropertyChanged(args);

            if (args.PropertyName == VisualElement.WidthProperty.PropertyName ||
                args.PropertyName == VisualElement.HeightProperty.PropertyName)
            {
                UpdateResolutionSelector();
            }
        }
    }

    internal class CameraResolutionAspectComparer : IComparer<CameraResolution>
    {
        public int Compare(CameraResolution x, CameraResolution y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            var aspect = ((double)x.Width / x.Height).CompareTo((double)y.Width / y.Height);
            return aspect == 0
                ? ((double)x.Width * x.Height).CompareTo((double)y.Width * y.Height)
                : aspect;
        }
    }
}