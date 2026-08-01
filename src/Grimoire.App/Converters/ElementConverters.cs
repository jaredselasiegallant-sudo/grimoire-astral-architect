using Microsoft.UI.Xaml.Data;
using Grimoire.Core.Enums;

namespace Grimoire.App.Converters;

/// <summary>
/// Converts an ElementType enum to a display-friendly colour hex string.
/// Used in XAML bindings for item rarity/element indicators.
/// </summary>
public sealed class ElementTypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ElementType element)
        {
            return element switch
            {
                ElementType.Mana => "#64C8FF",
                ElementType.Void => "#A064FF",
                ElementType.Ember => "#FF6A3D",
                ElementType.Frost => "#7DD4FF",
                ElementType.Verdant => "#64FF8A",
                ElementType.Luminous => "#FFE864",
                ElementType.Umbral => "#6A3DFF",
                _ => "#808080"
            };
        }
        return "#808080";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a Rarity enum to a display string with colour hint.
/// </summary>
public sealed class RarityToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => "Common",
                Rarity.Uncommon => "Uncommon",
                Rarity.Rare => "Rare",
                Rarity.Epic => "Epic",
                Rarity.Legendary => "★ Legendary",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
