using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ARCYN.Core.Models;

public class ModeConfig : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _accent = "#D64545";
    private List<string> _apps = [];
    private List<string> _websites = [];
    private List<string> _folders = [];
    private string? _shortcut;
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
        set => SetField(ref _accent, value);
    }

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

    [JsonPropertyName("shortcut")]
    public string? Shortcut
    {
        get => _shortcut;
        set
        {
            if (!SetField(ref _shortcut, value))
                return;

            OnPropertyChanged(nameof(ShortcutHint));
        }
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
    public string ShortcutHint
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_shortcut) && KeyCombo.TryParse(_shortcut, out var combo))
                return combo.ToString();
            return $"[{Math.Max(Index, 1)}]";
        }
    }

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
                    "xdg-open",
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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
