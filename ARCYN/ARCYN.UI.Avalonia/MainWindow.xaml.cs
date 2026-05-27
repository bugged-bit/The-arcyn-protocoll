using Avalonia.Controls;
using ARCYN.UI.Models;
using ARCYN.UI.Services;
using System.Collections.Generic;

namespace ARCYN.UI.Avalonia;

public partial class MainWindow : Window
{
    private readonly ModeService _modeService;
    private readonly ConfigService _configService;
    private readonly LaunchOrchestrator _launchOrchestrator;
    private readonly LogService _logService;

    public MainWindow()
    {
        InitializeComponent();

        // Load configuration
        _configService = new ConfigService();
        var config = _configService.Load();
        var modes = config?.Modes ?? new List<ModeConfig>();

        // Initialise services
        var appState = new ARCYN.UI.AppState();
        _modeService = new ModeService(appState);
        _modeService.Load(modes);

        // Populate UI list
        ModeList.Items = _modeService.Modes;

        _logService = new LogService();
        _launchOrchestrator = new LaunchOrchestrator(_logService);

        // Simple launch on selection
        ModeList.SelectionChanged += (_, __) =>
        {
            if (ModeList.SelectedItem is ModeConfig mode)
            {
                var cts = new System.Threading.CancellationTokenSource();
                _ = _launchOrchestrator.LaunchModeAsync(mode, cts.Token);
            }
        };
    }
}