using System.Diagnostics;
using ARCYN.Core.Models;

namespace ARCYN.Platform;

/// <summary>
/// Abstracts OS‑specific process launching.
/// Implementations create a ProcessStartInfo appropriate for the current platform.
/// </summary>
public interface IPlatformLauncher
{
    /// <summary>
    /// Build a ProcessStartInfo for launching the specified target.
    /// </summary>
    ProcessStartInfo CreateLaunchInfo(TargetItem target);
}