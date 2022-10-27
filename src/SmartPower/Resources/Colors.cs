using System.Globalization;
using IDS.UI.Converters;
using Xamarin.Forms;

namespace SmartPower.Resources;

public class Colors : IDS.UI.Resources.Style.Colors
{
    private static readonly AccessibleTextColorConverter _accessibleTextColorConverter = new();
    protected override void OnUseLightTheme()
    {
        base.OnUseLightTheme();
        NormalizeKeyColors();
    }

    protected override void OnUseDarkTheme()
    {
        base.OnUseDarkTheme();
        NormalizeKeyColors();
    }

    private void NormalizeKeyColors()
    {
        this[Primary] = PrimaryKeyColor;
        this[Secondary] = SecondaryKeyColor;
        this[Tertiary] = TertiaryKeyColor;
        this[Error] = ErrorKeyColor;
        this[OnPrimary] = this["Primary95"];
        this[OnSecondary] = GetOnColor(SecondaryKeyColor);
        this[OnTertiary] = GetOnColor(TertiaryKeyColor);
        this[OnError] = GetOnColor(ErrorKeyColor);
    }

    private static Color GetOnColor(Color color)
        => (Color) _accessibleTextColorConverter.Convert(color, typeof(Color), null, CultureInfo.InvariantCulture);
}