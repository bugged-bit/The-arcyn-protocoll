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
using ARCYN.Core.Models;
using ARCYN.UI.ViewModels;
using ARCYN.UI.Services;

namespace ARCYN.UI;

public partial class MainWindow : Window, IDisposable, RenderService.ISubscriber
{
    private readonly MainWindowViewModel _vm;

    // Render / particle / chrome fields
    private readonly RenderService _render;
    private readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly List<ContentControl> _cardWrappers = [];
    private readonly List<Button> _cardButtons = [];
    private readonly List<TrailParticle> _cursorTrail = [];

    private CancellationTokenSource? _lifeCts;
    private ParticleEngine? _particles;
    private TelemetryMonitor? _telemetry;

    private bool _disposed;
    private bool _isClosing;
    private bool _mouseMoved;

    private long _accelAmbient;
    private long _accelParticle;
    private long _accelTelemetry;
    private long _accelTime;
    private long _accelTrail;

    private double _ambientPhase;
    private double _spinnerAngle;
    private int _trailSkip;
    private long _idleElapsed;

    private Point _lastMousePos;
    private DispatcherTimer? _idleTimer;
    private DispatcherTimer? _launchDotTimer;
    private int _launchDotIndex;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainWindowViewModel();
        DataContext = _vm;

        _render = new RenderService();
        _lifeCts = new CancellationTokenSource();

        // Subscribe to ViewModel events for UI operations
        _vm.LaunchStarting += OnLaunchStarting;
        _vm.LaunchCompleted += OnLaunchCompleted;
        _vm.ConfigChanged += OnConfigChanged;
        _vm.CloseRequested += OnCloseRequestedAsync;
        _vm.State.PhaseChanged += OnVmPhaseChanged;

        Loaded += OnLoaded;
        Deactivated += OnDeactivated;
        Closed += (_, _) => Dispose();
    }

    // ── ViewModel event handlers (UI operations) ──────────────────

    private async Task OnLaunchStarting(int index, ModeConfig mode)
    {
        // Animate: hide mode panel, show launch panel
        if (!_vm.ReducedEffects)
        {
            AnimationService.FadeOut(ModePanel, 100);
            await DelaySafe(60);
        }
        else
        {
            ModePanel.Visibility = Visibility.Collapsed;
        }

        ModePanel.IsHitTestVisible = false;
        ModePanel.Visibility = Visibility.Collapsed;

        if (!_vm.ReducedEffects)
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

        // Start dot animation
        _launchDotTimer?.Stop();
        _launchDotIndex = 0;
        _launchDotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _launchDotTimer.Tick += (_, _) =>
        {
            _launchDotIndex = (_launchDotIndex + 1) % 4;
            _vm.LaunchStatus = "Launching" + new string('.', _launchDotIndex);
        };
        _launchDotTimer.Start();
    }

    private async Task OnLaunchCompleted(bool success)
    {
        _launchDotTimer?.Stop();

        if (_vm.CloseOnLaunch)
        {
            // Delay then close
            await DelaySafe(success ? 2000 : 5000);
            await CloseWithAnimation();
            return;
        }

        // Return to ready
        if (!_vm.ReducedEffects)
        {
            AnimationService.FadeOut(LaunchPanel, 100);
            await DelaySafe(60);
        }

        LaunchPanel.Visibility = Visibility.Collapsed;
        ModePanel.Visibility = Visibility.Visible;

        if (!_vm.ReducedEffects)
        {
            ModePanel.Opacity = 0;
            AnimationService.FadeIn(ModePanel, 100);
            await DelaySafe(40);
        }
        else
        {
            ModePanel.Opacity = 1;
        }

        if (_cardButtons.Count > 0)
            _cardButtons[0].Focus();

        _ = _vm.ReturnToReady();
    }

    private void OnConfigChanged()
    {
        BuildDashboard();
        UpdateOperationalChrome();
    }

    private async Task OnCloseRequestedAsync()
    {
        await CloseWithAnimation();
    }

    private void OnVmPhaseChanged(AppPhase previous, AppPhase next)
    {
        _vm.Log.Write("Window UI: Phase {0} -> {1}", previous, next);

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
            _lifeCts?.Cancel();
        }
    }

    // ── Window loaded ─────────────────────────────────────────────

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);

            try { NativeMethods.EnableAcrylic(hwnd, 0xE20A0A0A); }
            catch { _vm.Log.Write("Acrylic enable skipped"); }

            _vm.Log.Write("OnLoaded: start");
            Activate();
            Focus();
            Keyboard.Focus(this);

            _vm.Theme.LoadResources(this);

            // Delegate config loading to ViewModel
            var arcynConfig = await _vm.InitializeAsync();

            if (arcynConfig == null)
            {
                _vm.Log.Write("No config found — first-run detected");
                _vm.TransitionTo(AppPhase.Closing);
                Close();
                return;
            }

            Topmost = _vm.AlwaysOnTop;

            // Build the visual dashboard from ViewModel modes
            BuildDashboard();

            _telemetry = new TelemetryMonitor();
            _particles = new ParticleEngine(ParticleCanvas);

            _render.Subscribe(this);
            _render.Start();
            if (!_vm.ReducedEffects)
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

            _vm.Log.Write("OnLoaded: runtime initialized");

            await PlayStartupSequence(_lifeCts!.Token);
            _vm.Log.Write("OnLoaded: boot sequence done");
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in OnLoaded: {0}", ex);
            _vm.TransitionTo(AppPhase.Closing);
            Close();
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _vm.Log.Write("Window deactivated while Phase={0}", _vm.State.Phase);
    }

    // ── Render tick ───────────────────────────────────────────────

    void RenderService.ISubscriber.OnRenderTick(long dt)
    {
        if (_disposed || _vm.State.Phase == AppPhase.Closing)
            return;

        _accelParticle += dt;
        if (_accelParticle >= 30)
        {
            _accelParticle -= 30;
            _particles?.Tick();
        }

        if (!_vm.ReducedEffects)
        {
            _accelAmbient += dt;
            if (_accelAmbient >= 30)
            {
                _accelAmbient = 0;
                _ambientPhase += 0.025;
                AmbientGlow.Opacity = Math.Clamp(0.26 + Math.Sin(_ambientPhase) * 0.12, 0.08, 0.45);
            }

            _accelTrail += dt;
            if (_accelTrail >= 16 && _vm.State.Phase == AppPhase.Ready)
            {
                _accelTrail = 0;
                UpdateCursorTrail();
            }
        }

        _accelTelemetry += dt;
        if (_accelTelemetry >= 1000 && _vm.State.Phase != AppPhase.Boot)
        {
            _accelTelemetry -= 1000;
            _telemetry?.Sample();
            _vm.UpdateTelemetry(_telemetry);
            UpdateTelemetryUI();
        }

        _accelTime += dt;
        if (_accelTime >= 1000)
        {
            _accelTime -= 1000;
            _vm.UpdateTimeDisplay();
        }

        if (_vm.State.Phase == AppPhase.Launching)
        {
            _spinnerAngle = (_spinnerAngle + dt * 0.15) % 360;
            SpinnerRotation.Angle = _spinnerAngle;
        }
    }

    private void UpdateTelemetryUI()
    {
        if (_telemetry == null) return;
        var cpu = _telemetry.CpuPercent;
        var ram = _telemetry.RamPercent;

        // HeaderSys Foreground depends on system health - UI-specific, set directly
        HeaderSys.Foreground = cpu < 75 && ram < 85
            ? _vm.Theme.AccentBright
            : _vm.Theme.AccentLight;

        // Idle counter is tracked in code-behind
        _vm.UpdateIdleDisplay((int)_idleElapsed);
    }

    // ── Boot animation sequence ───────────────────────────────────

    private async Task PlayStartupSequence(CancellationToken ct)
    {
        _vm.TransitionTo(AppPhase.Boot);

        if (_vm.ReducedEffects)
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
            _vm.TransitionTo(AppPhase.Ready);
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

        _vm.TransitionTo(AppPhase.Ready);
        StartIdleTimer();

        _ = Dispatcher.BeginInvoke(() =>
        {
            if (_cardButtons.Count > 0)
                _cardButtons[0].Focus();
        }, DispatcherPriority.ContextIdle);
    }

    private async Task PlayShellExpansion(CancellationToken ct)
    {
        UpdateLayout();
        var targetWidth = ShellHost.ActualWidth;
        var targetHeight = ShellHost.ActualHeight;

        MainBorder.Width = 6;
        MainBorder.Height = 6;

        AnimationService.ResizeTo(MainBorder, targetWidth, 6, 280, EasingMode.EaseOut);
        await DelaySafe(300, ct);
        if (ct.IsCancellationRequested) return;

        AnimationService.ResizeTo(MainBorder, targetWidth, targetHeight, 280, EasingMode.EaseOut);
        await DelaySafe(320, ct);
    }

    // ── Mode selection button ─────────────────────────────────────

    private async void ModeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { Tag: ModeConfig mode }) return;
            var index = _vm.Modes.IndexOf(mode);
            if (index >= 0) await _vm.SelectMode(index);
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in ModeButton_Click: {0}", ex);
        }
    }

    // ── Keyboard handling ─────────────────────────────────────────

    private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        try
        {
            ResetIdle();

            if (e.Key == Key.Escape)
            {
                if (_vm.State.Phase == AppPhase.Launching)
                {
                    _vm.CancelLaunch();
                }
                else if (_vm.State.Phase != AppPhase.Closing)
                {
                    await CloseWithAnimation();
                }
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

            if (modeIndex.HasValue) { await _vm.SelectMode(modeIndex.Value); e.Handled = true; return; }

            if (_vm.State.Phase == AppPhase.Ready && ModePanel.Visibility == Visibility.Visible)
            {
                var focused = FocusManager.GetFocusedElement(this);
                var currentIndex = -1;
                if (focused is Button { Tag: ModeConfig mode }) currentIndex = _vm.Modes.IndexOf(mode);

                if (e.Key is Key.Down or Key.Right)
                { FocusModeButton((currentIndex + 1 + _vm.Modes.Count) % _vm.Modes.Count); e.Handled = true; return; }
                if (e.Key is Key.Up or Key.Left)
                { FocusModeButton(currentIndex <= 0 ? _vm.Modes.Count - 1 : currentIndex - 1); e.Handled = true; return; }
            }

            if (e.Key == Key.Enter && _vm.State.Phase == AppPhase.Ready)
            {
                if (FocusManager.GetFocusedElement(this) is Button fb) ModeButton_Click(fb, new RoutedEventArgs());
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in Window_PreviewKeyDown: {0}", ex);
        }
    }

    private void FocusModeButton(int index)
    {
        if (index >= 0 && index < _cardButtons.Count)
            _cardButtons[index].Focus();
    }

    // ── Context menu handlers ─────────────────────────────────────

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
                await _vm.SaveCurrentConfig();
                BuildDashboard();
                UpdateOperationalChrome();
            }
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in MenuEdit_Click: {0}", ex);
        }
    }

    private async void MenuDuplicate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;
            await _vm.DuplicateMode(mode);
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in MenuDuplicate_Click: {0}", ex);
        }
    }

    private async void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;
            await _vm.DeleteMode(mode);
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in MenuDelete_Click: {0}", ex);
        }
    }

    private async void MenuMoveUp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;
            await _vm.MoveModeUp(mode);
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in MenuMoveUp_Click: {0}", ex);
        }
    }

    private async void MenuMoveDown_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = GetModeFromMenu(sender);
            if (mode == null) return;
            await _vm.MoveModeDown(mode);
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in MenuMoveDown_Click: {0}", ex);
        }
    }

    private static ModeConfig? GetModeFromMenu(object sender)
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is Button { Tag: ModeConfig mode })
            return mode;
        return null;
    }

    // ── Mouse / click-outside handling ────────────────────────────

    private async void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (_vm.State.Phase == AppPhase.Closing) return;
            var pos = e.GetPosition(MainBorder);
            if (pos.X < 0 || pos.Y < 0 || pos.X > MainBorder.ActualWidth || pos.Y > MainBorder.ActualHeight)
                await CloseWithAnimation();
        }
        catch (Exception ex)
        {
            _vm.Log.Write("UNHANDLED EXCEPTION in RootGrid_MouseDown: {0}", ex);
        }
    }

    // ── Dashboard building ────────────────────────────────────────

    private void BuildDashboard()
    {
        DashboardGrid.Children.Clear();
        DashboardGrid.ColumnDefinitions.Clear();
        DashboardGrid.RowDefinitions.Clear();
        _cardWrappers.Clear();
        _cardButtons.Clear();

        if (_vm.Modes.Count == 0) return;

        DashboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var template = (DataTemplate)FindResource("ModeCardTemplate");

        for (int i = 0; i < _vm.Modes.Count; i++)
        {
            DashboardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var wrapper = new ContentControl
            {
                Content = _vm.Modes.Get(i),
                ContentTemplate = template,
                Margin = new Thickness(0, 0, 0, _vm.CompactMode ? 3 : 6),
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Height = _vm.CompactMode ? 60 : double.NaN
            };
            Grid.SetColumn(wrapper, 0);
            Grid.SetRow(wrapper, i);
            DashboardGrid.Children.Add(wrapper);
            _cardWrappers.Add(wrapper);
        }

        foreach (var wrapper in _cardWrappers)
        {
            if (FindVisualChild<Button>(wrapper) is { } btn)
                _cardButtons.Add(btn);
        }
    }

    // ── Chrome updates ────────────────────────────────────────────

    private void UpdateOperationalChrome()
    {
        // Delegate to ViewModel for property updates; XAML bindings handle the display.
        _vm.UpdateOperationalChrome();
    }

    // ── Idle timer ────────────────────────────────────────────────

    private void StartIdleTimer()
    {
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += async (_, _) =>
        {
            if (_vm.State.Phase != AppPhase.Ready) return;
            _idleElapsed += 1000;
            _vm.UpdateIdleDisplay((int)_idleElapsed);
            if (_idleElapsed >= _vm.IdleTimeout * 1000)
                await CloseWithAnimation();
        };
        _idleTimer.Start();
    }

    private void ResetIdle()
    {
        if (_vm.State.Phase != AppPhase.Ready) return;
        _idleElapsed = 0;
        _vm.FooterIdle = string.Empty;
        _idleTimer?.Stop();
        _idleTimer?.Start();
    }

    // ── Close animation ───────────────────────────────────────────

    private async Task CloseWithAnimation()
    {
        if (_isClosing || _disposed) return;
        _isClosing = true;
        _vm.TransitionTo(AppPhase.Closing);
        _lifeCts?.Cancel();
        _idleTimer?.Stop();
        _launchDotTimer?.Stop();

        AnimationService.FadeOut(BootOverlay, 80);
        AnimationService.FadeOut(MainHUD, 80);
        await DelaySafe(60);

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
        BeginAnimation(OpacityProperty, fade);
        await DelaySafe(200);
        Close();
    }

    // ── Cursor trail ──────────────────────────────────────────────

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
                Width = 2.5, Height = 2.5, Fill = _vm.Theme.AccentBright,
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

    // ── Helpers ───────────────────────────────────────────────────

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

    // ── Disposal ──────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _vm.Log.Write("ARCYN disposed");
        _lifeCts?.Cancel(); _lifeCts?.Dispose();
        _idleTimer?.Stop(); _launchDotTimer?.Stop();
        _render.Dispose(); _telemetry?.Dispose(); _particles?.Dispose();
        _vm.Dispose();
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
