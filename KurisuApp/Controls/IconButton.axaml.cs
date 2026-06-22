using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace KurisuApp.Controls;

public class IconButton : Button
{
    public static readonly StyledProperty<string> IconTextProperty
        = AvaloniaProperty.Register<IconButton, string>(nameof(IconText));

    public static readonly StyledProperty<FontFamily> IconFontFamilyProperty
        = AvaloniaProperty.Register<IconButton, FontFamily>(nameof(IconFontFamily));

    public string IconText
    {
        get => GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }

    public FontFamily IconFontFamily
    {
        get => GetValue(IconFontFamilyProperty);
        set => SetValue(IconFontFamilyProperty, value);
    }
}