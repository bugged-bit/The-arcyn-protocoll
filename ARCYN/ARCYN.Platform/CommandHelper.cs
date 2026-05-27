using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ARCYN.UI.Services;

namespace ARCYN.Platform;

/// <summary>
/// Helper utilities for platform‑specific command detection.
/// </summary>
internal static class CommandHelper
{
    /// <summary>
    /// Checks whether an executable with the given name exists in any directory of the PATH environment variable.
    /// Works on Windows (adds .exe) and Linux/macOS.
    /// Logs verification status for Linux‑specific commands.
    /// </summary>
    public static bool IsCommandAvailable(string command)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            LogService.WriteStatic("UNVERIFIED – PATH environment variable is empty; cannot locate command '{0}'.", command);
            return false;
        }

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new[] { ".exe", "" } : new[] { "" };
        var pathDirs = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var dir in pathDirs)
        {
            foreach (var ext in extensions)
            {
                var candidate = Path.Combine(dir, command + ext);
                if (File.Exists(candidate))
                {
                    LogService.WriteStatic("VERIFIED – command '{0}' found at '{1}'.", command, candidate);
                    return true;
                }
            }
        }
        LogService.WriteStatic("UNVERIFIED – command '{0}' not found in PATH.", command);
        return false;
    }
}
