using System;

namespace ARCYN.Platform;

/// <summary>
/// No‑op implementation for platforms that do not support acrylic blur.
/// </summary>
public class NoOpWindowEffects : IWindowEffects
{
    public void EnableAcrylic(IntPtr windowHandle, uint opacityColor = 0xEB000000)
    {
        // Intentionally empty – acrylic blur only supported on Windows with specific APIs.
    }
}