using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ARCYN.UI;

public sealed class ParticleEngine : IDisposable
{
    private readonly Canvas _canvas;
    private readonly Random _rng = new();
    private readonly List<Particle> _particles = [];
    private readonly Stack<Ellipse> _pool = [];
    private const int Count = 60;
    private bool _running;
    private bool _disposed;
    private double _w, _h;

    private sealed class Particle
    {
        public Ellipse? Shape;
        public double Vx, Vy, Life, MaxLife, DriftPhase, DriftAmp;
        public bool Alive;
    }

    public ParticleEngine(Canvas canvas) => _canvas = canvas;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _w = Math.Max(_canvas.ActualWidth, 800);
        _h = Math.Max(_canvas.ActualHeight, 480);

        for (int i = 0; i < Count; i++)
        {
            var p = new Particle
            {
                Shape = RentShape(),
                Vx = (_rng.NextDouble() - 0.5) * 1.0,
                Vy = -(_rng.NextDouble() * 0.6 + 0.15),
                MaxLife = _rng.Next(120, 350),
                Life = _rng.Next(-60, 0),
                DriftPhase = _rng.NextDouble() * Math.PI * 2,
                DriftAmp = _rng.NextDouble() * 0.5 + 0.15,
                Alive = true
            };
            Canvas.SetLeft(p.Shape, _rng.NextDouble() * 800);
            Canvas.SetTop(p.Shape, _rng.NextDouble() * 480);
            _canvas.Children.Add(p.Shape);
            _particles.Add(p);
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        foreach (var p in _particles)
        {
            if (p.Shape != null)
            {
                _canvas.Children.Remove(p.Shape);
                ReturnShape(p.Shape);
                p.Shape = null;
            }
            p.Alive = false;
        }
        _particles.Clear();
    }

    public void Tick()
    {
        if (!_running || _disposed || !_canvas.IsVisible) return;
        _w = Math.Max(_canvas.ActualWidth, 800);
        _h = Math.Max(_canvas.ActualHeight, 480);

        for (int i = 0; i < _particles.Count; i++)
        {
            var p = _particles[i];
            if (!p.Alive) continue;

            p.Life++;
            if (p.Life < 0) continue;

            var ratio = p.Life / p.MaxLife;
            if (ratio > 1) ratio = 1;
            var shape = p.Shape;
            if (shape == null) continue;

            shape.Opacity = ratio < 0.08 ? ratio / 0.08 :
                            ratio > 0.75 ? (1 - ratio) / 0.25 : 1;

            var x = Canvas.GetLeft(shape) + p.Vx;
            var y = Canvas.GetTop(shape) + p.Vy;
            x += Math.Sin(p.Life * 0.02 + p.DriftPhase) * p.DriftAmp;

            if (y < -10 || x < -10 || x > _w + 10 || p.Life > p.MaxLife)
            {
                Canvas.SetLeft(shape, _rng.NextDouble() * _w);
                Canvas.SetTop(shape, _h + 5);
                p.Vx = (_rng.NextDouble() - 0.5) * 0.8;
                p.Vy = -(_rng.NextDouble() * 0.5 + 0.12);
                p.Life = 0;
                p.MaxLife = _rng.Next(120, 350);
                continue;
            }

            Canvas.SetLeft(shape, x);
            Canvas.SetTop(shape, y);
        }
    }

    private Ellipse RentShape()
    {
        if (_pool.Count > 0) return _pool.Pop();
        return new Ellipse
        {
            Width = _rng.Next(2, 5),
            Height = _rng.Next(2, 5),
            IsHitTestVisible = false,
            Fill = new SolidColorBrush(Color.FromArgb(
                (byte)_rng.Next(60, 210), 0xE8, 0x60, 0x60))
        };
    }

    private void ReturnShape(Ellipse e) => _pool.Push(e);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _pool.Clear();
        GC.SuppressFinalize(this);
    }
}
