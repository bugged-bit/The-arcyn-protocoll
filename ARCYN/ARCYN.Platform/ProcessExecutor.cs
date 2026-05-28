using System;
using System.Diagnostics;
using ARCYN.Core.Services;

namespace ARCYN.Platform;

/// <summary>
/// Simple helper to start a process with optional timeout and log results.
/// Used by UI layers to launch targets while capturing exit codes and failures.
/// </summary>
public static class ProcessExecutor
{
    /// <summary>
    /// Starts the process defined by <paramref name="psi"/>.
    /// If <paramref name="timeoutMs"/> > 0, waits that many milliseconds for exit; otherwise waits indefinitely.
    /// Logs success, exit code, timeout, and any exception.
    /// Returns <c>true</c> if the process started (and optionally exited) without throwing.
    /// </summary>
    public static bool TryExecute(ProcessStartInfo psi, int timeoutMs, out int exitCode, out string? error)
    {
        exitCode = -1;
        error = null;
        try
        {
            using var proc = new Process { StartInfo = psi };
            bool started = proc.Start();
            if (!started)
            {
                error = "Process.Start returned false";
                LogService.WriteStatic("Launch failed – {0}", error);
                return false;
            }

            if (timeoutMs > 0)
            {
                bool exited = proc.WaitForExit(timeoutMs);
                if (!exited)
                {
                    error = $"Process timed out after {timeoutMs}ms";
                    LogService.WriteStatic("Launch timeout – {0}", error);
                    try { proc.Kill(); } catch { }
                    return false;
                }
            }
            else
            {
                proc.WaitForExit();
            }

            exitCode = proc.ExitCode;
            LogService.WriteStatic("Process exited with code {0}", exitCode);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            LogService.WriteStatic("Process execution exception: {0}", ex);
            return false;
        }
    }
}
