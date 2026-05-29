using System;
using System.Diagnostics;
using System.IO;
using ARCYN.UI.Models;

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

        // Folder launch – ensure folder exists
        if (target.Kind == TargetKind.Folder)
        {
            var folderPath = Environment.ExpandEnvironmentVariables(target.LaunchArg ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                error = "Folder does not exist";
                return false;
            }
            psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = QuotePath(folderPath),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = folderPath
            };
            return true;
        }

        // Website launch – validate URL
        if (target.Kind == TargetKind.Website)
        {
            if (!Uri.TryCreate(cmd, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "Invalid URL";
                return false;
            }
            psi = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = string.Empty,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = AppContext.BaseDirectory
            };
            return true;
        }

        // Guard against empty command for non‑folder/website targets
        if (string.IsNullOrWhiteSpace(cmd))
        {
            error = "Empty command";
            return false;
        }
        // Path ending with slash – treat as folder launch via explorer
        if (cmd.Length >= 3 && cmd[1] == ':' && (cmd.EndsWith("\\") || cmd.EndsWith("/")))
        {
            if (!Directory.Exists(cmd))
            {
                error = "Folder path does not exist";
                return false;
            }
            psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = QuotePath(cmd),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = cmd
            };
            return true;
        }

        // Directories – open in explorer
        if (Directory.Exists(cmd))
        {
            psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = QuotePath(cmd),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = cmd
            };
            return true;
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
