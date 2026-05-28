using System.Diagnostics;
using ARCYN.Core.Models;

namespace ARCYN.Platform;

/// <summary>
/// Windows‑specific process launcher.
/// Uses the built‑in explorer for folders and opens URLs directly.
/// </summary>
public sealed class WindowsPlatformLauncher : IPlatformLauncher
{
    public ProcessStartInfo CreateLaunchInfo(TargetItem target)
    {
        // Folder launch – use explorer.exe with quoted path
        if (target.Kind == TargetKind.Folder)
        {
            var folder = target.LaunchArg ?? string.Empty;
            return new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = QuotePath(folder),
                UseShellExecute = true,
                WorkingDirectory = folder
            };
        }

        // URL launch – on Windows the URL string itself can be started directly.
        if (target.Kind == TargetKind.Website)
        {
            var url = target.LaunchCmd ?? string.Empty;
            return new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }

        // Executable or .lnk – launch directly.
        var cmd = target.LaunchCmd ?? string.Empty;
        var args = string.Empty;
        // Basic splitting – same logic as before.
        if (cmd.Contains(' ') && !cmd.StartsWith('"'))
        {
            var split = cmd.Split(new[] { ' ' }, 2);
            cmd = split[0];
            if (split.Length > 1) args = split[1];
        }
        else if (cmd.StartsWith('"'))
        {
            var end = cmd.IndexOf('"', 1);
            if (end > 1)
            {
                var exe = cmd.Substring(1, end - 1);
                var remainder = cmd.Substring(end + 1).Trim();
                cmd = exe;
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

    private static string QuotePath(string path) => path.Contains(' ') ? $"\"{path}\"" : path;
}
