using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace SmartPower.Resources
{
    public class Typography : ResourceDictionary
    {
        public const string DisplayLargeFont = "DisplayLargeFont";
        public const string DisplayLargeLineHeight = "DisplayLargeLineHeight";
        public const string DisplayLargeSize = "DisplayLargeSize";
        public const string DisplayLargeTracking = "DisplayLargeTracking";
        public const string DisplayLargeWeight = "DisplayLargeWeight";
        public const string DisplayMediumFont = "DisplayMediumFont";
        public const string DisplayMediumLineHeight = "DisplayMediumLineHeight";
        public const string DisplayMediumSize = "DisplayMediumSize";
        public const string DisplayMediumTracking = "DisplayMediumTracking";
        public const string DisplayMediumWeight = "DisplayMediumWeight";
        public const string DisplaySmallFont = "DisplaySmallFont";
        public const string DisplaySmallLineHeight = "DisplaySmallLineHeight";
        public const string DisplaySmallSize = "DisplaySmallSize";
        public const string DisplaySmallTracking = "DisplaySmallTracking";
        public const string DisplaySmallWeight = "DisplaySmallWeight";

        public const string HeadlineLargeFont = "HeadlineLargeFont";
        public const string HeadlineLargeLineHeight = "HeadlineLargeLineHeight";
        public const string HeadlineLargeSize = "HeadlineLargeSize";
        public const string HeadlineLargeTracking = "HeadlineLargeTracking";
        public const string HeadlineLargeWeight = "HeadlineLargeWeight";
        public const string HeadlineMediumFont = "HeadlineMediumFont";
        public const string HeadlineMediumLineHeight = "HeadlineMediumLineHeight";
        public const string HeadlineMediumSize = "HeadlineMediumSize";
        public const string HeadlineMediumTracking = "HeadlineMediumTracking";
        public const string HeadlineMediumWeight = "HeadlineMediumWeight";
        public const string HeadlineSmallFont = "HeadlineSmallFont";
        public const string HeadlineSmallLineHeight = "HeadlineSmallLineHeight";
        public const string HeadlineSmallSize = "HeadlineSmallSize";
        public const string HeadlineSmallTracking = "HeadlineSmallTracking";
        public const string HeadlineSmallWeight = "HeadlineSmallWeight";

        public const string TitleLargeFont = "TitleLargeFont";
        public const string TitleLargeLineHeight = "TitleLargeLineHeight";
        public const string TitleLargeSize = "TitleLargeSize";
        public const string TitleLargeTracking = "TitleLargeTracking";
        public const string TitleLargeWeight = "TitleLargeWeight";
        public const string TitleMediumFont = "TitleMediumFont";
        public const string TitleMediumLineHeight = "TitleMediumLineHeight";
        public const string TitleMediumSize = "TitleMediumSize";
        public const string TitleMediumTracking = "TitleMediumTracking";
        public const string TitleMediumWeight = "TitleMediumWeight";
        public const string TitleSmallFont = "TitleSmallFont";
        public const string TitleSmallLineHeight = "TitleSmallLineHeight";
        public const string TitleSmallSize = "TitleSmallSize";
        public const string TitleSmallTracking = "TitleSmallTracking";
        public const string TitleSmallWeight = "TitleSmallWeight";

        public const string LabelLargeFont = "LabelLargeFont";
        public const string LabelLargeLineHeight = "LabelLargeLineHeight";
        public const string LabelLargeSize = "LabelLargeSize";
        public const string LabelLargeTracking = "LabelLargeTracking";
        public const string LabelLargeWeight = "LabelLargeWeight";
        public const string LabelMediumFont = "LabelMediumFont";
        public const string LabelMediumLineHeight = "LabelMediumLineHeight";
        public const string LabelMediumSize = "LabelMediumSize";
        public const string LabelMediumTracking = "LabelMediumTracking";
        public const string LabelMediumWeight = "LabelMediumWeight";
        public const string LabelSmallFont = "LabelSmallFont";
        public const string LabelSmallLineHeight = "LabelSmallLineHeight";
        public const string LabelSmallSize = "LabelSmallSize";
        public const string LabelSmallTracking = "LabelSmallTracking";
        public const string LabelSmallWeight = "LabelSmallWeight";

        public const string BodyLargeFont = "BodyLargeFont";
        public const string BodyLargeLineHeight = "BodyLargeLineHeight";
        public const string BodyLargeSize = "BodyLargeSize";
        public const string BodyLargeTracking = "BodyLargeTracking";
        public const string BodyLargeWeight = "BodyLargeWeight";
        public const string BodyMediumFont = "BodyMediumFont";
        public const string BodyMediumLineHeight = "BodyMediumLineHeight";
        public const string BodyMediumSize = "BodyMediumSize";
        public const string BodyMediumTracking = "BodyMediumTracking";
        public const string BodyMediumWeight = "BodyMediumWeight";
        public const string BodySmallFont = "BodySmallFont";
        public const string BodySmallLineHeight = "BodySmallLineHeight";
        public const string BodySmallSize = "BodySmallSize";
        public const string BodySmallTracking = "BodySmallTracking";
        public const string BodySmallWeight = "BodySmallWeight";

        public const string DisplayLarge = "DisplayLarge";
        public const string DisplayMedium = "DisplayMedium";
        public const string DisplaySmall = "DisplaySmall";
        public const string HeadlineLarge = "HeadlineLarge";
        public const string HeadlineMedium = "HeadlineMedium";
        public const string HeadlineSmall = "HeadlineSmall";
        public const string TitleLarge = "TitleLarge";
        public const string TitleMedium = "TitleMedium";
        public const string TitleSmall = "TitleSmall";
        public const string LabelLarge = "LabelLarge";
        public const string LabelMedium = "LabelMedium";
        public const string LabelSmall = "LabelSmall";
        public const string BodyLarge = "BodyLarge";
        public const string BodyMedium = "BodyMedium";
        public const string BodySmall = "BodySmall";

        public Typography()
        {
            this[DisplayLargeFont] = "Rubik-Bold";
            this[DisplayLargeLineHeight] = 1;
            this[DisplayLargeSize] = SetOnPlatform<double>(57, 57);
            this[DisplayLargeTracking] = 1;
            this[DisplayLargeWeight] = 0;
            this[DisplayMediumFont] = "Rubik-Medium";
            this[DisplayMediumLineHeight] = 1;
            this[DisplayMediumSize] = SetOnPlatform<double>(45, 45);
            this[DisplayMediumTracking] = 1;
            this[DisplayMediumWeight] = 0;
            this[DisplaySmallFont] = "Rubik-Regular";
            this[DisplaySmallLineHeight] = 1;
            this[DisplaySmallSize] = SetOnPlatform<double>(36, 36);
            this[DisplaySmallTracking] = 1;
            this[DisplaySmallWeight] = 0;

            this[HeadlineLargeFont] = "Rubik-Bold";
            this[HeadlineLargeLineHeight] = 1;
            this[HeadlineLargeSize] = SetOnPlatform<double>(32, 32);
            this[HeadlineLargeTracking] = 1;
            this[HeadlineLargeWeight] = 0;
            this[HeadlineMediumFont] = "Rubik-Medium";
            this[HeadlineMediumLineHeight] = 1;
            this[HeadlineMediumSize] = SetOnPlatform<double>(28, 28);
            this[HeadlineMediumTracking] = 1;
            this[HeadlineMediumWeight] = 0;
            this[HeadlineSmallFont] = "Rubik-Regular";
            this[HeadlineSmallLineHeight] = 1;
            this[HeadlineSmallSize] = SetOnPlatform<double>(24, 24);
            this[HeadlineSmallTracking] = 1;
            this[HeadlineSmallWeight] = 0;

            this[TitleLargeFont] = "Rubik-Medium";
            this[TitleLargeLineHeight] = 1;
            this[TitleLargeSize] = SetOnPlatform<double>(22, 22);
            this[TitleLargeTracking] = 1;
            this[TitleLargeWeight] = 0;
            this[TitleMediumFont] = "Rubik-Regular";
            this[TitleMediumLineHeight] = 1;
            this[TitleMediumSize] = SetOnPlatform<double>(16, 16);
            this[TitleMediumTracking] = 1;
            this[TitleMediumWeight] = 0;
            this[TitleSmallFont] = "Rubik-Regular";
            this[TitleSmallLineHeight] = 1;
            this[TitleSmallSize] = SetOnPlatform<double>(14, 14);
            this[TitleSmallTracking] = 1;
            this[TitleSmallWeight] = 0;

            this[LabelLargeFont] = "Rubik-Regular";
            this[LabelLargeLineHeight] = 1;
            this[LabelLargeSize] = SetOnPlatform<double>(14, 14);
            this[LabelLargeTracking] = 1;
            this[LabelLargeWeight] = 0;
            this[LabelMediumFont] = "Rubik-Regular";
            this[LabelMediumLineHeight] = 1;
            this[LabelMediumSize] = SetOnPlatform<double>(12, 12);
            this[LabelMediumTracking] = 1;
            this[LabelMediumWeight] = 0;
            this[LabelSmallFont] = "Rubik-Regular";
            this[LabelSmallLineHeight] = 1;
            this[LabelSmallSize] = SetOnPlatform<double>(11, 11);
            this[LabelSmallTracking] = 1;
            this[LabelSmallWeight] = 0;

            this[BodyLargeFont] = "Rubik-Medium";
            this[BodyLargeLineHeight] = 1;
            this[BodyLargeSize] = SetOnPlatform<double>(16, 16);
            this[BodyLargeTracking] = 1;
            this[BodyLargeWeight] = 0;
            this[BodyMediumFont] = "Rubik-Medium";
            this[BodyMediumLineHeight] = 1;
            this[BodyMediumSize] = SetOnPlatform<double>(14, 14);
            this[BodyMediumTracking] = 1;
            this[BodyMediumWeight] = 0;
            this[BodySmallFont] = "Rubik-Regular";
            this[BodySmallLineHeight] = 1;
            this[BodySmallSize] = SetOnPlatform<double>(12, 12);
            this[BodySmallTracking] = 1;
            this[BodySmallWeight] = 0;
            
            this[DisplayLarge] = SetTypographicStyle(DisplayLargeFont, DisplayLargeLineHeight, DisplayLargeSize, DisplayLargeTracking, DisplayLargeWeight);
            this[DisplayMedium] = SetTypographicStyle(DisplayMediumFont, DisplayMediumLineHeight, DisplayMediumSize, DisplayMediumTracking, DisplayMediumWeight);
            this[DisplaySmall] = SetTypographicStyle(DisplaySmallFont, DisplaySmallLineHeight, DisplaySmallSize, DisplaySmallTracking, DisplaySmallWeight);

            this[HeadlineLarge] = SetTypographicStyle(HeadlineLargeFont, HeadlineLargeLineHeight, HeadlineLargeSize, HeadlineLargeTracking, HeadlineLargeWeight);
            this[HeadlineMedium] = SetTypographicStyle(HeadlineMediumFont, HeadlineMediumLineHeight, HeadlineMediumSize, HeadlineMediumTracking, HeadlineMediumWeight);
            this[HeadlineSmall] = SetTypographicStyle(HeadlineSmallFont, HeadlineSmallLineHeight, HeadlineSmallSize, HeadlineSmallTracking, HeadlineSmallWeight);

            this[TitleLarge] = SetTypographicStyle(TitleLargeFont, TitleLargeLineHeight, TitleLargeSize, TitleLargeTracking, TitleLargeWeight);
            this[TitleMedium] = SetTypographicStyle(TitleMediumFont, TitleMediumLineHeight, TitleMediumSize, TitleMediumTracking, TitleMediumWeight);
            this[TitleSmall] = SetTypographicStyle(TitleSmallFont, TitleSmallLineHeight, TitleSmallSize, TitleSmallTracking, TitleSmallWeight);

            this[LabelLarge] = SetTypographicStyle(LabelLargeFont, LabelLargeLineHeight, LabelLargeSize, LabelLargeTracking, LabelLargeWeight);
            this[LabelMedium] = SetTypographicStyle(LabelMediumFont, LabelMediumLineHeight, LabelMediumSize, LabelMediumTracking, LabelMediumWeight);
            this[LabelSmall] = SetTypographicStyle(LabelSmallFont, LabelSmallLineHeight, LabelSmallSize, LabelSmallTracking, LabelSmallWeight);

            this[BodyLarge] = SetTypographicStyle(BodyLargeFont, BodyLargeLineHeight, BodyLargeSize, BodyLargeTracking, BodyLargeWeight);
            this[BodyMedium] = SetTypographicStyle(BodyMediumFont, BodyMediumLineHeight, BodyMediumSize, BodyMediumTracking, BodyMediumWeight);
            this[BodySmall] = SetTypographicStyle(BodySmallFont, BodySmallLineHeight, BodySmallSize, BodySmallTracking, BodySmallWeight);
        }
        
        private static Style SetTypographicStyle(string fontKey, string lineHeightKey, string sizeKey, string trackingKey, string weightKey)
            => new (typeof(Element))
            {
                Setters =
                {
                    new Setter { Property = Label.FontFamilyProperty, Value = new DynamicResource(fontKey) },
                    new Setter { Property = Label.LineHeightProperty, Value = new DynamicResource(lineHeightKey) },
                    new Setter { Property = Label.FontSizeProperty, Value = new DynamicResource(sizeKey) },
                    new Setter { Property = Label.CharacterSpacingProperty, Value = new DynamicResource(trackingKey) },
                    // Font Weight not handled

                    new Setter { Property = Span.FontFamilyProperty, Value = new DynamicResource(fontKey) },
                    new Setter { Property = Span.LineHeightProperty, Value = new DynamicResource(lineHeightKey) },
                    new Setter { Property = Span.FontSizeProperty, Value = new DynamicResource(sizeKey) },
                    new Setter { Property = Span.CharacterSpacingProperty, Value = new DynamicResource(trackingKey) },
                    // Font Weight not handled
                    
                    new Setter { Property = Button.FontFamilyProperty, Value = new DynamicResource(fontKey) },
                    new Setter { Property = Button.FontSizeProperty, Value = new DynamicResource(sizeKey) },
                    new Setter { Property = Button.CharacterSpacingProperty, Value = new DynamicResource(trackingKey) },
                    // Font Weight not handled
                    // Line Height not handled
                    
                    new Setter { Property = Entry.FontFamilyProperty, Value = new DynamicResource(fontKey) },
                    new Setter { Property = Entry.FontSizeProperty, Value = new DynamicResource(sizeKey) },
                    new Setter { Property = Entry.CharacterSpacingProperty, Value = new DynamicResource(trackingKey) },
                    // Font Weight not handled
                    // Line Height not handled
                    
                    new Setter { Property = Editor.FontFamilyProperty, Value = new DynamicResource(fontKey) },
                    new Setter { Property = Editor.FontSizeProperty, Value = new DynamicResource(sizeKey) },
                    new Setter { Property = Editor.CharacterSpacingProperty, Value = new DynamicResource(trackingKey) },
                    // Font Weight not handled
                    // Line Height not handled
                }
            };

        private static OnPlatform<T> SetOnPlatform<T>(T android, T ios)
            => new() { Platforms = { new On { Platform = new List<string> { "Android" }, Value = android }, new On { Platform = new List<string> { "iOS" }, Value = ios } } };
    }

    public class BindingProxy<T> : Element
    {
        #region Value Property
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            propertyName: nameof(Value),
            returnType: typeof(T),
            declaringType: typeof(BindingProxy<T>),
            defaultValue: default(T),
            defaultBindingMode: BindingMode.OneWay);

        public T Value
        {
            get => (T)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        #endregion
    }
}
