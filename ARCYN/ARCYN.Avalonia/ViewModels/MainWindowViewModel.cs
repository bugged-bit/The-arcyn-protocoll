using System.Diagnostics;
using System.Windows.Input;
using ARCYN.Core.Models;
using ARCYN.Core.Services;
using ARCYN.UI.Services;

namespace ARCYN.Avalonia.ViewModels;

/// <summary>
/// ViewModel for MainWindow. Owns application state, config, mode selection,
/// and exposes bindable properties and commands for the HUD UI.
/// Code-behind subscribes to events for UI-specific operations (animations, etc.).
/// </summary>
public sealed class MainWindowViewModel : BaseViewModel, IDisposable
{
    private readonly AppState _state;
    private readonly LogService _log;
    private readonly ConfigService _config;
    private readonly ModeService _modes;
    private readonly ThemeService _theme;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();

    private CancellationTokenSource? _launchCts;
    private bool _disposed;
    private bool _isLaunching;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _launchCts?.Cancel();
        _launchCts?.Dispose();
        _uptime.Stop();
    }

    // ── Config state ───────────────────────────────────────────────
    private bool _reducedEffects;
    private bool _compactMode;
    private bool _alwaysOnTop;
    private bool _closeOnLaunch;
    private int _idleTimeout;

    // ── Observable properties ──────────────────────────────────────
    private string _phaseLabel = "BOOT";
    private string _modeCount = "00";
    private string _processStrip = "PROC 00  -  ACTIVE STANDBY";
    private string _footerHint = "[1] select  -  ENTER launch  -  ESC close";
    private string _footerIdle = string.Empty;
    private string _headerCpu = "CPU --%";
    private string _headerRam = "RAM --%";
    private string _headerSys = "ONLINE";
    private string _headerUptime = "UP 00:00:00";
    private string _timeDisplay = "00:00:00";
    private string _runtimeStrip = "RUNTIME 00:00:00";
    private string _systemStrip = "SYS ONLINE  -  HUD STABLE";
    private string _launchModeLabel = string.Empty;
    private string _launchSessionLabel = string.Empty;
    private string _launchStatus = "Launching";
    private string _launchProgressText = "PROC 00/00";
    private string _launchCpu = "CPU --%";
    private string _launchRam = "RAM --%";
    private string _launchTargetCount = "00 TARGETS";
    private string _launchRecent = string.Empty;
    private string _launchFeedText = string.Empty;
    private double _launchProgressScale;
    private bool _modePanelVisible = true;
    private bool _launchPanelVisible;

    // ── Constructor ────────────────────────────────────────────────
    public MainWindowViewModel()
    {
        _state = new AppState();
        _log = new LogService();
        _config = new ConfigService();
        _modes = new ModeService();
        _theme = new ThemeService();

        _state.PhaseChanged += OnPhaseChanged;

        // Register commands
        SelectModeCommand = new AsyncRelayCommand(async p =>
        {
            if (p is int index) await SelectMode(index);
        }, _ => !_isLaunching);

        CancelCommand = new RelayCommand(_ => CancelLaunch(), _ => _isLaunching);
        CloseCommand = new AsyncRelayCommand(async _ => await CloseWithAnimationAsync(), _ => !_isLaunching);

        EditModeCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ModeConfig mode) await EditMode(mode);
        });

        DuplicateModeCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ModeConfig mode) await DuplicateMode(mode);
        });

        DeleteModeCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ModeConfig mode) await DeleteMode(mode);
        });

        MoveUpCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ModeConfig mode) await MoveModeUp(mode);
        });

        MoveDownCommand = new AsyncRelayCommand(async p =>
        {
            if (p is ModeConfig mode) await MoveModeDown(mode);
        });
    }

    // ── Events for code-behind subscription ───────────────────────
    public event Func<int, ModeConfig, Task>? LaunchStarting;
    public event Func<bool, Task>? LaunchCompleted;
    public event Action? ConfigChanged;
    public event Func<Task>? CloseRequested;

    // ── Public properties (bound by XAML) ──────────────────────────
    public AppState State => _state;
    public LogService Log => _log;
    public ConfigService Config => _config;
    public ModeService Modes => _modes;
    public ThemeService Theme => _theme;
    public Stopwatch Uptime => _uptime;

    public bool ReducedEffects { get => _reducedEffects; set => SetProperty(ref _reducedEffects, value); }
    public bool CompactMode { get => _compactMode; set => SetProperty(ref _compactMode, value); }
    public bool AlwaysOnTop { get => _alwaysOnTop; set => SetProperty(ref _alwaysOnTop, value); }
    public bool CloseOnLaunch { get => _closeOnLaunch; set => SetProperty(ref _closeOnLaunch, value); }
    public int IdleTimeout { get => _idleTimeout; set => SetProperty(ref _idleTimeout, value); }
    public bool IsLaunching => _isLaunching;
    public string PhaseLabel { get => _phaseLabel; set => SetProperty(ref _phaseLabel, value); }
    public string ModeCount { get => _modeCount; set => SetProperty(ref _modeCount, value); }
    public string ProcessStrip { get => _processStrip; set => SetProperty(ref _processStrip, value); }
    public string FooterHint { get => _footerHint; set => SetProperty(ref _footerHint, value); }
    public string FooterIdle { get => _footerIdle; set => SetProperty(ref _footerIdle, value); }
    public string HeaderCpu { get => _headerCpu; set => SetProperty(ref _headerCpu, value); }
    // Additional properties omitted for brevity
    // Commands omitted for brevity
    // Methods omitted for brevity
}
