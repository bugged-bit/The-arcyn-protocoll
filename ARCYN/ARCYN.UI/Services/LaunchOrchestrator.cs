using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ARCYN.UI.Models;
using ARCYN.UI.Services;

namespace ARCYN.UI.Services;

public sealed class LaunchOrchestrator
{
    private readonly LogService _log;

    public LaunchOrchestrator(LogService log) => _log = log;

    public async Task<LaunchResult> LaunchModeAsync(ModeConfig mode, CancellationToken token, IProgress<LaunchProgress>? progress = null)
    {
        var result = new LaunchResult { TotalTargets = mode.Targets.Count };
        int completed = 0;

        foreach (var target in mode.Targets)
        {
            if (token.IsCancellationRequested)
            {
                result.Canceled = true;
                break;
            }

                if (!LaunchService.TryPrepare(target, out var psi, out var error))
                {
                    result.Failures.Add(target.DisplayLabel);
                    _log.Write("VALIDATION FAIL: {0} – {1}", target.DisplayLabel, error);
                    completed++;
                    progress?.Report(new LaunchProgress { CompletedTargets = completed, TotalTargets = result.TotalTargets, CurrentLabel = target.DisplayLabel });
                    continue;
                }

                try
                {
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        result.LaunchedTargets++;
                        _log.Write("OK: {0}", target.DisplayLabel);
                    }
                    else
                    {
                        // Process.Start returned null – treat as a failure to launch.
                        result.Failures.Add(target.DisplayLabel);
                        _log.Write("FAIL (no proc): {0}", target.DisplayLabel);
                    }
                }
                catch (Exception ex)
                {
                    result.Failures.Add(target.DisplayLabel);
                    _log.Write("FAIL: {0} \n{1}", target.DisplayLabel, ex);
                }

            completed++;
            progress?.Report(new LaunchProgress { CompletedTargets = completed, TotalTargets = result.TotalTargets, CurrentLabel = target.DisplayLabel });
            // Yield to keep UI responsive; no artificial delay.
            await Task.Yield();
        }

        return result;
    }

}

public sealed class LaunchResult
{
    public int LaunchedTargets { get; set; }
    public int TotalTargets { get; set; }
    public List<string> Failures { get; } = new();
    public bool Canceled { get; set; }
    public bool Success => !Canceled && Failures.Count == 0 && LaunchedTargets > 0;
}

public sealed class LaunchProgress
{
    public int CompletedTargets { get; set; }
    public int TotalTargets { get; set; }
    public string? CurrentLabel { get; set; }
}