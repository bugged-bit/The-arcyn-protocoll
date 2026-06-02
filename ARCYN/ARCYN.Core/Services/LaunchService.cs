using System;
using System.Diagnostics;
using System.IO;
using ARCYN.Core.Models;
using ARCYN.Core.Utils;

namespace ARCYN.Core.Services;

public static class LaunchService
{
    // Validates and normalizes Linux launch targets before building ProcessStartInfo.
    public static bool TryPrepare(TargetItem target, out ProcessStartInfo psi, out string? error)
    {
        psi = null!;
        error = null;
        var cmd = Environment.ExpandEnvironmentVariables(target.LaunchCmd ?? string.Empty).Trim();
        var workingDir = ResolveWorkingDir(target, cmd);

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
                FileName = "xdg-open",
                Arguments = SharedHelper.QuotePath(folderPath),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = folderPath
            };
            return true;
        }

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
                FileName = "xdg-open",
                Arguments = cmd,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = AppContext.BaseDirectory
            };
            return true;
        }

        if (string.IsNullOrWhiteSpace(cmd))
        {
            error = "Empty command";
            return false;
        }

        if (Directory.Exists(cmd))
        {
            psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = SharedHelper.QuotePath(cmd),
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,
                WorkingDirectory = cmd
            };
            return true;
        }

        var ext = System.IO.Path.GetExtension(cmd);
        if (ext.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            error = "Windows .lnk shortcuts are not supported on Linux. Use a Linux command, .desktop launcher command, or absolute executable path.";
            return false;
        }

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

        if (Path.IsPathRooted(exePart))
        {
            if (!File.Exists(exePart))
            {
                error = "Executable not found";
                return false;
            }
        }

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
        if (target.Kind == TargetKind.Folder && !string.IsNullOrWhiteSpace(target.LaunchArg))
            return Environment.ExpandEnvironmentVariables(target.LaunchArg).Trim();

        if (target.Kind == TargetKind.App)
        {
            if (Path.IsPathRooted(expandedCmd) && File.Exists(expandedCmd))
                return System.IO.Path.GetDirectoryName(expandedCmd) ?? AppContext.BaseDirectory;
        }

        return AppContext.BaseDirectory;
    }

}
