using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using Rectangle = Xamarin.Forms.Rectangle;

namespace SmartPower.UserInterface.Controls;

[ContentProperty(nameof(Content))]
public class CameraLayout : Layout<ZXingScannerView>
{
    private ZXingScannerView? _content;
    public ZXingScannerView? Content
    {
        get => _content;
        set
        {
            if (ReferenceEquals(value, _content)) return;
            
            if (_content is not null)
            {
                Children.RemoveAt(0);
            }
            
            _content = value;

            if (_content is null) return;
            Children.Add(_content);
        }
    }

    protected override void LayoutChildren(double x, double y, double width, double height)
    {
        if (_content.WidthRequest> 0 && _content.HeightRequest > 0)
        {
            x += (width - _content.WidthRequest) * 0.5;
            y += (height - _content.HeightRequest) * 0.5;
            width = _content.WidthRequest;
            height = _content.HeightRequest;
        }
        
        LayoutChildIntoBoundingRegion(_content, new Rectangle(x, y, width, height));
    }
}