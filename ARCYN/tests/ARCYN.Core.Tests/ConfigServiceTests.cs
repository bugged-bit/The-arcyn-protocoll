using ARCYN.Core.Models;
using ARCYN.Core.Services;

namespace ARCYN.Core.Tests;

public sealed class ConfigServiceTests : IDisposable
{
    private readonly string _configPath;

    public ConfigServiceTests()
    {
        _configPath = Path.Combine(AppContext.BaseDirectory, "arcyn.json");
    }

    [Fact]
    public void Load_ValidDefaultConfig_ReturnsConfigWithExpectedDefaults()
    {
        // Arrange
        var json = """
        {
          "theme": {
            "accent": "#D64545",
            "glow_opacity": 0.28,
            "scanlines": true,
            "animations": true
          },
          "behavior": {
            "idle_timeout_seconds": 10,
            "always_on_top": true,
            "close_on_launch": true
          },
          "modes": [
            {
              "name": "TEST",
              "description": "Test mode",
              "accent": "#D64545",
              "apps": ["code"],
              "websites": [],
              "folders": []
            }
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
            Assert.NotNull(config.Theme);
            Assert.Equal("#D64545", config.Theme.Accent);
            Assert.Equal(0.28, config.Theme.GlowOpacity);
            Assert.True(config.Theme.Scanlines);
            Assert.True(config.Theme.Animations);
            Assert.NotNull(config.Behavior);
            Assert.Equal(10, config.Behavior.IdleTimeoutSeconds);
            Assert.True(config.Behavior.AlwaysOnTop);
            Assert.True(config.Behavior.CloseOnLaunch);
            Assert.Single(config.Modes);
            Assert.Equal("TEST", config.Modes[0].Name);
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
