using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ARCYN.UI.Models;
using ARCYN.UI.Services;

namespace ARCYN.UI;

public partial class MainWindow : Window, IDisposable, RenderService.ISubscriber
{
    private readonly AppState _state = new();
    private readonly LogService _log;
    private readonly ConfigService _config = new();
    private readonly ModeService _modes;
    private readonly ThemeService _theme = new();
    private readonly RenderService _render;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly List<ContentControl> _cardWrappers = [];
    private readonly List<Button> _cardButtons = [];
    private readonly List<TrailParticle> _cursorTrail = [];
    // Reuse a StringBuilder for launch‑feed text to avoid per‑target string allocations.
    private readonly System.Text.StringBuilder _launchFeedBuilder = new();

    private CancellationTokenSource? _lifeCts;
    private CancellationTokenSource? _launchCts;
    private DispatcherTimer? _idleTimer;
    private DispatcherTimer? _launchDotTimer;
    private ParticleEngine? _particles;
    private TelemetryMonitor? _telemetry;

    private bool _disposed;
    private bool _isClosing;
    private bool _isLaunching;
    private bool _mouseMoved;

    private long _accelAmbient;
    private long _accelParticle;
    private long _accelTelemetry;
    private long _accelTime;
    private long _accelTrail;
    private long _idleElapsed;

    private bool _reducedEffects;
    private bool _compactMode;
        private bool _alwaysOnTop;
        private bool _closeOnLaunch;

    private double _ambientPhase;
    private double _spinnerAngle;
    private int _launchDotIndex;
    private int _trailSkip;
    private int _idleTimeout;

    private Point _lastMousePos;

    public MainWindow()
    {
        InitializeComponent();

        _modes = new ModeService(_state);
        _log = new LogService();
        _render = new RenderService();
        _lifeCts = new CancellationTokenSource();
        _launchCts = new CancellationTokenSource();

        _state.PhaseChanged += OnPhaseChanged;
        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
        Closed += (_, _) => Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);

            try
            {
                NativeMethods.EnableAcrylic(hwnd, 0xE20A0A0A);
            }
            catch
            {
                _log.Write("Acrylic enable skipped");
            }

            _log.Write("OnLoaded: start");
            Activate();
            Focus();
            Keyboard.Focus(this);

            _theme.LoadResources(this);

            // Load config on background thread to avoid UI freeze
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
                _state.TransitionTo(AppPhase.Closing);
                Close();
                return;
            }

            _modes.Load(arcynConfig.Modes);
            _idleTimeout = arcynConfig.Behavior.IdleTimeoutSeconds;
            _reducedEffects = arcynConfig.Theme.ReducedEffects;
            _compactMode = arcynConfig.Theme.CompactMode;
            _alwaysOnTop = arcynConfig.Behavior.AlwaysOnTop;
            _closeOnLaunch = arcynConfig.Behavior.CloseOnLaunch;
            Topmost = _alwaysOnTop;
            _log.Write("Config loaded ({0} modes), reduced_effects={1}, compact={2}",
                _modes.Count, _reducedEffects, _compactMode);

            // Build the visual dashboard from the loaded modes before we interact with the UI
            BuildDashboard();

            _telemetry = new TelemetryMonitor();
            _particles = new ParticleEngine(ParticleCanvas);

            _render.Subscribe(this);
            _render.Start();
            if (!_reducedEffects)
            {
                _particles.Start();
                InitCursorTrail();
            }
            else
            {
                ScanlinesOverlay.Visibility = Visibility.Collapsed;
                AmbientGlow.Visibility = Visibility.Collapsed;
            }
            UpdateOperationalChrome();

            _log.Write("OnLoaded: runtime initialized");

            await PlayStartupSequence(_lifeCts!.Token);
            _log.Write("OnLoaded: boot sequence done");
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in OnLoaded: {0}", ex);
            // Fail fast – let the application shut down gracefully
            _state.TransitionTo(AppPhase.Closing);
            Close();
        }
    }


    private void OnDeactivated(object? sender, EventArgs e)
    {
        _log.Write("Window deactivated while Phase={0}", _state.Phase);
    }

    void RenderService.ISubscriber.OnRenderTick(long dt)
    {
        if (_disposed || _state.Phase == AppPhase.Closing)
            return;

        _accelParticle += dt;
        if (_accelParticle >= 30)
        {
            _accelParticle -= 30;
            _particles?.Tick();
        }

        if (!_reducedEffects)
        {
            _accelAmbient += dt;
            if (_accelAmbient >= 30)
            {
                _accelAmbient = 0;
                _ambientPhase += 0.025;
                AmbientGlow.Opacity = Math.Clamp(0.26 + Math.Sin(_ambientPhase) * 0.12, 0.08, 0.45);
            }

            _accelTrail += dt;
            if (_accelTrail >= 16 && _state.Phase == AppPhase.Ready)
            {
                _accelTrail = 0;
                UpdateCursorTrail();
            }
        }

        _accelTelemetry += dt;
        if (_accelTelemetry >= 1000 && _state.Phase != AppPhase.Boot)
        {
            _accelTelemetry -= 1000;
            _telemetry?.Sample();
            UpdateTelemetryUI();
        }

        _accelTime += dt;
        if (_accelTime >= 1000)
        {
            _accelTime -= 1000;
            TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            HeaderUptime.Text = $"UP {_uptime.Elapsed:hh\\:mm\\:ss}";
            RuntimeStrip.Text = $"RUNTIME {_uptime.Elapsed:hh\\:mm\\:ss}";
        }

        if (_state.Phase == AppPhase.Launching)
        {
            _spinnerAngle = (_spinnerAngle + dt * 0.15) % 360;
            SpinnerRotation.Angle = _spinnerAngle;
        }
    }

    private void OnPhaseChanged(AppPhase previous, AppPhase next)
    {
        _log.Write("State: {0} -> {1}", previous, next);
        PhaseLabel.Text = _state.PhaseLabel;
        UpdateOperationalChrome();

        if (next == AppPhase.Ready)
        {
            ModePanel.Opacity = 1;
            ModePanel.IsHitTestVisible = true;
            LaunchPanel.Visibility = Visibility.Collapsed;
        }

        if (next == AppPhase.Closing)
        {
            _particles?.Stop();
            _render.Stop();
        }
    }

    private async Task PlayStartupSequence(CancellationToken ct)
    {
        _state.TransitionTo(AppPhase.Boot);

        if (_reducedEffects)
        {
            BootOverlay.Visibility = Visibility.Collapsed;
            MainHUD.Opacity = 1;
            foreach (var wrapper in _cardWrappers)
            {
                if (ct.IsCancellationRequested) return;
                wrapper.Opacity = 1;
            }
            FooterHint.Opacity = 1;
            FooterIdle.Opacity = 1;
            _state.TransitionTo(AppPhase.Ready);
            StartIdleTimer();
            return;
        }

        var flashAnim = new DoubleAnimation(0.08, 0, TimeSpan.FromMilliseconds(350))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        StartupFlash.BeginAnimation(UIElement.OpacityProperty, flashAnim);

        BootOverlay.Visibility = Visibility.Visible;
        BootOverlay.Opacity = 0;
        MainHUD.Opacity = 0;
        FooterHint.Opacity = 0;
        FooterIdle.Opacity = 0;

        await PlayShellExpansion(ct);
        if (ct.IsCancellationRequested) return;

        AnimationService.FadeIn(BootOverlay, 120);
        await DelaySafe(80, ct);
        if (ct.IsCancellationRequested) return;

        await TypeText(BootTitle, "A R C Y N", 25, ct);
        if (ct.IsCancellationRequested) return;

        await TypeText(BootSubtitle, "SYSTEM BOOT SEQUENCE  -  v1.0.0", 15, ct);
        if (ct.IsCancellationRequested) return;

        var progressAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(900))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        BootProgress.BeginAnimation(ScaleTransform.ScaleXProperty, progressAnim);

        string[] bootMessages =
        [
            "LINKING LAUNCH PROFILES...",
            "TELEMETRY CHANNEL ONLINE",
            "PROCESS TABLE VERIFIED",
            "TACTICAL HUD STABLE",
            "SYSTEM CONFIGURED"
        ];
        string[] bootPercentages = ["24%", "47%", "68%", "89%", "100%"];

        foreach (var (message, index) in bootMessages.Select((value, idx) => (value, idx)))
        {
            if (ct.IsCancellationRequested) return;
            BootLog.Text += "\n> ";
            await TypeText(BootLog, message, 12, ct);
            BootPercent.Text = bootPercentages[index];
            await DelaySafe(100, ct);
        }

        if (ct.IsCancellationRequested) return;

        AnimationService.FadeIn(BootReady, 200);
        var pulseAnim = new DoubleAnimation(0.7, 1, TimeSpan.FromMilliseconds(500))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };
        BootReady.BeginAnimation(UIElement.OpacityProperty, pulseAnim);
        await DelaySafe(350, ct);
        if (ct.IsCancellationRequested) return;
        BootReady.BeginAnimation(UIElement.OpacityProperty, null);

        _telemetry?.Sample();
        BootCpu.Text = $"CPU: {_telemetry?.CpuPercent ?? 0:F1}%";
        BootRam.Text = $"RAM: {_telemetry?.RamPercent ?? 0:F1}%";
        UpdateTelemetryUI();

        AnimationService.FadeOut(BootOverlay, 150);
        await DelaySafe(160, ct);
        if (ct.IsCancellationRequested) return;

        BootOverlay.Visibility = Visibility.Collapsed;
        MainHUD.Opacity = 1;
        AnimationService.FadeIn(MainHUD, 120);
        await DelaySafe(40, ct);
        if (ct.IsCancellationRequested) return;

        foreach (var wrapper in _cardWrappers)
        {
            if (ct.IsCancellationRequested) return;
            AnimationService.FadeIn(wrapper, 120);
            await DelaySafe(25, ct);
        }

        AnimationService.FadeIn(FooterHint, 120);
        AnimationService.FadeIn(FooterIdle, 120);

        _state.TransitionTo(AppPhase.Ready);
        StartIdleTimer();

        _ = Dispatcher.BeginInvoke(() =>
        {
            if (_cardButtons.Count > 0)
                _cardButtons[0].Focus();
        }, DispatcherPriority.ContextIdle);
    }

    private async Task PlayShellExpansion(CancellationToken ct)
    {
        // Responsive expansion – animate to actual control size without enforcing large minimums.
        UpdateLayout();
        var targetWidth = ShellHost.ActualWidth;
        var targetHeight = ShellHost.ActualHeight;

        // Start from a tiny square for visual effect.
        MainBorder.Width = 6;
        MainBorder.Height = 6;

        // Expand width first, keep height minimal.
        AnimationService.ResizeTo(MainBorder, targetWidth, 6, 280, EasingMode.EaseOut);
        await DelaySafe(300, ct);
        if (ct.IsCancellationRequested) return;

        // Then expand height to final size.
        AnimationService.ResizeTo(MainBorder, targetWidth, targetHeight, 280, EasingMode.EaseOut);
        await DelaySafe(320, ct);
    }

    private async void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: ModeConfig mode }) return;
            var index = _modes.IndexOf(mode);
            if (index >= 0) await SelectMode(index);
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in ModeButton_Click: {0}", ex);
        }
    }

    private async Task SelectMode(int index)
    {
        var mode = _modes.Get(index);
        if (mode == null || _isLaunching) return;
        if (!_state.TransitionTo(AppPhase.Selecting)) return;

        _isLaunching = true;
        _modes.SelectedIndex = index;
        _log.Write("SelectMode({0}): name={1}, targets={2}", index, mode.Name, mode.ProcessCount);

        try
        {
            if (!_reducedEffects)
            {
                AnimationService.FadeOut(ModePanel, 100);
                await DelaySafe(60);
            }
            else
            {
                // Immediate hide without animation
                ModePanel.Visibility = Visibility.Collapsed;
            }

            ModePanel.IsHitTestVisible = false;
            ModePanel.Visibility = Visibility.Collapsed;

            LaunchModeLabel.Text = mode.Name.ToUpperInvariant();
            LaunchSessionLabel.Text = $"MODE {mode.IndexLabel}";
            LaunchStatus.Text = "Launching";
            LaunchStatus.Foreground = _theme.TextPrimary;
            if (!_reducedEffects)
            {
                LaunchPanel.Opacity = 0;
                LaunchPanel.Visibility = Visibility.Visible;
                AnimationService.FadeIn(LaunchPanel, 100);
            }
            else
            {
                LaunchPanel.Opacity = 1;
                LaunchPanel.Visibility = Visibility.Visible;
            }
            SetLaunchProgress(0, Math.Max(mode.ProcessCount, 1));

            _state.TransitionTo(AppPhase.Launching);

            _launchCts?.Dispose();
            _launchCts = new CancellationTokenSource();
            var token = _launchCts.Token;

            _launchDotTimer?.Stop();
            _launchDotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _launchDotTimer.Tick += (_, _) =>
            {
                _launchDotIndex = (_launchDotIndex + 1) % 4;
                LaunchStatus.Text = "Launching" + new string('.', _launchDotIndex);
            };
            _launchDotTimer.Start();
            LaunchTargetCount.Text = $"{mode.Targets.Count} TARGETS";

            // Launch orchestration – validation, start, progress
            var orchestrator = new LaunchOrchestrator(_log);
            // Clear previous launch feed for this run
            _launchFeedBuilder.Clear();
            LaunchFeedText.Text = string.Empty;
            var progress = new Progress<LaunchProgress>(p =>
            {
                // UI feed update per target – use StringBuilder to avoid repeated string allocations
                if (!string.IsNullOrEmpty(p.CurrentLabel))
                {
                    _launchFeedBuilder.Append("> ").Append(p.CurrentLabel).Append('\n');
                    LaunchFeedText.Text = _launchFeedBuilder.ToString();
                    LaunchFeedScroll.ScrollToBottom();
                }
                SetLaunchProgress(p.CompletedTargets, p.TotalTargets);
            });

            var launchResult = await orchestrator.LaunchModeAsync(mode, token, progress);

            _launchDotTimer?.Stop();

            if (launchResult.Canceled)
            {
                LaunchStatus.Text = "Cancelled";
                await ReturnToReady();
                return;
            }

            bool anySuccess = launchResult.LaunchedTargets > 0;
            bool success = launchResult.Failures.Count == 0 && anySuccess;
            LaunchStatus.Text = success ? "Ready" : anySuccess ? "Partial" : "Fault";
            LaunchStatus.Foreground = success ? _theme.TextPrimary : _theme.AccentLight;
            LaunchRecent.Text = launchResult.Failures.Count == 0
                ? $"Recent: {mode.LastLaunchLabel}"
                : $"Issue: {string.Join(", ", launchResult.Failures)}";

            mode.RecordLaunch(launchResult.LaunchedTargets, launchResult.TotalTargets, success);
            UpdateOperationalChrome();

            await DelaySafe(success ? 2000 : 5000, token);

            if (_closeOnLaunch)
                Close();
            else
                await ReturnToReady();
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

        private async Task ReturnToReady()
        {
            if (!_state.TransitionTo(AppPhase.Ready)) return;
            _isLaunching = false;
            _launchDotTimer?.Stop();

            if (!_reducedEffects)
            {
                AnimationService.FadeOut(LaunchPanel, 100);
                await DelaySafe(60);
            }
            else
            {
                LaunchPanel.Visibility = Visibility.Collapsed;
            }

            LaunchPanel.Visibility = Visibility.Collapsed;
            ModePanel.Visibility = Visibility.Visible;
            if (!_reducedEffects)
            {
                ModePanel.Opacity = 0;
                AnimationService.FadeIn(ModePanel, 100);
                await DelaySafe(40);
            }
            else
            {
                ModePanel.Opacity = 1;
            }
        }

    private void InitCursorTrail()
    {
        PreviewMouseMove += (_, e) =>
        {
            var position = e.GetPosition(TrailCanvas);
            if (position == _lastMousePos) return;
            _lastMousePos = position;
            _mouseMoved = true;
            ResetIdle();
        };
    }

    private void UpdateCursorTrail()
    {
        for (int i = _cursorTrail.Count - 1; i >= 0; i--)
        {
            var p = _cursorTrail[i];
            p.Life++;
            p.X += p.Vx;
            p.Y += p.Vy;

            if (p.Life >= p.MaxLife)
            {
                TrailCanvas.Children.Remove(p.Element);
                _cursorTrail.RemoveAt(i);
                continue;
            }

            var ratio = p.Life / p.MaxLife;
            p.Element.Opacity = 0.6 * (1 - ratio);
            var size = 2.5 * (1 - ratio * 0.6);
            p.Element.Width = size;
            p.Element.Height = size;
            Canvas.SetLeft(p.Element, p.X);
            Canvas.SetTop(p.Element, p.Y);
        }

        if (!_mouseMoved) return;
        _mouseMoved = false;
        _trailSkip++;
        if (_trailSkip % 3 != 0) return;

        var newP = new TrailParticle
        {
            Element = new Ellipse
            {
                Width = 2.5, Height = 2.5, Fill = _theme.AccentBright,
                IsHitTestVisible = false, Opacity = 0.6
            },
            Life = 0, MaxLife = 20,
            X = _lastMousePos.X, Y = _lastMousePos.Y,
            Vx = (Random.Shared.NextDouble() - 0.5) * 0.4,
            Vy = (Random.Shared.NextDouble() - 0.5) * 0.4
        };

        Canvas.SetLeft(newP.Element, newP.X);
        Canvas.SetTop(newP.Element, newP.Y);
        TrailCanvas.Children.Add(newP.Element);
        _cursorTrail.Add(newP);

        while (_cursorTrail.Count > 25)
        {
            var old = _cursorTrail[0];
            TrailCanvas.Children.Remove(old.Element);
            _cursorTrail.RemoveAt(0);
        }
    }

    private void UpdateTelemetryUI()
    {
        if (_telemetry == null) return;
        var cpu = _telemetry.CpuPercent;
        var ram = _telemetry.RamPercent;

        HeaderCpu.Text = $"CPU {cpu,4:F1}%";
        HeaderRam.Text = $"RAM {ram,4:F1}%";
        HeaderSys.Text = cpu < 75 && ram < 85 ? "ONLINE" : "LOAD";
        HeaderSys.Foreground = cpu < 75 && ram < 85 ? _theme.AccentBright : _theme.AccentLight;

        SystemStrip.Text = cpu < 75 && ram < 85
            ? $"SYS {HeaderSys.Text}  -  HUD STABLE"
            : $"SYS {HeaderSys.Text}  -  CPU {cpu:F0}% / RAM {ram:F0}%";

        if (_state.Phase == AppPhase.Launching)
        {
            LaunchCpu.Text = $"CPU {cpu:F1}%";
            LaunchRam.Text = $"RAM {ram:F1}%";
        }

        var idleSec = _idleElapsed > 0 ? (int)(_idleElapsed / 1000) : 0;
        FooterIdle.Text = idleSec > 0 ? $"idle {idleSec}s" : string.Empty;
    }

    private void StartIdleTimer()
    {
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += async (_, _) =>
        {
            if (_state.Phase != AppPhase.Ready) return;
            _idleElapsed += 1000;
            if (_idleElapsed >= _idleTimeout * 1000)
                await CloseWithAnimation();
        };
        _idleTimer.Start();
    }

    private void ResetIdle()
    {
        if (_state.Phase != AppPhase.Ready) return;
        _idleElapsed = 0;
        FooterIdle.Text = string.Empty;
        _idleTimer?.Stop();
        _idleTimer?.Start();
    }

    private async Task CloseWithAnimation()
    {
        if (_isClosing || _disposed) return;
        _isClosing = true;
        _state.TransitionTo(AppPhase.Closing);
        _lifeCts?.Cancel();
        _idleTimer?.Stop();

        AnimationService.FadeOut(BootOverlay, 80);
        AnimationService.FadeOut(MainHUD, 80);
        await DelaySafe(60);

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        BeginAnimation(OpacityProperty, fade);
        await DelaySafe(200);
        Close();
    }

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            ResetIdle();

            if (e.Key == Key.Escape)
            {
                if (_state.Phase == AppPhase.Launching)
                {
                    LaunchStatus.Text = "Cancelling...";
                    _launchCts?.Cancel();
                }
                else if (_state.Phase != AppPhase.Closing)
                    await CloseWithAnimation();
                e.Handled = true;
                return;
            }

            int? modeIndex = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 0, Key.D2 or Key.NumPad2 => 1,
                Key.D3 or Key.NumPad3 => 2, Key.D4 or Key.NumPad4 => 3,
                Key.D5 or Key.NumPad5 => 4, Key.D6 or Key.NumPad6 => 5,
                Key.D7 or Key.NumPad7 => 6, Key.D8 or Key.NumPad8 => 7,
                Key.D9 or Key.NumPad9 => 8, _ => null
            };

            if (modeIndex.HasValue) { await SelectMode(modeIndex.Value); e.Handled = true; return; }

            if (_state.Phase == AppPhase.Ready && ModePanel.Visibility == Visibility.Visible)
            {
                var focused = FocusManager.GetFocusedElement(this);
                var currentIndex = -1;
                if (focused is Button { Tag: ModeConfig mode }) currentIndex = _modes.IndexOf(mode);

                if (e.Key is Key.Down or Key.Right)
                { FocusModeButton((currentIndex + 1 + _modes.Count) % _modes.Count); e.Handled = true; return; }
                if (e.Key is Key.Up or Key.Left)
                { FocusModeButton(currentIndex <= 0 ? _modes.Count - 1 : currentIndex - 1); e.Handled = true; return; }
            }

            if (e.Key == Key.Enter && _state.Phase == AppPhase.Ready)
            {
                if (FocusManager.GetFocusedElement(this) is Button fb) ModeButton_Click(fb, new RoutedEventArgs());
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in Window_PreviewKeyDown: {0}", ex);
        }
    }

    private void FocusModeButton(int index)
    {
        if (index >= 0 && index < _cardButtons.Count)
            _cardButtons[index].Focus();
    }

    private async void MenuEdit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;

            var dialog = new EditModeWindow(mode);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                await SaveCurrentConfig();
                BuildDashboard();
                UpdateOperationalChrome();
            }
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in MenuEdit_Click: {0}", ex);
        }
    }

    private async void MenuDuplicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;

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
            BuildDashboard();
            UpdateOperationalChrome();
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in MenuDuplicate_Click: {0}", ex);
        }
    }

    private async void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null || _modes.Count <= 1) return;

            var idx = _modes.IndexOf(mode);
            _modes.RemoveAt(idx);
            await SaveCurrentConfig();
            BuildDashboard();
            UpdateOperationalChrome();
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in MenuDelete_Click: {0}", ex);
        }
    }

    private async void MenuMoveUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;

            var idx = _modes.IndexOf(mode);
            if (_modes.MoveUp(idx))
            {
                await SaveCurrentConfig();
                BuildDashboard();
                UpdateOperationalChrome();
            }
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in MenuMoveUp_Click: {0}", ex);
        }
    }

    private async void MenuMoveDown_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;

            var idx = _modes.IndexOf(mode);
            if (_modes.MoveDown(idx))
            {
                await SaveCurrentConfig();
                BuildDashboard();
                UpdateOperationalChrome();
            }
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in MenuMoveDown_Click: {0}", ex);
        }
    }

    private async Task SaveCurrentConfig()
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

    private static ModeConfig? GetModeFromMenu(object sender)
    {
        if (sender is System.Windows.Controls.MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is Button { Tag: ModeConfig mode })
            return mode;
        return null;
    }

    private async void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (_state.Phase == AppPhase.Closing) return;
            var pos = e.GetPosition(MainBorder);
            if (pos.X < 0 || pos.Y < 0 || pos.X > MainBorder.ActualWidth || pos.Y > MainBorder.ActualHeight)
                await CloseWithAnimation();
        }
        catch (Exception ex)
        {
            _log.Write("UNHANDLED EXCEPTION in RootGrid_MouseDown: {0}", ex);
        }
    }

    private void BuildDashboard()
    {
        DashboardGrid.Children.Clear();
        DashboardGrid.ColumnDefinitions.Clear();
        DashboardGrid.RowDefinitions.Clear();
        _cardWrappers.Clear();
        _cardButtons.Clear();

        if (_modes.Count == 0) return;

        DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var template = (DataTemplate)FindResource("ModeCardTemplate");

        for (int i = 0; i < _modes.Count; i++)
        {
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var wrapper = new ContentControl
            {
                Content = _modes.Get(i),
                ContentTemplate = template,
                Margin = new Thickness(0, 0, 0, _compactMode ? 3 : 6),
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Height = _compactMode ? 60 : double.NaN
            };
            Grid.SetColumn(wrapper, 0);
            Grid.SetRow(wrapper, i);
            DashboardGrid.Children.Add(wrapper);
            _cardWrappers.Add(wrapper);
        }

        // DashboardGrid.UpdateLayout(); // Layout is handled by WPF after UI changes – avoid forced layout pass each rebuild.

        foreach (var wrapper in _cardWrappers)
        {
            if (FindVisualChild<Button>(wrapper) is { } btn)
                _cardButtons.Add(btn);
        }
    }

    private void UpdateOperationalChrome()
    {
        var activeLabel = _modes.ActiveLabel;
        ModeCountRun.Text = _modes.Count.ToString("D2");
        ProcessStrip.Text = $"PROC {_modes.TotalProcesses:D2}  -  ACTIVE {activeLabel}";
        FooterHint.Text = $"{_modes.ShortcutHint} select  -  ENTER launch  -  ESC close";
        var sel = _modes.SelectedMode;
        LaunchRecent.Text = sel != null ? $"Recent: {sel.LastLaunchLabel}" : "Recent: none";
    }

    private void SetLaunchProgress(int completed, int total)
    {
        var safeTotal = Math.Max(total, 1);
        var ratio = Math.Clamp((double)completed / safeTotal, 0, 1);
        LaunchProgressScale.ScaleX = ratio;
        LaunchProgressText.Text = $"PROC {Math.Min(completed, safeTotal):D2}/{safeTotal:D2}";
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T tc) return tc;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private static async Task TypeText(TextBlock target, string text, int intervalMs, CancellationToken ct = default)
    {
        foreach (char c in text)
        {
            if (ct.IsCancellationRequested) return;
            target.Text += c;
            await DelaySafe(intervalMs, ct);
        }
    }

    private static async Task DelaySafe(int ms, CancellationToken ct = default)
    {
        try { await Task.Delay(ms, ct); }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _log.Write("ARCYN disposed");
        _state.PhaseChanged -= OnPhaseChanged;
        _lifeCts?.Cancel(); _lifeCts?.Dispose();
        _launchCts?.Cancel(); _launchCts?.Dispose();
        _idleTimer?.Stop(); _launchDotTimer?.Stop();
        _render.Dispose(); _telemetry?.Dispose(); _particles?.Dispose(); _log.Dispose();
    }

    private sealed class TrailParticle
    {
        public required Ellipse Element { get; init; }
        public double Life { get; set; }
        public double MaxLife { get; init; }
        public double X { get; set; } public double Y { get; set; }
        public double Vx { get; init; } public double Vy { get; init; }
    }
}
