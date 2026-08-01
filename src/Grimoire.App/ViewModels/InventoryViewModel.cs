using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Core.Enums;
using Grimoire.Core.Models;

namespace Grimoire.App.ViewModels;

/// <summary>
/// Manages the player's inventory display and item selection.
/// Backed by the GameState.Inventory collection.
/// </summary>
public partial class InventoryViewModel : ObservableObject
{
    [ObservableProperty] private InventoryItem? _selectedItem;

    public ObservableCollection<InventoryItem> Items { get; } = [];

    /// <summary>Load items from the game state into the observable collection.</summary>
    public void LoadFrom(List<InventoryItem> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
    }

    [RelayCommand]
    private void SelectItem(InventoryItem? item)
    {
        SelectedItem = item;
    }

    /// <summary>Returns the total mana power of all inventory items (for spell fuel display).</summary>
    public int TotalManaPower => Items.Sum(i => i.ManaPower * i.Quantity);
}
