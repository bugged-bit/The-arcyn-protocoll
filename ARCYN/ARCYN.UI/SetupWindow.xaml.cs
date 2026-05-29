using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using ARCYN.UI.Models;
using ARCYN.UI.Services;


namespace ARCYN.UI;

public partial class SetupWindow : Window
{
    private readonly ConfigService _config = new();
    private readonly List<ModeConfig> _modes = [];
    private ModeConfig? _currentMode;
    private int _step;
    private ParticleEngine? _particles;
    private DispatcherTimer? _particleTimer;
    private static readonly string[] StepTitles = ["Welcome", "Mode", "Apps", "Folders", "Websites", "Review"];

    private static readonly Dictionary<string, string> ColorMap = new()
    {
        ["ColorRed"] = "#D64545",
        ["ColorBlue"] = "#4580D6",
        ["ColorGreen"] = "#45D680",
        ["ColorOrange"] = "#D6A045",
        ["ColorPurple"] = "#A045D6"
    };

        public SetupWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            _currentMode = NewMode();
            ShowStep(0);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            try { NativeMethods.EnableAcrylic(hwnd, 0xE20A0A0A); } catch { }

            _particles = new ParticleEngine(ParticleCanvas);
            _particles.Start();
            _particleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _particleTimer.Tick += (_, _) => _particles?.Tick();
            _particleTimer.Start();
        }

        private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(MainBorder);
            if (pos.X < 0 || pos.Y < 0 || pos.X > MainBorder.ActualWidth || pos.Y > MainBorder.ActualHeight)
                Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _particleTimer?.Stop();
            _particles?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _particleTimer?.Stop();
            _particles?.Dispose();
            base.OnClosed(e);
        }

    private static ModeConfig NewMode()
    {
        return new ModeConfig
        {
            Name = "",
            Description = "",
            Accent = "#D64545"
        };
    }

    private void ShowStep(int step)
    {
        _step = step;
        StepWelcome.Visibility = Visibility.Collapsed;
        StepMode.Visibility = Visibility.Collapsed;
        StepApps.Visibility = Visibility.Collapsed;
        StepFolders.Visibility = Visibility.Collapsed;
        StepWebsites.Visibility = Visibility.Collapsed;
        StepReview.Visibility = Visibility.Collapsed;

        switch (step)
        {
            case 0:
                StepWelcome.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Start Setup";
                StatusBar.Text = "Begin configuration";
                break;
            case 1:
                StepMode.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Next";
                StatusBar.Text = "Name your workspace mode";
                break;
            case 2:
                StepApps.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Next";
                StatusBar.Text = "Add applications to launch";
                break;
            case 3:
                StepFolders.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Next";
                StatusBar.Text = "Add folders to open";
                break;
            case 4:
                StepWebsites.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Visible;
                FinishButton.Visibility = Visibility.Collapsed;
                NextButton.Content = "Next";
                StatusBar.Text = "Add websites to open";
                break;
            case 5:
                StepReview.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
                NextButton.Visibility = Visibility.Collapsed;
                FinishButton.Visibility = Visibility.Visible;
                StatusBar.Text = _modes.Count == 1 ? "1 mode configured" : $"{_modes.Count} modes configured";
                RefreshReview();
                break;
        }

        StepCounter.Text = $"{step + 1} / {StepTitles.Length}";
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        switch (_step)
        {
            case 0:
                // Welcome → Mode creation
                ShowStep(1);
                Keyboard.Focus(ModeNameInput);
                break;

            case 1:
                // Save mode name/desc/color → Apps
                if (!CommitCurrentMode()) return;
                ShowStep(2);
                break;

            case 2:
                // Save apps → Folders
                SaveCurrentApps();
                ShowStep(3);
                break;

            case 3:
                // Save folders → Websites
                SaveCurrentFolders();
                ShowStep(4);
                Keyboard.Focus(WebsiteInput);
                break;

            case 4:
                // Save websites → Review
                SaveCurrentWebsites();
                FinalizeCurrentMode();
                ShowStep(5);
                break;
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_step <= 1)
        {
            ShowStep(0);
            return;
        }

        // When going back from review, restore last mode for editing
        if (_step == 5 && _modes.Count > 0)
        {
            _currentMode = _modes[^1];
            _modes.RemoveAt(_modes.Count - 1);
            RestoreCurrentModeUI();
        }

        ShowStep(_step - 1);
    }

    private bool CommitCurrentMode()
    {
        var name = ModeNameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusBar.Text = "Name is required";
            return false;
        }

        _currentMode ??= NewMode();
        _currentMode.Name = name.ToUpperInvariant();
        _currentMode.Description = ModeDescInput.Text.Trim();
        _currentMode.Accent = GetSelectedAccent();

        return true;
    }

    private void SaveCurrentApps()
    {
        if (_currentMode == null) return;
        _currentMode.Apps.Clear();
        foreach (var item in AppsList.Items)
            _currentMode.Apps.Add(item.ToString() ?? string.Empty);
    }

    private void SaveCurrentFolders()
    {
        if (_currentMode == null) return;
        _currentMode.Folders.Clear();
        foreach (var item in FoldersList.Items)
            _currentMode.Folders.Add(item.ToString() ?? string.Empty);
    }

    private void SaveCurrentWebsites()
    {
        if (_currentMode == null) return;
        _currentMode.Websites.Clear();
        foreach (var item in WebsitesList.Items)
            _currentMode.Websites.Add(item.ToString() ?? string.Empty);
    }

    private void FinalizeCurrentMode()
    {
        if (_currentMode == null) return;
        _currentMode.Index = _modes.Count + 1;
        if (!string.IsNullOrWhiteSpace(_currentMode.Name))
            _modes.Add(_currentMode);
        _currentMode = NewMode();
    }

    private void RestoreCurrentModeUI()
    {
        if (_currentMode == null) return;
        ModeNameInput.Text = _currentMode.Name;
        ModeDescInput.Text = _currentMode.Description;
        SetAccentRadio(_currentMode.Accent);
        AppsList.Items.Clear();
        foreach (var a in _currentMode.Apps) AppsList.Items.Add(a);
        FoldersList.Items.Clear();
        foreach (var f in _currentMode.Folders) FoldersList.Items.Add(f);
        WebsitesList.Items.Clear();
        foreach (var w in _currentMode.Websites) WebsitesList.Items.Add(w);
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

    private void SetAccentRadio(string hex)
    {
        ColorRed.IsChecked = hex == "#D64545";
        ColorBlue.IsChecked = hex == "#4580D6";
        ColorGreen.IsChecked = hex == "#45D680";
        ColorOrange.IsChecked = hex == "#D6A045";
        ColorPurple.IsChecked = hex == "#A045D6";
    }

    private void RefreshReview()
    {
        // Show preview list using wrap objects with AccentBrush converted for binding
        var items = _modes.Select(m => new ModePreviewItem
        {
            Name = m.Name,
            Description = m.Description,
            ProcessCount = m.ProcessCount,
            AccentBrush = HexToBrush(m.Accent)
        }).ToList();
        ModesPreviewList.ItemsSource = items;
    }

    private static SolidColorBrush HexToBrush(string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
        }
    }

    private void AddApp_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select an application",
            Filter = "Applications|*.exe;*.lnk;*.com;*.bat|All Files|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
            AppsList.Items.Add(dialog.FileName);
    }

    private void AddAppQuick_Click(object sender, RoutedEventArgs e)
    {
        var text = AppQuickInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        AppsList.Items.Add(text);
        AppQuickInput.Clear();
    }

    private void RemoveApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string app })
            AppsList.Items.Remove(app);
    }

    private void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        // Use native WPF folder picker via OpenFileDialog workaround
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select a folder",
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select"
        };

        if (dialog.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(dir))
                FoldersList.Items.Add(dir);
        }
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string folder })
            FoldersList.Items.Remove(folder);
    }

    private void AddWebsite_Click(object sender, RoutedEventArgs e)
    {
        var text = WebsiteInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            StatusBar.Text = "URL must start with http:// or https://";
            return;
        }

        WebsitesList.Items.Add(text);
        WebsiteInput.Clear();
    }

    private void RemoveWebsite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string site })
            WebsitesList.Items.Remove(site);
    }

    private void AddMode_Click(object sender, RoutedEventArgs e)
    {
        // Clear inputs for new mode
        _currentMode = NewMode();
        ModeNameInput.Clear();
        ModeDescInput.Clear();
        AppsList.Items.Clear();
        FoldersList.Items.Clear();
        WebsitesList.Items.Clear();
        ColorRed.IsChecked = true;

        ShowStep(1);
        Keyboard.Focus(ModeNameInput);
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        if (_modes.Count == 0)
        {
            StatusBar.Text = "Create at least one mode to continue";
            return;
        }

        var config = new ArcynConfig
        {
            Modes = _modes
        };

        for (int i = 0; i < config.Modes.Count; i++)
            config.Modes[i].Index = i + 1;

        await _config.SaveAsync(config);

        StatusBar.Text = "Config saved. Launching ARCYN...";

        await Task.Delay(300);

        _particleTimer?.Stop();
        _particles?.Dispose();

        var mainWindow = new MainWindow();
        mainWindow.Show();

        Close();
    }

    private sealed class ModePreviewItem
    {
        public string Name { get; init; } = "";
        public string Description { get; init; } = "";
        public string Summary => $"{ProcessCount} targets";
        public int ProcessCount { get; init; }
        public SolidColorBrush AccentBrush { get; init; } = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));
    }
}
