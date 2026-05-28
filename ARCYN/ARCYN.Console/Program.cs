using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ARCYN.Core.Models;
using ARCYN.Core.Services;
using ARCYN.Platform;
using ARCYN.UI.Services;

namespace ARCYN.Console;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("--- ARCYN headless test harness ---");

        // 1. Build a sample config in memory
        var sampleMode = new ModeConfig
        {
            Name = "TEST",
            Description = "Sample mode for testing",
            Accent = "#FFAA00",
            Apps = ["dummyapp.exe"],
            Websites = ["https://example.com"],
            Folders = [Environment.CurrentDirectory]
        };

        var config = new ArcynConfig
        {
            Modes = [sampleMode],
            Theme = new ThemeConfig(),
            Behavior = new BehaviorConfig()
        };

        var configService = new ConfigService(new ConfigPathProvider());
        // Save asynchronously (creates file in appropriate location)
        await configService.SaveAsync(config);
        System.Console.WriteLine("Config saved.");

        // 2. Load the config back
        var loaded = configService.Load();
        if (loaded == null)
        {
            System.Console.WriteLine("Failed to load config.");
            return;
        }
        System.Console.WriteLine($"Loaded config with {loaded.Modes.Count} mode(s).");

        // 3. For each mode, prepare launch info for all targets
        foreach (var mode in loaded.Modes)
        {
            System.Console.WriteLine($"Mode: {mode.Name} – {mode.Description}");
            foreach (var target in mode.Targets)
            {
                System.Console.WriteLine($"  Target: {target.Kind} – {target.DisplayLabel}");
                if (LaunchService.TryPrepare(target, out var psi, out var error))
                {
                    System.Console.WriteLine($"    Prepared: FileName='{psi.FileName}' Arguments='{psi.Arguments}' WorkingDir='{psi.WorkingDirectory}'");
                }
                else
                {
                    System.Console.WriteLine($"    Validation failed: {error}");
                }
            }
        }

        System.Console.WriteLine("--- End of test harness ---");
    }
}
