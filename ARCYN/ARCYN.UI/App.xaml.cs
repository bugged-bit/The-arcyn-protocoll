using System.IO;
using System.Linq;
using System.Windows;
using ARCYN.UI.Services;

namespace ARCYN.UI;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "arcyn_launch.log");

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            try
            {
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] FATAL: {ex?.Message}{Environment.NewLine}{ex?.StackTrace}{Environment.NewLine}");
            }
            catch { }
        };

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                File.AppendAllText(logPath,
                    $"[{DateTime.Now:HH:mm:ss.fff}] DISPATCHER: {args.Exception.Message}{Environment.NewLine}{args.Exception.StackTrace}{Environment.NewLine}");
            }
            catch { }

            System.Windows.MessageBox.Show(
                $"ARCYN encountered an unexpected error.\n\n{args.Exception.Message}",
                "ARCYN",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);

            args.Handled = true;
            Current.Shutdown(-1);
        };

        var consoleWnd = NativeMethods.GetConsoleWindow();
        if (consoleWnd != IntPtr.Zero)
            NativeMethods.ShowWindowAsync(consoleWnd, NativeMethods.SW_HIDE);

        // CLI flags
        var cliArgs = e.Args;
        if (cliArgs.Contains("--help"))
        {
            ShowHelp();
            return;
        }
        if (cliArgs.Contains("--version"))
        {
            System.Console.WriteLine("ARCYN v1.0.0");
            System.Console.WriteLine("Tactical workspace launcher");
            System.Console.WriteLine("MIT License - https://github.com/bugged-bit/ARCYN");
            return;
        }
        if (cliArgs.Contains("--config-path"))
        {
            var cfg = new ConfigService();
            var path = cfg.ResolvePath();
            System.Console.WriteLine(path ?? "No config found");
            return;
        }
        if (cliArgs.Contains("--setup"))
        {
            RunCliSetup();
            return;
        }

        // Detect first-run: no config exists
        try
        {
            var config = new ConfigService();
            var existing = config.Load();

            if (existing == null)
            {
                LogService.WriteStatic("First-run detected — launching setup wizard");
                var setup = new SetupWindow();
                setup.Show();
            }
            else
            {
                var main = new MainWindow();
                main.Show();
            }
        }
        catch (Exception ex)
        {
            LogService.WriteStatic("Startup error: {0}", ex.Message);
            System.Windows.MessageBox.Show(
                $"ARCYN encountered an error during startup.\n\n{ex.Message}",
                "ARCYN",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Current.Shutdown(-1);
        }
    }

    private static void ShowHelp()
    {
        System.Console.WriteLine("ARCYN v1.0.0 — Tactical workspace launcher");
        System.Console.WriteLine();
        System.Console.WriteLine("Usage: ARCYN [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("Options:");
        System.Console.WriteLine("  --help         Show this help");
        System.Console.WriteLine("  --version      Show version info");
        System.Console.WriteLine("  --config-path  Show resolved config path");
        System.Console.WriteLine("  --setup        Run CLI setup wizard");
    }

    private static void RunCliSetup()
    {
        // Headless CLI setup for terminal users
        System.Console.WriteLine("=== ARCYN Setup (CLI) ===");

        var config = new Services.ConfigService();
        var arcynConfig = new Models.ArcynConfig();

        System.Console.WriteLine("Create your workspace modes.");
        System.Console.WriteLine("Press Enter with empty name to finish.\n");

        while (true)
        {
            System.Console.Write($"Mode {arcynConfig.Modes.Count + 1} name (e.g. CODE): ");
            var name = System.Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                if (arcynConfig.Modes.Count == 0)
                {
                    System.Console.WriteLine("Need at least one mode.");
                    continue;
                }
                break;
            }

            var mode = new Models.ModeConfig { Name = name };

            System.Console.Write("  Description: ");
            mode.Description = System.Console.ReadLine()?.Trim() ?? "";

            System.Console.Write("  Apps (comma-separated, e.g. wt.exe,code): ");
            var apps = System.Console.ReadLine() ?? "";
            foreach (var a in apps.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                mode.Apps.Add(a);

            System.Console.Write("  Folders (comma-separated): ");
            var folders = System.Console.ReadLine() ?? "";
            foreach (var f in folders.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                mode.Folders.Add(f);

            System.Console.Write("  Websites (comma-separated, e.g. https://github.com): ");
            var sites = System.Console.ReadLine() ?? "";
            foreach (var s in sites.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                mode.Websites.Add(s);

            arcynConfig.Modes.Add(mode);
            System.Console.WriteLine($"  ✓ Mode '{name}' added\n");
        }

        for (int i = 0; i < arcynConfig.Modes.Count; i++)
            arcynConfig.Modes[i].Index = i + 1;

        config.Save(arcynConfig);
        System.Console.WriteLine($"Config saved with {arcynConfig.Modes.Count} modes.");
        System.Console.WriteLine("Run ARCYN without --setup to launch the HUD.");
    }
}
