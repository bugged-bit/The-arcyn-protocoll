using Avalonia;
using System;

namespace ARCYN.Avalonia;

class Program
{
    // Initialization code. Do not use any Avalonia-specific code before AppBuilder.
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
