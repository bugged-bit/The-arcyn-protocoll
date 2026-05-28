using System;
using System.Runtime.InteropServices;
using ARCYN.Core.Services;

namespace ARCYN.Platform;

/// <summary>
/// Factory that returns the appropriate <see cref="IPlatformLauncher"/> implementation for the current OS.
/// Logs which concrete launcher is selected so that verification status can be inspected in the log file.
/// </summary>
public static class PlatformLauncherFactory
{
    public static IPlatformLauncher Create()
    {
        IPlatformLauncher launcher;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            launcher = new WindowsPlatformLauncher();
            LogService.WriteStatic("VERIFIED – WindowsPlatformLauncher selected.");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            launcher = new LinuxPlatformLauncher();
            LogService.WriteStatic("VERIFIED – LinuxPlatformLauncher selected.");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            launcher = new MacPlatformLauncher();
            LogService.WriteStatic("VERIFIED – MacPlatformLauncher selected.");
        }
        else
        {
            var msg = "UNVERIFIED – Unknown OS; cannot determine appropriate PlatformLauncher.";
            LogService.WriteStatic(msg);
            throw new PlatformNotSupportedException("Unsupported OS. ARCYN supports Windows, Linux, and macOS.");
        }
        return launcher;
    }
}
