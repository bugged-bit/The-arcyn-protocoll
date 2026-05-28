using System;
using System.Diagnostics;
using ARCYN.Core.Models;

namespace ARCYN.Platform;

/// <summary>
/// macOS‑specific process launcher.
/// Uses the built‑in "open" command for folders and URLs.
/// </summary>
public sealed class MacPlatformLauncher : IPlatformLauncher
{
    private const string OpenCmd = "open";

    public ProcessStartInfo CreateLaunchInfo(TargetItem target)
    {
        if (target.Kind == TargetKind.Folder || target.Kind == TargetKind.Website)
        {
            var arg = target.Kind == TargetKind.Folder ? target.LaunchArg : target.LaunchCmd;
            return new ProcessStartInfo
            {
                FileName = OpenCmd,
                Arguments = QuotePath(arg ?? string.Empty),
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };
        }

        // Executable or .lnk – launch directly (same logic as Windows/Linux).
        var cmd = target.LaunchCmd ?? string.Empty;
        var args = string.Empty;
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
