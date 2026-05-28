using System;
using System.IO;
using System.Runtime.InteropServices;
using ARCYN.Core.Abstractions;

namespace ARCYN.Platform;

/// <summary>
/// Provides config file locations respecting platform conventions.
/// </summary>
public class ConfigPathProvider : IConfigPathProvider
{
    private const string ConfigFileName = "arcyn.json";
    private const string AppDataFolder = "ARCYN";

    public string? ResolvePath()
    {
        // Windows: %APPDATA%\ARCYN\arcyn.json
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder,
                ConfigFileName);
            if (File.Exists(appData))
                return appData;
        }
        else // Linux/macOS – XDG config directories
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrWhiteSpace(xdg))
                xdg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            var path = Path.Combine(xdg, "Arcyn", ConfigFileName);
            if (File.Exists(path))
                return path;
        }
        // Fallback: local directory next to executable
        var local = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        return File.Exists(local) ? local : null;
    }

    public string GetOrCreatePath()
    {
        // Prefer platform‑specific location, ensure directory exists.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDataFolder);
            Directory.CreateDirectory(appDataDir);
            return Path.Combine(appDataDir, ConfigFileName);
        }
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrWhiteSpace(xdg))
                xdg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            var dir = Path.Combine(xdg, "Arcyn");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, ConfigFileName);
        }
    }
}