using ARCYN.Core.Models;
using ARCYN.Core.Services;

namespace ARCYN.Core.Tests;

public sealed class BusinessLogicTests
{
    [Fact]
    public void Load_SetsModesWithCorrectState()
    {
        // Arrange
        var service = new ModeService();
        var modes = new List<ModeConfig>
        {
            new() { Name = "Dev", Description = "Development" },
            new() { Name = "Work", Description = "Work mode" },
        };

        // Act
        service.Load(modes);

        // Assert
        Assert.Equal(2, service.Count);
        Assert.Equal(-1, service.SelectedIndex);
        Assert.False(service.HasSelection);
        Assert.Equal(2, service.Modes.Count);

        // Verify modes are the same references and indexed correctly
        Assert.Same(modes[0], service.Modes[0]);
        Assert.Same(modes[1], service.Modes[1]);
        Assert.Equal(1, service.Modes[0].Index);
        Assert.Equal(2, service.Modes[1].Index);
    }

    [Fact]
    public void Load_AssignsIndicesAndPreservesShortcut()
    {
        // Arrange
        var service = new ModeService();
        var modes = new List<ModeConfig>
        {
            new() { Name = "Dev", Shortcut = "Ctrl+Alt+1" },
            new() { Name = "Work" }
        };

        // Act
        service.Load(modes);

        // Assert
        Assert.Equal("Dev", service.Modes[0].Name);
        Assert.Equal("Ctrl+Alt+1", service.Modes[0].Shortcut);
        Assert.Equal(1, service.Modes[0].Index);
        Assert.Equal("[2]", service.Modes[1].ShortcutHint);
    }

    [Fact]
    public void ShortcutHint_ReflectsModeCount()
    {
        var service = new ModeService();
        service.Load(new List<ModeConfig>
        {
            new() { Name = "A" },
            new() { Name = "B" },
            new() { Name = "C" }
        });
        Assert.Equal("[1-3]", service.ShortcutHint);
    }
}
