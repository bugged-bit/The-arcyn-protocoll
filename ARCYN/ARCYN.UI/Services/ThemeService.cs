using System.Windows;
using System.Windows.Media;

namespace ARCYN.UI.Services;

public sealed class ThemeService
{
    private static readonly Dictionary<string, string> PresetAccents = new()
    {
        ["Classic Red"] = "#D64545",
        ["Cyber Blue"] = "#4580D6",
        ["Matrix Green"] = "#45D680",
        ["Phosphor Orange"] = "#D6A045",
        ["Void Purple"] = "#A045D6"
    };

    public static IReadOnlyDictionary<string, string> Presets => PresetAccents;

    public Brush AccentBright { get; private set; } = Brushes.White;
    public Brush AccentLight { get; private set; } = Brushes.White;
    public Brush TextPrimary { get; private set; } = Brushes.White;

    public void LoadResources(FrameworkElement element)
    {
        AccentBright = (element.TryFindResource("AccentBrightBrush") as Brush)
            ?? new SolidColorBrush(Color.FromRgb(0xF0, 0x80, 0x80));
        AccentLight = (element.TryFindResource("AccentLightBrush") as Brush)
            ?? new SolidColorBrush(Color.FromRgb(0xF7, 0xA0, 0xA0));
        TextPrimary = (element.TryFindResource("TextPrimaryBrush") as Brush)
            ?? Brushes.White;
    }

    public static Color ParseColor(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            return Color.FromRgb(0xD6, 0x45, 0x45);
        }
    }

    public static SolidColorBrush HexToBrush(string hex)
    {
        return new SolidColorBrush(ParseColor(hex));
    }
}
