using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace ARCYN.UI.Models;

public sealed class ModeConfig : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _accent = "#D64545";
    private List<string> _apps = [];
    private List<string> _websites = [];
    private List<string> _folders = [];
    private int _index;
    private int _launchCount;
    private DateTime? _lastLaunchedAt;
    private string _lastLaunchOutcome = "STANDBY";

    [JsonPropertyName("name")]
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    [JsonPropertyName("description")]
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    [JsonPropertyName("accent")]
    public string Accent
    {
        get => _accent;
        set
        {
            if (!SetField(ref _accent, value)) return;
            OnPropertyChanged(nameof(AccentColor));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(AccentBarBrush));
        }
    }

    [JsonIgnore]
    public Color AccentColor => ColorFromHex(_accent);

    [JsonIgnore]
    public SolidColorBrush AccentBrush => new(AccentColor);

    [JsonIgnore]
    public SolidColorBrush AccentBarBrush => new(Color.FromArgb(0x66, AccentColor.R, AccentColor.G, AccentColor.B));

    [JsonPropertyName("apps")]
    public List<string> Apps
    {
        get => _apps;
        set => SetField(ref _apps, value);
    }

    [JsonPropertyName("websites")]
    public List<string> Websites
    {
        get => _websites;
        set => SetField(ref _websites, value);
    }

    [JsonPropertyName("folders")]
    public List<string> Folders
    {
        get => _folders;
        set => SetField(ref _folders, value);
    }

    [JsonIgnore]
    public int Index
    {
        get => _index;
        set
        {
            if (!SetField(ref _index, value))
                return;

            OnPropertyChanged(nameof(IndexLabel));
            OnPropertyChanged(nameof(ShortcutHint));
        }
    }

    [JsonIgnore]
    public string IndexLabel => Index <= 0 ? "--" : Index.ToString("D2");

    [JsonIgnore]
    public string ShortcutHint => $"[{Math.Max(Index, 1)}]";

    [JsonIgnore]
    public int ProcessCount => _apps.Count + _websites.Count + _folders.Count;

    [JsonIgnore]
    public string ProcessLabel => $"{ProcessCount:D2} PROC";

    [JsonIgnore]
    public string LaunchCountLabel => $"{_launchCount:D2} RUN";

    [JsonIgnore]
    public string LastLaunchLabel => _lastLaunchedAt?.ToString("HH:mm") ?? "NONE";

    [JsonIgnore]
    public string LastOutcomeLabel => _lastLaunchOutcome;

    [JsonIgnore]
    public List<TargetItem> Targets
    {
        get
        {
            var items = new List<TargetItem>();
            foreach (var app in _apps)
            {
                if (string.IsNullOrWhiteSpace(app)) continue;
                items.Add(new TargetItem(
                    MakeAppLabel(app),
                    app,
                    string.Empty,
                    TargetKind.App));
            }
            foreach (var site in _websites)
            {
                if (string.IsNullOrWhiteSpace(site)) continue;
                items.Add(new TargetItem(
                    MakeWebLabel(site),
                    site,
                    string.Empty,
                    TargetKind.Website));
            }
            foreach (var folder in _folders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                var name = Path.GetFileName(folder.TrimEnd('\\', '/'));
                items.Add(new TargetItem(
                    string.IsNullOrEmpty(name) ? folder : name,
                    "explorer.exe",
                    folder,
                    TargetKind.Folder));
            }
            return items;
        }
    }

    public void RecordLaunch(int launched, int total, bool fullSuccess)
    {
        _launchCount++;
        _lastLaunchedAt = DateTime.Now;
        _lastLaunchOutcome = fullSuccess
            ? "SYNCED"
            : launched > 0
                ? $"PARTIAL {launched:D2}/{total:D2}"
                : "FAULT";

        OnPropertyChanged(nameof(LaunchCountLabel));
        OnPropertyChanged(nameof(LastLaunchLabel));
        OnPropertyChanged(nameof(LastOutcomeLabel));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static Color ColorFromHex(string hex)
    {
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch { return Color.FromRgb(0xD6, 0x45, 0x45); }
    }

    private static string MakeAppLabel(string app)
    {
        var name = Path.GetFileNameWithoutExtension(app);
        return string.IsNullOrEmpty(name) ? app : name;
    }

    private static string MakeWebLabel(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var host = uri.Host;
            return string.IsNullOrWhiteSpace(host) ? url : host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        return url;
    }
}

public enum TargetKind { App, Website, Folder }

public sealed record TargetItem(
    [property: JsonIgnore] string DisplayLabel,
    [property: JsonIgnore] string LaunchCmd,
    [property: JsonIgnore] string LaunchArg,
    [property: JsonIgnore] TargetKind Kind);
