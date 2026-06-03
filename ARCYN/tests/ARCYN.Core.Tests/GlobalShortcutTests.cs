using ARCYN.Core.Models;
using ARCYN.Core.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ARCYN.Core.Tests;

public sealed class GlobalShortcutTests : IDisposable
{
    private readonly string _configPath;

    public GlobalShortcutTests()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "arcyn.json");
    }

    [Fact]
    public void Load_ValidGlobalShortcut_PersistedInConfig()
    {
        // Arrange
        var json = """
        {
          "behavior": { "global_shortcut": "Ctrl+Alt+A" },
          "modes": [
            { "name": "CODE", "apps": ["code"], "websites": [], "folders": [] }
          ]
        }
        """;
        File.WriteAllText(_configPath, json);

        try
        {
            var service = new ConfigService();

            // Act
            var config = service.Load();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Behavior);
            Assert.Equal("Ctrl+Alt+A", config.Behavior.GlobalShortcut);
        }
        finally
        {
            if (File.Exists(_configPath))
                File.Delete(_configPath);
        }
    }

    [Fact]
    public void Load_InvalidGlobalShortcut_Cleared()
    {
        // Arrange
        var json = """
        {
          "behavior": { "global_shortcut": "garbage+++" },
          "modes": [
            { "name": "CODE", "apps": ["code"], "websites": [], "folders": [] }
          ]
        }
        """;
        File.WriteAllText(_configPath, json);

        try
        {
            var service = new ConfigService();

            // Act
            var config = service.Load();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Behavior);
            Assert.Null(config.Behavior.GlobalShortcut);
        }
        finally
        {
            if (File.Exists(_configPath))
                File.Delete(_configPath);
        }
    }

    [Fact]
    public void Load_GlobalShortcutCollidingWithModeShortcut_Cleared()
    {
        // Arrange
        var json = """
        {
          "behavior": { "global_shortcut": "Ctrl+Alt+1" },
          "modes": [
            { "name": "CODE", "shortcut": "Ctrl+Alt+1", "apps": ["code"], "websites": [], "folders": [] }
          ]
        }
        """;
        File.WriteAllText(_configPath, json);

        try
        {
            var service = new ConfigService();

            // Act
            var config = service.Load();

            // Assert
            Assert.NotNull(config);
            Assert.NotNull(config.Behavior);
            Assert.Null(config.Behavior.GlobalShortcut);
        }
        finally
        {
            if (File.Exists(_configPath))
                File.Delete(_configPath);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_configPath))
            File.Delete(_configPath);
    }
}
