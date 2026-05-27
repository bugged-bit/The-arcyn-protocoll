using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ARCYN.UI.Models;

namespace ARCYN.UI.Services;

public sealed class LaunchOrchestrator
{
    private readonly LogService _log;
    private readonly Func<TargetItem, ProcessStartInfo?> _processInfoFactory;

    public LaunchOrchestrator(LogService log, Func<TargetItem, ProcessStartInfo?>? processInfoFactory = null)
    {
        _log = log;
        _processInfoFactory = processInfoFactory ?? DefaultProcessInfoFactory;
    }

    private static ProcessStartInfo? DefaultProcessInfoFactory(TargetItem target)
    {
        if (target.Kind == TargetKind.Website)
            return new ProcessStartInfo(target.LaunchCmd ?? string.Empty) { UseShellExecute = true };

        if (target.Kind == TargetKind.Folder)
        {
            _ = target.LaunchArg ?? string.Empty;
            return null; // folder launch not supported via fallback
        }

        var cmd = target.LaunchCmd ?? string.Empty;
        return new ProcessStartInfo(cmd) { UseShellExecute = true };
    }

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

            ProcessStartInfo? psi;
            string? error = null;

            try
            {
                psi = _processInfoFactory(target);
                if (psi == null)
                    error = "Unsupported target kind for fallback launcher.";
            }
            catch (Exception ex)
            {
                psi = null;
                error = ex.Message;
            }

            if (psi == null)
            {
                result.Failures.Add(target.DisplayLabel);
                _log.Write("VALIDATION FAIL: {0} – {1}", target.DisplayLabel, error ?? "unknown");
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
