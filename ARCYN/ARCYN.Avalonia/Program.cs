using Avalonia;
using System;
using System.Linq;
using System.Reflection;

namespace ARCYN.Avalonia;

class Program
{
    // Initialization code. Do not use any Avalonia-specific code before AppBuilder.
    public static void Main(string[] args)
    {
        if (args.Contains("--version"))
        {
            var ver = Assembly.GetEntryAssembly()?.GetName()?.Version;
            Console.WriteLine(ver is not null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v0.0.0");
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
