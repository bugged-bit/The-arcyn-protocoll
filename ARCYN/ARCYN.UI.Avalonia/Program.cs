using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace ARCYN.UI.Avalonia;

class Program
{
    // Avalonia entry point
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}