using Avalonia;
using System;
using System.Threading.Tasks;
using Tmds.DBus;
using ARCYN.Avalonia.Services;

namespace ARCYN.Avalonia;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        bool activateOnly = args.Contains("--activate") || args.Contains("--show");

        if (TryActivateOrRegisterAsync().GetAwaiter().GetResult())
            return;

        // --activate / --show: only focus existing instance, never start a new one.
        if (activateOnly)
            return;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static async Task<bool> TryActivateOrRegisterAsync()
    {
        try
        {
            var conn = Connection.Session;

            if (await conn.IsServiceActiveAsync(ArcynDbusService.ServiceName))
            {
                var proxy = conn.CreateProxy<IArcynDbus>(
                    ArcynDbusService.ServiceName,
                    ArcynDbusService.Path);
                await proxy.ShowAsync();
                return true;
            }

            await conn.RegisterServiceAsync(
                ArcynDbusService.ServiceName,
                ServiceRegistrationOptions.None);

            await conn.RegisterObjectAsync(new ArcynDbusService());
        }
        catch
        {
            // D-Bus unavailable — app runs without single-instance protection.
        }

        return false;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
