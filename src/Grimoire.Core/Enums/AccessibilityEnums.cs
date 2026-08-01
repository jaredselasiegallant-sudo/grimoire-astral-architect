namespace Grimoire.Core.Enums;

/// <summary>Accessibility mode options.</summary>
public enum GestureAssistMode
{
    Off,
    Simplified,   // Larger hit targets, slower recognition window
    AutoComplete  // Draw any circle/triangle-ish shape and it snaps
}

/// <summary>Colour blind palette options.</summary>
public enum ColorBlindMode
{
    Normal,
    Protanopia,   // Red-green (red weak)
    Deuteranopia, // Red-green (green weak)
    Tritanopia    // Blue-yellow
}
