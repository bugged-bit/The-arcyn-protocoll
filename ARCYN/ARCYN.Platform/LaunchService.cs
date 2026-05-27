using System;
using System.Diagnostics;
using System.IO;
using ARCYN.UI.Models;
using ARCYN.Platform;

namespace ARCYN.UI.Services;

/// <summary>
/// Central service that validates a <see cref="TargetItem"/> and prepares a <see cref="ProcessStartInfo"/> using the platform‑specific <see cref="IPlatformLauncher"/> implementation.
/// All logging is routed through <see cref="LogService"/> so that the output is captured on every OS.
/// </summary>
public static class LaunchService
{
    // Platform‑specific launcher selected once at startup.
    private static readonly IPlatformLauncher _launcher = PlatformLauncherFactory.Create();

    /// <summary>
    /// Validates the target and, if valid, creates a <see cref="ProcessStartInfo"/> suitable for the current operating system.
    /// Returns <c>false</c> on validation error and provides a human‑readable <paramref name="error"/>.
    /// </summary>
    public static bool TryPrepare(TargetItem target, out ProcessStartInfo psi, out string? error)
    {
        psi = null!;
        error = null;

        LogService.WriteStatic("TryPrepare called for target: Kind={0}, Cmd={1}, Arg={2}",
            target.Kind, target.LaunchCmd, target.LaunchArg);

        // ------------------------------------------------------------
        // Folder validation – ensure the expanded path exists.
        // ------------------------------------------------------------
        if (target.Kind == TargetKind.Folder)
        {
            var folderPath = Environment.ExpandEnvironmentVariables(target.LaunchArg ?? string.Empty).Trim();
            LogService.WriteStatic("Checking folder existence: {0}", folderPath);
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                error = "Folder does not exist";
                LogService.WriteStatic("Folder validation failed: {0}", folderPath);
                return false;
            }
        }

        // ------------------------------------------------------------
        // URL validation – must be absolute http(s) URL.
        // ------------------------------------------------------------
        if (target.Kind == TargetKind.Website)
        {
            var url = target.LaunchCmd ?? string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "Invalid URL";
                LogService.WriteStatic("URL validation failed: {0}", url);
                return false;
            }
        }

        // ------------------------------------------------------------
        // Platform‑specific ProcessStartInfo creation.
        // ------------------------------------------------------------
        try
        {
            psi = _launcher.CreateLaunchInfo(target);
            LogService.WriteStatic(
                "Prepared ProcessStartInfo: FileName='{0}' Arguments='{1}' WorkingDirectory='{2}'",
                psi.FileName, psi.Arguments, psi.WorkingDirectory);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            LogService.WriteStatic("Launch preparation exception: {0}", ex);
            return false;
        }
    }
}
