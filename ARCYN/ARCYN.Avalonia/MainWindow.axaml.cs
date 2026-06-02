using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ARCYN.Core.Models;
using ARCYN.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KeyBinding = Avalonia.Input.KeyBinding;
using KeyGesture = Avalonia.Input.KeyGesture;
using KeyModifiers = Avalonia.Input.KeyModifiers;
using Key = Avalonia.Input.Key;

namespace ARCYN.Avalonia;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService = new();
    private readonly ModeService _modeService = new();
    private ArcynConfig? _config;
    private readonly DispatcherTimer _idleTimer;
    private TimeSpan _idleRemaining;

    public MainWindow()
    {
        InitializeComponent();
        ConfigPathText.Text = ConfigService.DefaultConfigPath;

        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleTimer.Tick += OnIdleTick;
        PointerPressed += (_, _) => ResetIdle();
        KeyDown += (_, _) => ResetIdle();

        LoadConfig();
    }

    private void LoadConfig()
    {
        var config = _configService.Load();
        if (config == null)
        {
            _config = null;
            _modeService.Load([]);
            ModesPanel.Children.Clear();
            ApplyShortcuts();
            StatusText.Text = "No modes are configured yet. Create ~/.config/ARCYN/arcyn.json or copy ARCYN/example.arcyn.json there, then reload.";
            return;
        }

        _config = config;
        _modeService.Load(config.Modes);
        RenderModes();
        ApplyShortcuts();
    }

    private void RenderModes()
    {
        ModesPanel.Children.Clear();
        var modes = _modeService.Modes;
        if (modes.Count == 0)
        {
            StatusText.Text = "No modes are configured yet. Create ~/.config/ARCYN/arcyn.json or copy ARCYN/example.arcyn.json there, then reload.";
            return;
        }

        StatusText.Text = $"Loaded {modes.Count} mode(s). Press a shortcut or click Launch to start one.";
        foreach (var mode in modes)
            ModesPanel.Children.Add(CreateModeRow(mode));
    }

    private void ApplyShortcuts()
    {
        KeyBindings.Clear();
        var warnings = new List<string>();

        foreach (var mode in _modeService.Modes)
        {
            if (!string.IsNullOrWhiteSpace(mode.Shortcut))
            {
                if (KeyCombo.TryParse(mode.Shortcut, out var combo))
                {
                    if (TryBuildKeyGesture(combo, out var gesture))
                    {
                        var captured = mode;
                        KeyBindings.Add(new KeyBinding
                        {
                            Gesture = gesture,
                            Command = new RelayCommand(() => _ = LaunchModeAsync(captured))
                        });
                    }
                    else
                    {
                        warnings.Add($"Unsupported key for {mode.Name}: '{mode.Shortcut}'");
                    }
                }
                else
                {
                    warnings.Add($"Ignored invalid shortcut for {mode.Name}: '{mode.Shortcut}'");
                }
                continue;
            }

            // Implicit fallback: bare digit 1..9 maps to that mode's index.
            if (mode.Index >= 1 && mode.Index <= 9)
            {
                var captured = mode;
                var digit = (LogicalKey)((int)LogicalKey.D0 + mode.Index);
                KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture(MapKey(digit), KeyModifiers.None),
                    Command = new RelayCommand(() => _ = LaunchModeAsync(captured))
                });
            }
        }

        // Esc always closes the app.
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(MapKey(LogicalKey.Escape), KeyModifiers.None),
            Command = new RelayCommand(Close)
        });

        if (warnings.Count > 0)
        {
            var baseText = StatusText.Text ?? string.Empty;
            StatusText.Text = warnings.Count == 1
                ? $"{baseText}\n{warnings[0]}"
                : $"{baseText}\n• {string.Join("\n• ", warnings)}";
        }
    }

    private static bool TryBuildKeyGesture(KeyCombo combo, out KeyGesture gesture)
    {
        gesture = null!;
        var avKey = MapKey(combo.Key);
        if (avKey == Key.None)
            return false;
        gesture = new KeyGesture(avKey, MapModifiers(combo.Modifiers));
        return true;
    }

    private static Key MapKey(LogicalKey key)
    {
        if (key >= LogicalKey.D0 && key <= LogicalKey.D9)
            return (Key)((int)Key.D0 + (int)(key - LogicalKey.D0));
        if (key >= LogicalKey.A && key <= LogicalKey.Z)
            return (Key)((int)Key.A + (int)(key - LogicalKey.A));
        if (key >= LogicalKey.F1 && key <= LogicalKey.F24)
            return (Key)((int)Key.F1 + (int)(key - LogicalKey.F1));
        return key switch
        {
            LogicalKey.Escape => Key.Escape,
            LogicalKey.Tab => Key.Tab,
            LogicalKey.Space => Key.Space,
            LogicalKey.Enter => Key.Enter,
            LogicalKey.Insert => Key.Insert,
            LogicalKey.Delete => Key.Delete,
            LogicalKey.Home => Key.Home,
            LogicalKey.End => Key.End,
            LogicalKey.PageUp => Key.PageUp,
            LogicalKey.PageDown => Key.PageDown,
            LogicalKey.Up => Key.Up,
            LogicalKey.Down => Key.Down,
            LogicalKey.Left => Key.Left,
            LogicalKey.Right => Key.Right,
            _ => Key.None
        };
    }

    private static KeyModifiers MapModifiers(ARCYN.Core.Models.KeyModifiers mods)
    {
        var result = KeyModifiers.None;
        if ((mods & ARCYN.Core.Models.KeyModifiers.Ctrl) != 0) result |= KeyModifiers.Control;
        if ((mods & ARCYN.Core.Models.KeyModifiers.Alt) != 0) result |= KeyModifiers.Alt;
        if ((mods & ARCYN.Core.Models.KeyModifiers.Shift) != 0) result |= KeyModifiers.Shift;
        if ((mods & ARCYN.Core.Models.KeyModifiers.Meta) != 0) result |= KeyModifiers.Meta;
        // Avalonia exposes Super as the Meta flag on Linux too, so map it there.
        if ((mods & ARCYN.Core.Models.KeyModifiers.Super) != 0) result |= KeyModifiers.Meta;
        return result;
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
        var parts = new List<string> { $"Shortcut: {mode.ShortcutHint}" };
        if (mode.Apps.Count > 0) parts.Add($"{mode.Apps.Count} app(s)");
        if (mode.Websites.Count > 0) parts.Add($"{mode.Websites.Count} website(s)");
        if (mode.Folders.Count > 0) parts.Add($"{mode.Folders.Count} folder(s)");
        return string.Join(" | ", parts);
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

        var success = failures.Count == 0;
        mode.RecordLaunch(launched, mode.Targets.Count, success);

        var summary = success
            ? $"Launched {launched} target(s) for {mode.Name}."
            : $"Launched {launched} target(s). Failed: {string.Join("; ", failures)}";

        if (_config?.Behavior.CloseOnLaunch == true && success)
        {
            Close();
            return;
        }

        StatusText.Text = summary;
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

        try
        {
            Directory.CreateDirectory(configDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = configDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not open config folder: {ex.Message}. Config path: {configDir}";
        }
    }

    private void ResetIdle()
    {
        if (_config == null || _config.Behavior.IdleTimeoutSeconds <= 0)
        {
            _idleTimer.Stop();
            return;
        }
        _idleRemaining = TimeSpan.FromSeconds(_config.Behavior.IdleTimeoutSeconds);
        if (!_idleTimer.IsEnabled)
            _idleTimer.Start();
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        _idleRemaining = _idleRemaining - TimeSpan.FromSeconds(1);
        if (_idleRemaining <= TimeSpan.Zero)
        {
            _idleTimer.Stop();
            Close();
        }
    }

    private sealed class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
