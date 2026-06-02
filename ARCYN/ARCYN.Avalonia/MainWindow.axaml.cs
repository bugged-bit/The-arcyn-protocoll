using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using ARCYN.Core.Models;
using ARCYN.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ARCYN.Avalonia;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private ArcynConfig? _config;

    public MainWindow()
    {
        InitializeComponent();
        ConfigPathText.Text = ConfigService.DefaultConfigPath;
        LoadConfig();
    }

    private void LoadConfig()
    {
        _config = _configService.Load();
        ModesPanel.Children.Clear();

        if (_config == null || _config.Modes.Count == 0)
        {
            StatusText.Text = "No modes are configured yet. Create ~/.config/ARCYN/arcyn.json or copy ARCYN/example.arcyn.json there, then reload.";
            return;
        }

        StatusText.Text = $"Loaded {_config.Modes.Count} mode(s). Click a mode to launch its apps, folders, and websites.";

        for (var i = 0; i < _config.Modes.Count; i++)
        {
            var mode = _config.Modes[i];
            mode.Index = i + 1;
            ModesPanel.Children.Add(CreateModeRow(mode));
        }
    }

    private Control CreateModeRow(ModeConfig mode)
    {
        var title = new TextBlock
        {
            Text = $"{mode.Index}. {mode.Name}",
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush.Parse("#FBFFFFFF")
        };

        var details = new TextBlock
        {
            Text = BuildModeDetails(mode),
            Foreground = Brush.Parse("#B8C0C8"),
            TextWrapping = TextWrapping.Wrap
        };

        var launchButton = new Button
        {
            Content = "Launch",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 92,
            Tag = mode
        };
        launchButton.Click += LaunchMode_Click;

        var content = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var textStack = new StackPanel { Spacing = 4 };
        textStack.Children.Add(title);
        if (!string.IsNullOrWhiteSpace(mode.Description))
        {
            textStack.Children.Add(new TextBlock
            {
                Text = mode.Description,
                Foreground = Brush.Parse("#D7DCE2"),
                TextWrapping = TextWrapping.Wrap
            });
        }
        textStack.Children.Add(details);

        Grid.SetColumn(textStack, 0);
        Grid.SetColumn(launchButton, 1);
        content.Children.Add(textStack);
        content.Children.Add(launchButton);

        return new Border
        {
            Padding = new global::Avalonia.Thickness(14),
            CornerRadius = new global::Avalonia.CornerRadius(6),
            BorderBrush = Brush.Parse("#2FD64545"),
            BorderThickness = new global::Avalonia.Thickness(1),
            Background = Brush.Parse("#20242A"),
            Child = content
        };
    }

    private static string BuildModeDetails(ModeConfig mode)
    {
        var parts = new List<string>();
        if (mode.Apps.Count > 0) parts.Add($"{mode.Apps.Count} app(s)");
        if (mode.Websites.Count > 0) parts.Add($"{mode.Websites.Count} website(s)");
        if (mode.Folders.Count > 0) parts.Add($"{mode.Folders.Count} folder(s)");
        return parts.Count == 0 ? "No targets configured" : string.Join(" | ", parts);
    }

    private async void LaunchMode_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ModeConfig mode })
            return;

        await LaunchModeAsync(mode);
    }

    private async Task LaunchModeAsync(ModeConfig mode)
    {
        var failures = new List<string>();
        var launched = 0;

        StatusText.Text = $"Launching {mode.Name}...";

        foreach (var target in mode.Targets)
        {
            if (!LaunchService.TryPrepare(target, out var psi, out var error))
            {
                failures.Add($"{target.DisplayLabel}: {error}");
                continue;
            }

            try
            {
                Process.Start(psi);
                launched++;
            }
            catch (Exception ex)
            {
                failures.Add($"{target.DisplayLabel}: {ex.Message}");
            }

            await Task.Yield();
        }

        StatusText.Text = failures.Count == 0
            ? $"Launched {launched} target(s) for {mode.Name}."
            : $"Launched {launched} target(s). Failed: {string.Join("; ", failures)}";
    }

    private void ReloadConfig_Click(object? sender, RoutedEventArgs e)
    {
        LoadConfig();
    }

    private void OpenConfigFolder_Click(object? sender, RoutedEventArgs e)
    {
        var configDir = Path.GetDirectoryName(ConfigService.DefaultConfigPath);
        if (string.IsNullOrWhiteSpace(configDir))
            return;

        Directory.CreateDirectory(configDir);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = configDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not open config folder: {ex.Message}";
        }
    }
}
