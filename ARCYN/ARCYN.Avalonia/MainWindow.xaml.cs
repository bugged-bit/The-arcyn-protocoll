using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ARCYN.UI.Models;
using ARCYN.UI.Services;

namespace ARCYN.Avalonia;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly List<ModeConfig> _modes = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadModes();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void LoadModes()
    {
        var cfg = _configService.Load();
        if (cfg?.Modes != null)
        {
            _modes.AddRange(cfg.Modes);
            this.DataContext = _modes; // Bind list box to collection
        }
        else
        {
            LogService.WriteStatic("No configuration found; starting with empty mode list.");
            this.DataContext = _modes;
        }
    }

    private void OnLaunchClicked(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("ModesList");
        if (listBox?.SelectedItem is ModeConfig mode)
        {
            LogService.WriteStatic($"Launching mode '{mode.Name}'");
            foreach (var target in mode.Targets)
            {
                if (LaunchService.TryPrepare(target, out var psi, out var err))
                {
                    LogService.WriteStatic($"Launching {target.Kind}: {psi.FileName} {psi.Arguments}");
                    try
                    {
                        var proc = System.Diagnostics.Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteStatic($"Failed to start {target.Kind}: {ex.Message}");
                    }
                }
                else
                {
                    LogService.WriteStatic($"Target validation failed: {err}");
                }
            }
        }
        else
        {
            LogService.WriteStatic("No mode selected for launch.");
        }
    }
}
