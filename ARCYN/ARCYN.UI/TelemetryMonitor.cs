using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ARCYN.UI;

public sealed class TelemetryMonitor : IDisposable
{
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _ramAvailCounter;
    private readonly double _totalRamMb;
    private bool _disposed;

    public float CpuPercent { get; private set; }
    public float RamPercent { get; private set; }

    public TelemetryMonitor()
    {
        // Initialize counters only on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ = _cpuCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
            }

            try
            {
                _ramAvailCounter = new PerformanceCounter("Memory", "Available MBytes");
                _ = _ramAvailCounter.NextValue();
            }
            catch
            {
                _ramAvailCounter = null;
            }
        }

        _totalRamMb = GetTotalRamMb();
    }

    private static double GetTotalRamMb()
    {
        // Windows: use WMI
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                    return Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024.0 * 1024.0);
            }
            catch { }
        }

        // Linux / Unix: parse /proc/meminfo
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
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
        }

        // Fallback default (16 GB)
        return 16384;
    }

    public void Sample()
    {
        if (_disposed) return;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_cpuCounter != null)
                    CpuPercent = (float)Math.Round(_cpuCounter.NextValue(), 1);

                if (_ramAvailCounter != null && _totalRamMb > 0)
                {
                    var used = _totalRamMb - _ramAvailCounter.NextValue();
                    RamPercent = (float)Math.Round(Math.Clamp(used / _totalRamMb * 100.0, 0, 100), 1);
                }
            }
            else
            {
                // On non‑Windows platforms, set placeholders (could be extended later)
                CpuPercent = 0;
                RamPercent = 0;
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cpuCounter?.Dispose();
        _ramAvailCounter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
