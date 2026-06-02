using ARCYN.Core.Models;

namespace ARCYN.Core.Tests;

public sealed class ShortcutParsingTests
{
    [Theory]
    [InlineData("Ctrl+Alt+1")]
    [InlineData("ctrl+shift+F5")]
    [InlineData("Super+K")]
    [InlineData("F12")]
    [InlineData("Escape")]
    [InlineData("Ctrl+1")]
    [InlineData("Ctrl+Alt+Shift+F11")]
    [InlineData("Super+ctrl+alt+shift+a")]
    [InlineData("alt+space")]
    [InlineData("PageUp")]
    public void TryParse_ValidCombo_ReturnsTrue(string text)
    {
        Assert.True(KeyCombo.TryParse(text, out var combo));
        Assert.NotEqual(LogicalKey.None, combo.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("Ctrl+")]
    [InlineData("1+Ctrl")]
    [InlineData("Foo+Bar")]
    [InlineData("Ctrl+ZZZ")]
    [InlineData("Ctrl+Alt+Ctrl+1")]
    [InlineData("F0")]
    [InlineData("F25")]
    [InlineData("Modifier+1")]
    [InlineData("++")]
    [InlineData("Ctrl++")]
    public void TryParse_InvalidCombo_ReturnsFalse(string? text)
    {
        Assert.False(KeyCombo.TryParse(text, out _));
    }

    [Fact]
    public void TryParse_NormalizesDisplay()
    {
        Assert.True(KeyCombo.TryParse("ctrl+alt+1", out var combo));
        Assert.Equal("Ctrl+Alt+1", combo.ToString());
    }

    [Fact]
    public void TryParse_UppercasesLetter()
    {
        Assert.True(KeyCombo.TryParse("ctrl+shift+k", out var combo));
        Assert.Equal("Ctrl+Shift+K", combo.ToString());
    }

    [Fact]
    public void TryParse_SuperAlias_Works()
    {
        Assert.True(KeyCombo.TryParse("Win+K", out var combo));
        Assert.Equal(KeyModifiers.Super, combo.Modifiers);
        Assert.Equal(LogicalKey.K, combo.Key);
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        Assert.False(KeyCombo.TryParse(string.Empty, out _));
    }

    [Fact]
    public void TryParse_RejectsDuplicateModifier()
    {
        Assert.False(KeyCombo.TryParse("Ctrl+Ctrl+1", out _));
    }
}
