using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ARCYN.UI.Converters;

/// <summary>
/// Converts a hex color string (e.g. "#D64545") to a SolidColorBrush with alpha 0x66,
/// suitable for binding to ModeConfig.Accent in the mode card sidebar.
/// </summary>
public sealed class StringToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hex = value as string;
        if (string.IsNullOrWhiteSpace(hex))
            return new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            // Semi-transparent variant matching the original AccentBarBrush (0x66 alpha)
            return new SolidColorBrush(Color.FromArgb(0x66, color.R, color.G, color.B));
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
