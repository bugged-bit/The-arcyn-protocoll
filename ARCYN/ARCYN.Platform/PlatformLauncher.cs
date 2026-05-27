using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ARCYN.UI.Models;

namespace ARCYN.Platform;

/// <summary>
/// OS‑specific implementation of IPlatformLauncher.
/// Handles folder, URL and generic executable launching across Windows, Linux and macOS.
/// </summary>
public class PlatformLauncher : IPlatformLauncher
{
    public ProcessStartInfo CreateLaunchInfo(TargetItem target)
    {
        // Folder launch – use system file manager
        if (target.Kind == TargetKind.Folder)
        {
            var folder = target.LaunchArg ?? string.Empty;
            return new ProcessStartInfo
            {
                FileName = GetFolderOpenCommand(),
                Arguments = QuotePath(folder),
                UseShellExecute = true,
                WorkingDirectory = folder
            };
        }

        // Website launch – on Windows the URL itself can be launched directly.
        if (target.Kind == TargetKind.Website)
        {
            var url = target.LaunchCmd ?? string.Empty;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };
            }
            // Linux/macOS use xdg-open/open
            return new ProcessStartInfo
            {
                FileName = GetUrlOpenCommand(),
                Arguments = QuotePath(url),
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }

        // Application or .lnk – invoke directly.
        var cmd = target.LaunchCmd ?? string.Empty;
        var args = string.Empty;

        // Split args if there are spaces and not a quoted path.
        if (cmd.Contains(' ') && !cmd.StartsWith('"'))
        {
            var split = cmd.Split(new[] { ' ' }, 2);
            cmd = split[0];
            if (split.Length > 1) args = split[1];
        }
        else if (cmd.StartsWith('"'))
        {
            var endQuoteIdx = cmd.IndexOf('"', 1);
            if (endQuoteIdx > 1)
            {
                var exePart = cmd.Substring(1, endQuoteIdx - 1);
                var remainder = cmd.Substring(endQuoteIdx + 1).Trim();
                cmd = exePart;
                args = remainder;
            }
        }

        return new ProcessStartInfo
        {
            FileName = cmd,
            Arguments = args,
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
    }

    private static string GetFolderOpenCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "explorer.exe";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "xdg-open";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "open";
        return "explorer.exe"; // fallback
    }

    private static string GetUrlOpenCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "xdg-open";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "open";
        // Windows handled above directly.
        return "xdg-open"; // default fallback
    }

    private static string QuotePath(string path) => path.Contains(' ') ? $"\"{path}\"" : path;
}