using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Linq;
using ARCYN.UI.Models;

namespace ARCYN.UI.Services;

public sealed class ConfigService
{
    private const string ConfigFileName = "arcyn.json";
    private const string AppDataFolder = "ARCYN";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ArcynConfig? Load()
        {
            LogService.WriteStatic("Loading configuration");
        var path = ResolvePath();
        if (path == null) return null;
        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ArcynConfig>(json, JsonOptions);
            if (config == null) return null;
            var sanitized = ValidateAndSanitize(config);
            if (sanitized == null) return null;
            return sanitized;
        }
        catch { return null; }
    }

    public string? ResolvePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolder,
            ConfigFileName);
        if (File.Exists(appData))
            {
                LogService.WriteStatic("Resolved config path to AppData: {0}", appData);
                return appData;
            }
        var local = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(local))
            {
                LogService.WriteStatic("Resolved config path to local: {0}", local);
                return local;
            }
        return null;
    }

    public string GetOrCreatePath()
    {
        var local = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolder,
            ConfigFileName);
        var chosenPath = File.Exists(local) ? local : appData;
            LogService.WriteStatic("GetOrCreatePath using: {0}", chosenPath);
            return chosenPath;
    }

    public void Save(ArcynConfig config)
    {
        var path = GetOrCreatePath();
            LogService.WriteStatic("Saving config to {0}", path);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        for (int i = 0; i < config.Modes.Count; i++) config.Modes[i].Index = i + 1;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(path)) File.Replace(tempPath, path, null);
        else File.Move(tempPath, path);
    }

    public async Task SaveAsync(ArcynConfig config)
    {
        var path = GetOrCreatePath();
            LogService.WriteStatic("Saving config to {0}", path);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        for (int i = 0; i < config.Modes.Count; i++) config.Modes[i].Index = i + 1;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
        if (File.Exists(path)) File.Replace(tempPath, path, null);
        else File.Move(tempPath, path);
    }

    private ArcynConfig? ValidateAndSanitize(ArcynConfig config)
    {
LogService.WriteStatic("Validating and sanitizing configuration");
        // stray brace
        if (config == null) return null;
        if (config.Modes == null) config.Modes = [];
        var valid = new List<ModeConfig>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mode in config.Modes)
        {
            if (string.IsNullOrWhiteSpace(mode.Name)) continue;
            var nameKey = mode.Name.Trim();
            if (!seen.Add(nameKey)) continue;
            mode.Apps ??= [];
            mode.Websites ??= [];
            mode.Folders ??= [];
            mode.Apps = mode.Apps.Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            mode.Folders = mode.Folders.Select(f => Environment.ExpandEnvironmentVariables(f.Trim())).Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            mode.Websites = mode.Websites.Select(u => u.Trim()).Where(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (mode.Apps.Count == 0 && mode.Websites.Count == 0 && mode.Folders.Count == 0) continue;
            valid.Add(mode);
        }
        if (valid.Count == 0) return null;
        for (int i = 0; i < valid.Count; i++) valid[i].Index = i + 1;
        config.Modes = valid;
        config.Theme ??= new ThemeConfig();
        config.Behavior ??= new BehaviorConfig();
        return config;
    }
}