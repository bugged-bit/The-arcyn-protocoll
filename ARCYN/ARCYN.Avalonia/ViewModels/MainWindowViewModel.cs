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

    /// <summary>
    /// Raised when a mode is about to be launched. Code-behind should animate the UI transition.
    /// </summary>
    public event Func<int, ModeConfig, Task>? LaunchStarting;

    /// <summary>
    /// Raised after launch completes. Code-behind should close window or return to ready.
    /// </summary>
    public event Func<bool, Task>? LaunchCompleted;

    /// <summary>
    /// Raised when the config has changed. Code-behind should rebuild the dashboard.
    /// </summary>
    public event Action? ConfigChanged;

    /// <summary>
    /// Raised to request the window close with animation.
    /// </summary>
    public event Func<Task>? CloseRequested;

    // ── Public properties (bound by XAML) ──────────────────────────

    public AppState State => _state;
    public LogService Log => _log;
    public ConfigService Config => _config;
    public ModeService Modes => _modes;
    public ThemeService Theme => _theme;
    public Stopwatch Uptime => _uptime;

    public bool ReducedEffects
    {
        get => _reducedEffects;
        set => SetProperty(ref _reducedEffects, value);
    }

    public bool CompactMode
    {
        get => _compactMode;
        set => SetProperty(ref _compactMode, value);
    }

    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set => SetProperty(ref _alwaysOnTop, value);
    }

    public bool CloseOnLaunch
    {
        get => _closeOnLaunch;
        set => SetProperty(ref _closeOnLaunch, value);
    }

    public int IdleTimeout
    {
        get => _idleTimeout;
        set => SetProperty(ref _idleTimeout, value);
    }

    public bool IsLaunching => _isLaunching;

    public string PhaseLabel
    {
        get => _phaseLabel;
        set => SetProperty(ref _phaseLabel, value);
    }

    public string ModeCount
    {
        get => _modeCount;
        set => SetProperty(ref _modeCount, value);
    }

    public string ProcessStrip
    {
        get => _processStrip;
        set => SetProperty(ref _processStrip, value);
    }

    public string FooterHint
    {
        get => _footerHint;
        set => SetProperty(ref _footerHint, value);
    }

    public string FooterIdle
    {
        get => _footerIdle;
        set => SetProperty(ref _footerIdle, value);
    }

    public string HeaderCpu
    {
        get => _headerCpu;
        set => SetProperty(ref _headerCpu, value);
    }

    public string HeaderRam
    {
        get => _headerRam;
        set => SetProperty(ref _headerRam, value);
    }

    public string HeaderSys
    {
        get => _headerSys;
        set => SetProperty(ref _headerSys, value);
    }

    public string HeaderUptime
    {
        get => _headerUptime;
        set => SetProperty(ref _headerUptime, value);
    }

    public string TimeDisplay
    {
        get => _timeDisplay;
        set => SetProperty(ref _timeDisplay, value);
    }

    public string RuntimeStrip
    {
        get => _runtimeStrip;
        set => SetProperty(ref _runtimeStrip, value);
    }

    public string SystemStrip
    {
        get => _systemStrip;
        set => SetProperty(ref _systemStrip, value);
    }

    public string LaunchModeLabel
    {
        get => _launchModeLabel;
        set => SetProperty(ref _launchModeLabel, value);
    }

    public string LaunchSessionLabel
    {
        get => _launchSessionLabel;
        set => SetProperty(ref _launchSessionLabel, value);
    }

    public string LaunchStatus
    {
        get => _launchStatus;
        set => SetProperty(ref _launchStatus, value);
    }

    public string LaunchProgressText
    {
        get => _launchProgressText;
        set => SetProperty(ref _launchProgressText, value);
    }

    public string LaunchCpu
    {
        get => _launchCpu;
        set => SetProperty(ref _launchCpu, value);
    }

    public string LaunchRam
    {
        get => _launchRam;
        set => SetProperty(ref _launchRam, value);
    }

    public string LaunchTargetCount
    {
        get => _launchTargetCount;
        set => SetProperty(ref _launchTargetCount, value);
    }

    public string LaunchRecent
    {
        get => _launchRecent;
        set => SetProperty(ref _launchRecent, value);
    }

    public string LaunchFeedText
    {
        get => _launchFeedText;
        set => SetProperty(ref _launchFeedText, value);
    }

    public double LaunchProgressScale
    {
        get => _launchProgressScale;
        set => SetProperty(ref _launchProgressScale, value);
    }

    public bool ModePanelVisible
    {
        get => _modePanelVisible;
        set => SetProperty(ref _modePanelVisible, value);
    }

    public bool LaunchPanelVisible
    {
        get => _launchPanelVisible;
        set => SetProperty(ref _launchPanelVisible, value);
    }

    // ── Commands ──────────────────────────────────────────────────

    public ICommand SelectModeCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand EditModeCommand { get; }
    public ICommand DuplicateModeCommand { get; }
    public ICommand DeleteModeCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    // ── Initialization ────────────────────────────────────────────

    /// <summary>
    /// Loads config, initializes modes, and applies theme settings.
    /// Returns the loaded config so code-behind can access settings.
    /// </summary>
    public async Task<ArcynConfig?> InitializeAsync()
    {
        _log.Write("ViewModel InitializeAsync: start");

        ArcynConfig? arcynConfig = null;
        try
        {
            arcynConfig = await Task.Run(() => _config.Load());
        }
        catch (Exception ex)
        {
            _log.Write("CONFIG LOAD EXCEPTION: {0}", ex);
        }

        if (arcynConfig == null || arcynConfig.Modes.Count == 0)
        {
            _log.Write("No config found — first-run detected");
            return null;
        }

        _modes.Load(arcynConfig.Modes);
        _idleTimeout = arcynConfig.Behavior.IdleTimeoutSeconds;
        _reducedEffects = arcynConfig.Theme.ReducedEffects;
        _compactMode = arcynConfig.Theme.CompactMode;
        _alwaysOnTop = arcynConfig.Behavior.AlwaysOnTop;
        _closeOnLaunch = arcynConfig.Behavior.CloseOnLaunch;

        // Notify property changes
        OnPropertyChanged(nameof(IdleTimeout));
        OnPropertyChanged(nameof(ReducedEffects));
        OnPropertyChanged(nameof(CompactMode));
        OnPropertyChanged(nameof(AlwaysOnTop));
        OnPropertyChanged(nameof(CloseOnLaunch));

        _log.Write("Config loaded ({0} modes), reduced_effects={1}, compact={2}",
            _modes.Count, _reducedEffects, _compactMode);

        UpdateOperationalChrome();
        return arcynConfig;
    }

    // ── Phase handling ────────────────────────────────────────────

    private void OnPhaseChanged(AppPhase previous, AppPhase next)
    {
        _log.Write("State: {0} -> {1}", previous, next);
        PhaseLabel = _state.PhaseLabel;

        if (next == AppPhase.Ready)
        {
            ModePanelVisible = true;
            LaunchPanelVisible = false;
        }

        if (next == AppPhase.Closing)
        {
            _launchCts?.Cancel();
        }
    }

    public bool TransitionTo(AppPhase next) => _state.TransitionTo(next);
    public bool CanTransitionTo(AppPhase next) => _state.CanTransitionTo(next);
    public AppPhase Phase => _state.Phase;

    // ── Mode selection & launch ───────────────────────────────────

    public async Task SelectMode(int index)
    {
        if (_isLaunching) return;

        var mode = _modes.Get(index);
        if (mode == null) return;
        if (!_state.TransitionTo(AppPhase.Selecting)) return;

        _isLaunching = true;
        _modes.SelectedIndex = index;
        _log.Write("SelectMode({0}): name={1}, targets={2}", index, mode.Name, mode.ProcessCount);

        try
        {
            // Notify code-behind to animate UI transition
            if (LaunchStarting != null)
            {
                await LaunchStarting.Invoke(index, mode);
            }

            _launchModeLabel = mode.Name.ToUpperInvariant();
            _launchSessionLabel = $"MODE {mode.IndexLabel}";
            _launchStatus = "Launching";
            _launchProgressScale = 0;
            _launchProgressText = $"PROC 00/{Math.Max(mode.ProcessCount, 1):D2}";
            OnPropertyChanged(nameof(LaunchModeLabel));
            OnPropertyChanged(nameof(LaunchSessionLabel));
            OnPropertyChanged(nameof(LaunchStatus));
            OnPropertyChanged(nameof(LaunchProgressScale));
            OnPropertyChanged(nameof(LaunchProgressText));

            _state.TransitionTo(AppPhase.Launching);

            LaunchPanelVisible = true;
            LaunchTargetCount = $"{mode.Targets.Count} TARGETS";

            _launchCts?.Dispose();
            _launchCts = new CancellationTokenSource();
            var token = _launchCts.Token;

            // Launch orchestration
            var orchestrator = new LaunchOrchestrator(_log);
            LaunchFeedText = string.Empty;

            var progress = new Progress<LaunchProgress>(p =>
            {
                if (!string.IsNullOrEmpty(p.CurrentLabel))
                {
                    LaunchFeedText += $"> {p.CurrentLabel}\n";
                }
                var safeTotal = Math.Max(p.TotalTargets, 1);
                var ratio = Math.Clamp((double)p.CompletedTargets / safeTotal, 0, 1);
                LaunchProgressScale = ratio;
                LaunchProgressText = $"PROC {Math.Min(p.CompletedTargets, safeTotal):D2}/{safeTotal:D2}";
            });

            var launchResult = await orchestrator.LaunchModeAsync(mode, token, progress);

            if (launchResult.Canceled)
            {
                LaunchStatus = "Cancelled";
                _log.Write("Launch cancelled by user");
                if (LaunchCompleted != null)
                    await LaunchCompleted.Invoke(false);
                return;
            }

            bool anySuccess = launchResult.LaunchedTargets > 0;
            bool success = launchResult.Failures.Count == 0 && anySuccess;
            LaunchStatus = success ? "Ready" : anySuccess ? "Partial" : "Fault";

            LaunchRecent = launchResult.Failures.Count == 0
                ? $"Recent: {mode.LastLaunchLabel}"
                : $"Issue: {string.Join(", ", launchResult.Failures)}";

            mode.RecordLaunch(launchResult.LaunchedTargets, launchResult.TotalTargets, success);
            UpdateOperationalChrome();

            // Notify code-behind to handle post-launch (close or return to ready)
            if (LaunchCompleted != null)
                await LaunchCompleted.Invoke(success);
        }
        catch (Exception ex)
        {
            _log.Write("CRASH in SelectMode: {0}", ex);
        }
        finally
        {
            _isLaunching = false;
        }
    }

    public void CancelLaunch()
    {
        if (!_isLaunching) return;
        LaunchStatus = "Cancelling...";
        _launchCts?.Cancel();
    }

    // ── Return to ready after launch ──────────────────────────────

    public Task ReturnToReady()
    {
        if (!_state.TransitionTo(AppPhase.Ready)) return Task.CompletedTask;
        _isLaunching = false;

        ModePanelVisible = true;
        LaunchPanelVisible = false;

        OnPropertyChanged(nameof(ModePanelVisible));
        OnPropertyChanged(nameof(LaunchPanelVisible));
        return Task.CompletedTask;
    }

    // ── Context menu actions (Edit, Duplicate, Delete, Move) ──────

    public Task EditMode(ModeConfig mode)
    {
        // The code-behind handles the dialog since it creates windows.
        // This just marks that config needs saving.
        _log.Write("EditMode requested: {0}", mode.Name);
        return Task.CompletedTask;
    }

    public async Task DuplicateMode(ModeConfig mode)
    {
        var clone = new ModeConfig
        {
            Name = mode.Name + " 2",
            Description = mode.Description,
            Accent = mode.Accent
        };
        clone.Apps.AddRange(mode.Apps);
        clone.Websites.AddRange(mode.Websites);
        clone.Folders.AddRange(mode.Folders);

        var idx = _modes.IndexOf(mode);
        _modes.Insert(idx + 1, clone);
        await SaveCurrentConfig();
        ConfigChanged?.Invoke();
        UpdateOperationalChrome();
    }

    public async Task DeleteMode(ModeConfig mode)
    {
        if (_modes.Count <= 1) return;
        var idx = _modes.IndexOf(mode);
        _modes.RemoveAt(idx);
        await SaveCurrentConfig();
        ConfigChanged?.Invoke();
        UpdateOperationalChrome();
    }

    public async Task MoveModeUp(ModeConfig mode)
    {
        var idx = _modes.IndexOf(mode);
        if (_modes.MoveUp(idx))
        {
            await SaveCurrentConfig();
            ConfigChanged?.Invoke();
            UpdateOperationalChrome();
        }
    }

    public async Task MoveModeDown(ModeConfig mode)
    {
        var idx = _modes.IndexOf(mode);
        if (_modes.MoveDown(idx))
        {
            await SaveCurrentConfig();
            ConfigChanged?.Invoke();
            UpdateOperationalChrome();
        }
    }

    public async Task SaveCurrentConfig()
    {
        var cfg = new ArcynConfig
        {
            Modes = [.. _modes.Modes],
            Theme = new ThemeConfig
            {
                ReducedEffects = _reducedEffects,
                CompactMode = _compactMode
            },
            Behavior = new BehaviorConfig { IdleTimeoutSeconds = _idleTimeout }
        };
        await _config.SaveAsync(cfg);
    }

    // ── UI chrome updates ─────────────────────────────────────────

    public void UpdateOperationalChrome()
    {
        var activeLabel = _modes.ActiveLabel;
        ModeCount = _modes.Count.ToString("D2");
        ProcessStrip = $"PROC {_modes.TotalProcesses:D2}  -  ACTIVE {activeLabel}";
        FooterHint = $"{_modes.ShortcutHint} select  -  ENTER launch  -  ESC close";
        var sel = _modes.SelectedMode;
        LaunchRecent = sel != null ? $"Recent: {sel.LastLaunchLabel}" : "Recent: none";
    }

    /// <summary>
    /// Called each render tick (from code-behind) to update time-based properties.
    /// </summary>
    public void UpdateTimeDisplay()
    {
        var now = DateTime.Now;
        TimeDisplay = now.ToString("HH:mm:ss");
        HeaderUptime = $"UP {_uptime.Elapsed:hh\\:mm\\:ss}";
        RuntimeStrip = $"RUNTIME {_uptime.Elapsed:hh\\:mm\\:ss}";
    }

    /// <summary>
    /// Called each render tick to update telemetry-based properties.
    /// </summary>
    public void UpdateTelemetry(TelemetryMonitor? telemetry)
    {
        if (telemetry == null) return;
        var cpu = telemetry.CpuPercent;
        var ram = telemetry.RamPercent;

        HeaderCpu = $"CPU {cpu,4:F1}%";
        HeaderRam = $"RAM {ram,4:F1}%";
        HeaderSys = cpu < 75 && ram < 85 ? "ONLINE" : "LOAD";
        SystemStrip = cpu < 75 && ram < 85
            ? $"SYS {HeaderSys}  -  HUD STABLE"
            : $"SYS {HeaderSys}  -  CPU {cpu:F0}% / RAM {ram:F0}%";

        if (_state.Phase == AppPhase.Launching)
        {
            LaunchCpu = $"CPU {cpu:F1}%";
            LaunchRam = $"RAM {ram:F1}%";
        }
    }

    /// <summary>
    /// Updates the idle display text.
    /// </summary>
    public void UpdateIdleDisplay(int idleElapsedMs)
    {
        var idleSec = idleElapsedMs > 0 ? (int)(idleElapsedMs / 1000) : 0;
        FooterIdle = idleSec > 0 ? $"idle {idleSec}s" : string.Empty;
    }

    // ── Close animation request ───────────────────────────────────

    private async Task CloseWithAnimationAsync()
    {
        if (CloseRequested != null)
            await CloseRequested.Invoke();
    }

    // ── Disposal ──────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _log.Write("ViewModel disposed");
        _state.PhaseChanged -= OnPhaseChanged;
        _launchCts?.Cancel();
        _launchCts?.Dispose();
        _log.Dispose();
        _uptime.Stop();
    }
}
