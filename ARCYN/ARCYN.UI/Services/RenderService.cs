using System.Diagnostics;

namespace ARCYN.UI.Services;

public sealed class RenderService : IDisposable
{
    public interface ISubscriber
    {
        void OnRenderTick(long dtMs);
    }

    private readonly List<ISubscriber> _subscribers = [];
    private long _lastStamp;
    private bool _running;
    private bool _disposed;

    public void Subscribe(ISubscriber subscriber) => _subscribers.Add(subscriber);

    public void Unsubscribe(ISubscriber subscriber) => _subscribers.Remove(subscriber);

    public void Start()
    {
        if (_running) return;
        _running = true;
        _lastStamp = 0;
        CompositionTargetEx.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        CompositionTargetEx.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_disposed) return;

        var now = Stopwatch.GetTimestamp();
        if (_lastStamp == 0) { _lastStamp = now; return; }

        var dtMs = (now - _lastStamp) * 1000.0 / Stopwatch.Frequency;
        _lastStamp = now;

        var dt = (long)dtMs;
        if (dt < 1) dt = 1;
        if (dt > 100) dt = 100;

        // Iterate directly over the subscriber list to avoid per‑frame array allocation.
        // Since subscribers are only added/removed during startup or shutdown, a simple snapshot of the count is safe.
        var subs = _subscribers;
        int count = subs.Count;
        for (int i = 0; i < count; i++)
            subs[i].OnRenderTick(dt);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _subscribers.Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>Wraps CompositionTarget.Rendering to avoid raw event pattern.</summary>
    private static class CompositionTargetEx
    {
        public static event EventHandler? Rendering
        {
            add => System.Windows.Media.CompositionTarget.Rendering += value;
            remove => System.Windows.Media.CompositionTarget.Rendering -= value;
        }
    }
}
