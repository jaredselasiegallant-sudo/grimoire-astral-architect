using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.App.ViewModels;

/// <summary>
/// Controls the Alchemical Cauldron crafting panel.
/// Players combine inventory items to craft higher-tier resources and potions.
/// </summary>
public partial class CraftingCauldronViewModel : ObservableObject
{
    [ObservableProperty] private InventoryItem? _slotA;
    [ObservableProperty] private InventoryItem? _slotB;
    [ObservableProperty] private string _craftingResult = "Place two items in the cauldron to craft";
    [ObservableProperty] private bool _canCraft;

    public ObservableCollection<InventoryItem> AvailableItems { get; } = [];

    /// <summary>Populate available items from the player's inventory.</summary>
    public void LoadFrom(List<InventoryItem> items)
    {
        AvailableItems.Clear();
        foreach (var item in items.Where(i => i.Quantity > 0))
            AvailableItems.Add(item);
    }

    [RelayCommand]
    private void SetSlotA(InventoryItem? item)
    {
        SlotA = item;
        UpdateCanCraft();
    }

    [RelayCommand]
    private void SetSlotB(InventoryItem? item)
    {
        SlotB = item;
        UpdateCanCraft();
    }

    [RelayCommand]
    private void Craft()
    {
        if (SlotA is null || SlotB is null) return;

        // Determine the crafting result based on element combination
        var result = ResolveCrafting(SlotA, SlotB);

        // Consume ingredients
        SlotA.Quantity--;
        SlotB.Quantity--;

        if (SlotA.Quantity <= 0) AvailableItems.Remove(SlotA);
        if (SlotB.Quantity <= 0) AvailableItems.Remove(SlotB);

        CraftingResult = $"Crafted: {result.Name} ({result.Rarity})";
        AvailableItems.Add(result);

        SlotA = null;
        SlotB = null;
        UpdateCanCraft();
    }

    [RelayCommand]
    private void ClearSlots()
    {
        SlotA = null;
        SlotB = null;
        CraftingResult = "Place two items in the cauldron to craft";
        UpdateCanCraft();
    }

    private void UpdateCanCraft()
    {
        CanCraft = SlotA is not null && SlotB is not null;
    }

    /// <summary>
    /// Core alchemical recipe resolution.
    /// Same element = elemental refinement; different elements = hybrid creation.
    /// </summary>
    private static InventoryItem ResolveCrafting(InventoryItem a, InventoryItem b)
    {
        if (a.Element == b.Element)
        {
            // Same element: refine into a higher-tier version
            return new InventoryItem
            {
                Name = $"Refined {a.Element} Essence",
                Description = $"Concentrated essence of {a.Element}.",
                Element = a.Element,
                Rarity = GetUpgradedRarity(a.Rarity, b.Rarity),
                Quantity = 1,
                ManaPower = Math.Max(a.ManaPower, b.ManaPower) + 10
            };
        }
        else
        {
            // Different elements: hybrid creation
            var name = $"Harmonic Shard ({a.Element}+{b.Element})";
            return new InventoryItem
            {
                Name = name,
                Description = $"A rare fusion of {a.Element} and {b.Element} energies.",
                Element = a.Element, // primary element
                Rarity = GetUpgradedRarity(a.Rarity, b.Rarity),
                Quantity = 1,
                ManaPower = a.ManaPower + b.ManaPower + 5
            };
        }
    }

    private static Rarity GetUpgradedRarity(Rarity a, Rarity b)
    {
        var combined = Math.Max((int)a, (int)b);
        // 30% chance to upgrade one tier
        return Random.Shared.NextDouble() < 0.3 && combined < (int)Rarity.Legendary
            ? (Rarity)(combined + 1)
            : (Rarity)combined;
    }
}
