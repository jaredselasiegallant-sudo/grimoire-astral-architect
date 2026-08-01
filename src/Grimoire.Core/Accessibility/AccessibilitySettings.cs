using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.Core.Accessibility;

/// <summary>
/// Accessibility settings and helpers. Every feature here is designed
/// as a core pillar, not an afterthought. Award juries (IGF, AGA, BAFTA)
/// explicitly score accessibility.
/// </summary>
public sealed class AccessibilitySettings
{
    /// <summary>Gesture assist mode (Off, Simplified, AutoComplete).</summary>
    public GestureAssistMode GestureAssist { get; set; } = GestureAssistMode.Off;

    /// <summary>Colour blind mode for UI elements and glow palettes.</summary>
    public ColorBlindMode ColorBlindMode { get; set; } = ColorBlindMode.Normal;

    /// <summary>Whether narration text is spoken via TTS.</summary>
    public bool TextToSpeechEnabled { get; set; }

    /// <summary>Idle timer pacing multiplier. Players who can't check in often get
    /// slower expedition timers and gentler decay.</summary>
    public float IdlePacingMultiplier { get; set; } = 1.0f;

    /// <summary>High contrast mode — thicker outlines, brighter glows.</summary>
    public bool HighContrastMode { get; set; }

    /// <summary>Reduced motion — fewer particles, simpler animations.</summary>
    public bool ReducedMotion { get; set; }

    /// <summary>Subtitle size multiplier for narration text.</summary>
    public float SubtitleScale { get; set; } = 1.0f;

    /// <summary>Whether gesture trail is always visible (helps with tracing).</summary>
    public bool PersistentTrail { get; set; }
}

/// <summary>
/// Provides colour-blind-safe palette mappings.
/// </summary>
public static class AccessibilityPalette
{
    private static readonly Dictionary<string, Dictionary<ColorBlindMode, string>> Palettes = new()
    {
        ["mana"] = new() { [ColorBlindMode.Normal] = "#64C8FF", [ColorBlindMode.Protanopia] = "#6488FF", [ColorBlindMode.Deuteranopia] = "#6488FF", [ColorBlindMode.Tritanopia] = "#FF6488" },
        ["void"] = new() { [ColorBlindMode.Normal] = "#A064FF", [ColorBlindMode.Protanopia] = "#6488FF", [ColorBlindMode.Deuteranopia] = "#8864FF", [ColorBlindMode.Tritanopia] = "#FF64A0" },
        ["ember"] = new() { [ColorBlindMode.Normal] = "#FF6A3D", [ColorBlindMode.Protanopia] = "#FFB03D", [ColorBlindMode.Deuteranopia] = "#FFB03D", [ColorBlindMode.Tritanopia] = "#FF3D6A" },
        ["frost"] = new() { [ColorBlindMode.Normal] = "#7DD4FF", [ColorBlindMode.Protanopia] = "#7D88FF", [ColorBlindMode.Deuteranopia] = "#7D88FF", [ColorBlindMode.Tritanopia] = "#FF7DD4" },
        ["verdant"] = new() { [ColorBlindMode.Normal] = "#64FF8A", [ColorBlindMode.Protanopia] = "#88FFB0", [ColorBlindMode.Deuteranopia] = "#88FFB0", [ColorBlindMode.Tritanopia] = "#FFB064" },
        ["luminous"] = new() { [ColorBlindMode.Normal] = "#FFE864", [ColorBlindMode.Protanopia] = "#FFE888", [ColorBlindMode.Deuteranopia] = "#FFE888", [ColorBlindMode.Tritanopia] = "#64FFE8" },
        ["umbral"] = new() { [ColorBlindMode.Normal] = "#6A3DFF", [ColorBlindMode.Protanopia] = "#3D6AFF", [ColorBlindMode.Deuteranopia] = "#6A3DFF", [ColorBlindMode.Tritanopia] = "#FF6A3D" },
    };

    public static string GetColour(string element, ColorBlindMode mode)
    {
        if (Palettes.TryGetValue(element, out var palette) && palette.TryGetValue(mode, out var colour))
            return colour;
        return "#FFFFFF";
    }

    public static int GetSKColourAsInt(string element, ColorBlindMode mode)
    {
        var hex = GetColour(element, mode);
        return Convert.ToInt32(hex.Replace("#", ""), 16);
    }
}
