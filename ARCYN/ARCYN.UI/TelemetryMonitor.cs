using System;
using System.IO;

namespace ARCYN.UI;

public sealed class TelemetryMonitor : IDisposable
{
#if WINDOWS
    private long _prevIdleTime;
    private long _prevKernelTime;
    private long _prevUserTime;
#endif
    private readonly double _totalRamMb;
    private bool _disposed;

    public float CpuPercent { get; private set; }
    public float RamPercent { get; private set; }

    public TelemetryMonitor()
    {
#if WINDOWS
        _totalRamMb = GetTotalRamMb();
#else
        _totalRamMb = GetTotalRamMb();
#endif
    }

    private static double GetTotalRamMb()
    {
#if WINDOWS
        try
        {
            if (NativeMethods.GetPhysicallyInstalledSystemMemory(out var kb))
                return kb / 1024.0;
        }
        catch { }
#else
        // Linux: parse /proc/meminfo
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && long.TryParse(parts[1], out var kb))
                        return kb / 1024.0;
                }
            }
        }
        catch { }
#endif
        return 16384;
    }

    public void Sample()
    {
        if (_disposed) return;
        try
        {
#if WINDOWS
            if (NativeMethods.GetSystemTimesNative(out var idle, out var kernel, out var user))
            {
                if (_prevIdleTime != 0)
                {
                    var totalDelta = (kernel + user) - (_prevKernelTime + _prevUserTime);
                    var idleDelta = idle - _prevIdleTime;
                    if (totalDelta > 0)
                        CpuPercent = (float)Math.Round((totalDelta - idleDelta) * 100.0 / totalDelta, 1);
                }
                _prevIdleTime = idle;
                _prevKernelTime = kernel;
                _prevUserTime = user;
            }

            var mem = NativeMethods.GetMemoryStatus();
            if (mem.ullTotalPhys > 0)
            {
                var used = mem.ullTotalPhys - mem.ullAvailPhys;
                RamPercent = (float)Math.Round(Math.Clamp(used * 100.0 / (double)mem.ullTotalPhys, 0, 100), 1);
            }
#else
            CpuPercent = 0;
            RamPercent = 0;
#endif
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
