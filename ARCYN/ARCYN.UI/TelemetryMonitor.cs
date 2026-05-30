namespace ARCYN.UI;

/// <summary>
/// Monitors CPU and RAM usage. On Windows uses PerformanceCounter/WMI;
/// on other platforms returns stubs.
/// </summary>
public sealed class TelemetryMonitor : IDisposable
{
#if WINDOWS
    private System.Diagnostics.PerformanceCounter? _cpuCounter;
    private System.Diagnostics.PerformanceCounter? _ramAvailCounter;
    private readonly double _totalRamMb;
#endif
    private bool _disposed;

    public float CpuPercent { get; private set; }
    public float RamPercent { get; private set; }

    public TelemetryMonitor()
    {
#if WINDOWS
        try
        {
            _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ = _cpuCounter.NextValue();
        }
        catch
        {
            _cpuCounter = null;
        }

        try
        {
            _ramAvailCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            _ = _ramAvailCounter.NextValue();
        }
        catch
        {
            _ramAvailCounter = null;
        }

        _totalRamMb = GetTotalRamMb();
#endif
    }

#if WINDOWS
    private static double GetTotalRamMb()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
                return Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024.0 * 1024.0);
        }
        catch { }
        return 16384;
    }
#endif

    public void Sample()
    {
        if (_disposed) return;
#if WINDOWS
        try
        {
            if (_cpuCounter != null)
                CpuPercent = (float)Math.Round(_cpuCounter.NextValue(), 1);

            if (_ramAvailCounter != null && _totalRamMb > 0)
            {
                var used = _totalRamMb - _ramAvailCounter.NextValue();
                RamPercent = (float)Math.Round(Math.Clamp(used / _totalRamMb * 100.0, 0, 100), 1);
            }
        }
        catch { }
#endif
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
#if WINDOWS
        _cpuCounter?.Dispose();
        _ramAvailCounter?.Dispose();
#endif
        GC.SuppressFinalize(this);
    }
}
