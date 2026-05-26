using System.Text.Json.Serialization;

namespace ARCYN.UI.Models;

public sealed class ArcynConfig
{
    [JsonPropertyName("theme")]
    public ThemeConfig Theme { get; set; } = new();

    [JsonPropertyName("behavior")]
    public BehaviorConfig Behavior { get; set; } = new();

    [JsonPropertyName("modes")]
    public List<ModeConfig> Modes { get; set; } = [];
}

public sealed class ThemeConfig
{
    [JsonPropertyName("accent")]
    public string Accent { get; set; } = "#D64545";

    [JsonPropertyName("glow_opacity")]
    public double GlowOpacity { get; set; } = 0.28;

    [JsonPropertyName("scanlines")]
    public bool Scanlines { get; set; } = true;

    [JsonPropertyName("animations")]
    public bool Animations { get; set; } = true;

    [JsonPropertyName("reduced_effects")]
    public bool ReducedEffects { get; set; }

    [JsonPropertyName("compact_mode")]
    public bool CompactMode { get; set; }
}

public sealed class BehaviorConfig
{
    [JsonPropertyName("idle_timeout_seconds")]
    public int IdleTimeoutSeconds { get; set; } = 10;

    [JsonPropertyName("always_on_top")]
    public bool AlwaysOnTop { get; set; } = true;

    [JsonPropertyName("close_on_launch")]
    public bool CloseOnLaunch { get; set; } = true;
}
