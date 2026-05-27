using System;
using System.Diagnostics;
using System.IO;
using ARCYN.UI.Models;
using ARCYN.Platform;

namespace ARCYN.UI.Services;

public static class LaunchService
{
    // New robust creation – validates and normalizes before building ProcessStartInfo.
    public static bool TryPrepare(TargetItem target, out ProcessStartInfo psi, out string? error)
    {
        psi = null!;
        error = null;
        // Expand environment variables first
        var cmd = Environment.ExpandEnvironmentVariables(target.LaunchCmd ?? string.Empty).Trim();
        var workingDir = ResolveWorkingDir(target, cmd);

        // Folder launch – delegate to platform launcher
        if (target.Kind == TargetKind.Folder)
        {
            try
            {
                psi = _launcher.CreateLaunchInfo(target);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Website launch – delegate to platform launcher
        if (target.Kind == TargetKind.Website)
        {
            try
            {
                psi = _launcher.CreateLaunchInfo(target);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Guard against empty command for non‑folder/website targets
        if (string.IsNullOrWhiteSpace(cmd))
        {
            error = "Empty command";
            return false;
        }




        // Shortcut (.lnk) – ensure file exists
        var ext = System.IO.Path.GetExtension(cmd);
        if (ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(cmd))
            {
                error = ".lnk file not found";
                return false;
            }
            psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = string.Empty,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = workingDir
            };
            return true;
        }

        // Executable or shell command handling
        // Separate executable part from arguments to support commands like "git status".
        string exePart = cmd;
        string argsPart = string.Empty;
        if (cmd.Contains(' '))
        {
            // Handle quoted executable paths
            if (cmd.StartsWith('"'))
            {
                var endQuoteIdx = cmd.IndexOf('"', 1);
                if (endQuoteIdx > 1)
{
    private static readonly IPlatformLauncher _launcher = PlatformLauncherFactory.Create();
                    exePart = cmd.Substring(1, endQuoteIdx - 1);
                    argsPart = cmd.Substring(endQuoteIdx + 1).Trim();
                }
            }
            else
            {
                var split = cmd.Split(new[] { ' ' }, 2);
                exePart = split[0];
                if (split.Length > 1)
                    argsPart = split[1];
            }
        }

        // If we have a rooted executable path, verify it exists.
        if (Path.IsPathRooted(exePart))
        {
            if (!File.Exists(exePart))
            {
                error = "Executable not found";
                return false;
            }
        }
        // For non‑rooted executables we rely on OS PATH resolution.
        // Build ProcessStartInfo using separated parts.
        psi = new ProcessStartInfo
        {
            FileName = exePart,
            Arguments = argsPart,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
            WorkingDirectory = workingDir
        };
        return true;
    }

    private static string ResolveWorkingDir(TargetItem target, string expandedCmd)
    {
        // Folder kind – use folder itself
        if (target.Kind == TargetKind.Folder && !string.IsNullOrWhiteSpace(target.LaunchArg))
            return Environment.ExpandEnvironmentVariables(target.LaunchArg).Trim();

        // App kind – use directory of executable if absolute path exists
        if (target.Kind == TargetKind.App)
        {
            if (Path.IsPathRooted(expandedCmd) && File.Exists(expandedCmd))
                return System.IO.Path.GetDirectoryName(expandedCmd) ?? AppContext.BaseDirectory;
        }
        // Default – application base directory
        return AppContext.BaseDirectory;
    }

    public static string QuotePath(string path)
    {
        return path.Contains(' ') ? $"\"{path}\"" : path;
    }
}