using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ARCYN.UI.Models;

public sealed partial class ModeConfig
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Color AccentColor => (Color)ColorConverter.ConvertFromString(Accent) ?? Color.FromRgb(0xD6, 0x45, 0x45);

    [System.Text.Json.Serialization.JsonIgnore]
    public SolidColorBrush AccentBrush => new SolidColorBrush(AccentColor);

    [System.Text.Json.Serialization.JsonIgnore]
    public SolidColorBrush AccentBarBrush => new SolidColorBrush(Color.FromArgb(0x66, AccentColor.R, AccentColor.G, AccentColor.B));
}