using System;
using System.Diagnostics;
using ARCYN.Core.Models;
using ARCYN.Core.Services;

namespace ARCYN.Platform;

/// <summary>
/// Linux‑specific process launcher.
/// Uses xdg-open if available, otherwise falls back to gio.
/// Logs verification status and provides clear error messages when no opener is present.
/// </summary>
public sealed class LinuxPlatformLauncher : IPlatformLauncher
{
    private const string XdgOpen = "xdg-open";
    private const string GioOpen = "gio"; // "gio open <path>"

    private static readonly string _folderCommand;
    private static readonly string _urlCommand;
    private static readonly bool _hasXdgOpen;
    private static readonly bool _hasGio;

    static LinuxPlatformLauncher()
    {
        _hasXdgOpen = CommandHelper.IsCommandAvailable(XdgOpen);
        _hasGio = CommandHelper.IsCommandAvailable(GioOpen);

        if (_hasXdgOpen)
        {
            _folderCommand = XdgOpen;
            _urlCommand = XdgOpen;
            LogService.WriteStatic("VERIFIED – using xdg-open for folder and URL launch.");
        }
        else if (_hasGio)
        {
            _folderCommand = GioOpen;
            _urlCommand = GioOpen;
            LogService.WriteStatic("VERIFIED – using gio for folder and URL launch.");
        }
        else
        {
            _folderCommand = string.Empty;
            _urlCommand = string.Empty;
            LogService.WriteStatic("UNVERIFIED – neither xdg-open nor gio found. Folder/URL launch will fail. Install xdg-utils or gio.");
        }
    }

    public ProcessStartInfo CreateLaunchInfo(TargetItem target)
    {
        // Folder launch
        if (target.Kind == TargetKind.Folder)
        {
            if (string.IsNullOrEmpty(_folderCommand))
                throw new InvalidOperationException("No folder opener (xdg-open or gio) available on this Linux system.");

            var folder = target.LaunchArg ?? string.Empty;
            var psi = new ProcessStartInfo
            {
                FileName = _folderCommand,
                UseShellExecute = true,
                WorkingDirectory = folder
            };

            // gio requires "open" sub‑command
            if (psi.FileName == GioOpen)
                psi.Arguments = $"open {QuotePath(folder)}";
            else
                psi.Arguments = QuotePath(folder);

            return psi;
        }

        // URL launch
        if (target.Kind == TargetKind.Website)
        {
            if (string.IsNullOrEmpty(_urlCommand))
                throw new InvalidOperationException("No URL opener (xdg-open or gio) available on this Linux system.");

            var url = target.LaunchCmd ?? string.Empty;
            var psi = new ProcessStartInfo
            {
                FileName = _urlCommand,
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory
            };

            if (psi.FileName == GioOpen)
                psi.Arguments = $"open {QuotePath(url)}";
            else
                psi.Arguments = QuotePath(url);

            return psi;
        }

        // Executable or .lnk – launch directly (same logic as Windows).
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
