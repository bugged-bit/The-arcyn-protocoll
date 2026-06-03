using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ARCYN.Avalonia.Services;

[DBusInterface("com.github.arczyn.Arcyn")]
public interface IArcynDbus : IDBusObject
{
    Task ShowAsync();
    Task QuitAsync();
}

public class ArcynDbusService : IArcynDbus
{
    public const string ServiceName = "com.github.arczyn.Arcyn";
    public static readonly ObjectPath Path = new("/com/github/arczyn/Arcyn");

    public ObjectPath ObjectPath => Path;

    public async Task ShowAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var lifetime = Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime;

            var window = lifetime?.MainWindow;
            if (window == null)
                return;

            window.Show();
            window.Activate();

            window.Topmost = true;
            window.Topmost = false;
        });
    }

    public async Task QuitAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var lifetime = Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime;

            var window = lifetime?.MainWindow;
            if (window != null)
                window.Close();
        });
    }
}
