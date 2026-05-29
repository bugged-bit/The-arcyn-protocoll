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
    private const string OldConfigFileName = "protocoll.json";
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
        var path = ResolvePath();
        if (path == null)
            return null;

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<ArcynConfig>(json, JsonOptions);
            if (config == null) return null;

            // Validate and sanitize configuration – removes corrupt entries and enforces defaults
            var sanitized = ValidateAndSanitize(config);
            if (sanitized == null)
            {
                LogService.WriteStatic("Config validation failed – using empty configuration.");
                return null;
            }
            return sanitized;
        }
        catch (Exception ex)
        {
            LogService.WriteStatic("Config load error: {0}", ex.Message);
            return null;
        }
    }

    public string? ResolvePath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolder,
            ConfigFileName);

        if (File.Exists(appData))
            return appData;

        var local = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(local))
            return local;

        return TryMigrateOldConfig();
    }

    public string GetOrCreatePath()
    {
        var local = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolder,
            ConfigFileName);

        if (File.Exists(local))
            return local;

        return appData;
    }

    public void Save(ArcynConfig config)
    {
        var path = GetOrCreatePath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Re‑index before persisting
        for (int i = 0; i < config.Modes.Count; i++)
            config.Modes[i].Index = i + 1;

        var json = JsonSerializer.Serialize(config, JsonOptions);
        // Write to a temporary file then replace atomically to avoid corruption on crash
        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, json);
        if (File.Exists(path))
            File.Replace(tempPath, path, null);
        else
            File.Move(tempPath, path);
    }

    public async Task SaveAsync(ArcynConfig config)
    {
        var path = GetOrCreatePath();
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Re‑index before persisting
        for (int i = 0; i < config.Modes.Count; i++)
            config.Modes[i].Index = i + 1;

        var json = JsonSerializer.Serialize(config, JsonOptions);
        // Write to temp then atomically replace
        var tempPath = path + ".tmp";
        await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
        if (File.Exists(path))
            File.Replace(tempPath, path, null);
        else
            File.Move(tempPath, path);
    }

    private string? TryMigrateOldConfig()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, OldConfigFileName),
            Path.Combine(AppContext.BaseDirectory, "..", OldConfigFileName),
            Path.Combine(Environment.CurrentDirectory, OldConfigFileName)
        ];

        foreach (var oldPath in candidates)
        {
            if (!File.Exists(oldPath))
                continue;

            try
            {
                var json = File.ReadAllText(oldPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                    continue;

                var config = new ArcynConfig();

                foreach (var modeEl in root.EnumerateArray())
                {
                    var mode = new ModeConfig
                    {
                        Name = GetString(modeEl, "label") ?? "MODE",
                        Description = GetString(modeEl, "subtitle") ?? string.Empty,
                        Accent = "#D64545"
                    };

                    if (modeEl.TryGetProperty("targets", out var targets))
                    {
                        foreach (var t in targets.EnumerateArray())
                        {
                            var cmd = GetString(t, "cmd") ?? string.Empty;
                            var args = GetStringArray(t, "args");

                            if (string.IsNullOrWhiteSpace(cmd))
                                continue;

                            if (cmd.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase) && args.Count > 0)
                            {
                                mode.Folders.Add(args[0]);
                            }
                            else if (cmd.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                     cmd.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                            {
                                mode.Websites.Add(cmd);
                            }
                            else
                            {
                                mode.Apps.Add(cmd);
                            }
                        }
                    }

                    if (mode.Apps.Count > 0 || mode.Websites.Count > 0 || mode.Folders.Count > 0)
                        config.Modes.Add(mode);
                }

                if (config.Modes.Count > 0)
                {
                    var newPath = GetOrCreatePath();
                    var dir = Path.GetDirectoryName(newPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    Save(config);

                    LogService.WriteStatic("Migrated config from {0} → {1}", oldPath, newPath);
                    return newPath;
                }
            }
            catch
            {
                // ignore migration failures
            }
        }

        return null;
    }

    private static string GetString(JsonElement el, string key)
    {
        return el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;
    }

    private static List<string> GetStringArray(JsonElement el, string key)
    {
        var result = new List<string>();
        if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in prop.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    result.Add(item.GetString() ?? string.Empty);
            }
        }
        return result;
    }

    // ---------------------------------------------------------------------
    // Validation & sanitization ------------------------------------------------
    // ---------------------------------------------------------------------
    private ArcynConfig? ValidateAndSanitize(ArcynConfig config)
    {
        if (config == null) return null;
        // Ensure modes list exists
        if (config.Modes == null) config.Modes = [];

        var validModes = new List<ModeConfig>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mode in config.Modes)
        {
            // Basic name check – skip if empty or duplicate
            if (string.IsNullOrWhiteSpace(mode.Name)) continue;
            var nameKey = mode.Name.Trim();
            if (!seenNames.Add(nameKey)) continue; // duplicate name – skip

            // Ensure collection properties are not null
            mode.Apps ??= [];
            mode.Websites ??= [];
            mode.Folders ??= [];

            // Sanitize app entries – trim whitespace, remove empty entries
            mode.Apps = mode.Apps.Select(a => a.Trim()).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            // Sanitize folder entries – keep only existing directories
            mode.Folders = mode.Folders.Select(f => f.Trim()).Where(f => Directory.Exists(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            // Sanitize website entries – keep only well‑formed http/https URLs
            mode.Websites = mode.Websites.Select(u => u.Trim()).Where(u =>
            {
                if (Uri.TryCreate(u, UriKind.Absolute, out var uri))
                    return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
                return false;
            }).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // If mode has no targets after sanitization, drop it
            if (mode.Apps.Count == 0 && mode.Websites.Count == 0 && mode.Folders.Count == 0)
                continue;

            validModes.Add(mode);
        }
        // If no valid modes remain, configuration is unusable
        if (validModes.Count == 0) return null;

        // Re‑index sequentially
        for (int i = 0; i < validModes.Count; i++)
            validModes[i].Index = i + 1;

        config.Modes = validModes;
        // Ensure Theme and Behavior sections have defaults (they are already instantiated by default ctor)
        config.Theme ??= new ThemeConfig();
        config.Behavior ??= new BehaviorConfig();
        return config;
    }
}
