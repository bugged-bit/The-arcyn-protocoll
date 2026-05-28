using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ARCYN.Core.Services;
using ARCYN.Platform;

namespace ARCYN.Avalonia;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var cfg = new ConfigService(new ConfigPathProvider());
            var config = cfg.Load();

            if (config == null || config.Modes.Count == 0)
            {
                LogService.WriteStatic("First-run — no config found. Run with --setup to create modes.");
                Console.Error.WriteLine("ARCYN: No configuration found. Use --setup to create your first mode.");
            }
            else
            {
                LogService.WriteStatic("Config loaded ({0} modes).", config.Modes.Count);
            }

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
