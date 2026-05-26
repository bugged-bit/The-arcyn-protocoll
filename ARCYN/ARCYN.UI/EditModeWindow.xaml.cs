using System.Windows;
using ARCYN.UI.Models;

namespace ARCYN.UI;

public partial class EditModeWindow : Window
{
    private readonly ModeConfig _mode;

    private static readonly Dictionary<string, string> ColorMap = new()
    {
        ["ColorRed"] = "#D64545", ["ColorBlue"] = "#4580D6",
        ["ColorGreen"] = "#45D680", ["ColorOrange"] = "#D6A045",
        ["ColorPurple"] = "#A045D6"
    };

    public EditModeWindow(ModeConfig mode)
    {
        InitializeComponent();
        _mode = mode;
        NameInput.Text = mode.Name;
        DescInput.Text = mode.Description;
        SetAccentRadio(mode.Accent);
    }

    private void SetAccentRadio(string hex)
    {
        ColorRed.IsChecked = hex == "#D64545";
        ColorBlue.IsChecked = hex == "#4580D6";
        ColorGreen.IsChecked = hex == "#45D680";
        ColorOrange.IsChecked = hex == "#D6A045";
        ColorPurple.IsChecked = hex == "#A045D6";
        if (!ColorRed.IsChecked == true && !ColorBlue.IsChecked == true &&
            !ColorGreen.IsChecked == true && !ColorOrange.IsChecked == true &&
            !ColorPurple.IsChecked == true)
            ColorRed.IsChecked = true;
    }

    private string GetSelectedAccent()
    {
        if (ColorRed.IsChecked == true) return "#D64545";
        if (ColorBlue.IsChecked == true) return "#4580D6";
        if (ColorGreen.IsChecked == true) return "#45D680";
        if (ColorOrange.IsChecked == true) return "#D6A045";
        if (ColorPurple.IsChecked == true) return "#A045D6";
        return "#D64545";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            NameInput.Focus();
            return;
        }

        _mode.Name = name.ToUpperInvariant();
        _mode.Description = DescInput.Text.Trim();
        _mode.Accent = GetSelectedAccent();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
