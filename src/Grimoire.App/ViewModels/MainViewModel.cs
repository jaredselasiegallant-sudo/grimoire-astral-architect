using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grimoire.Core.Enums;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;
using Grimoire.Engine.Input;

namespace Grimoire.App.ViewModels;

/// <summary>
/// Root ViewModel for the main game screen.
/// Owns sub-ViewModels and coordinates the game canvas, gesture input, and spell casting.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IGameStateService _stateService;

    [ObservableProperty] private InventoryViewModel _inventory = new();
    [ObservableProperty] private FamiliarManagementViewModel _familiarManagement = new();
    [ObservableProperty] private CraftingCauldronViewModel _craftingCauldron = new();

    [ObservableProperty] private string _playerName = "Architect";
    [ObservableProperty] private int _manaCrystals;
    [ObservableProperty] private string _currentSpellStatus = "Draw a shape to cast a spell";
    [ObservableProperty] private string _cooldownDisplay = "";
    [ObservableProperty] private string _lastExpeditionLog = "No expeditions yet";
    [ObservableProperty] private bool _isStrokeActive;

    public MainViewModel(IGameStateService stateService)
    {
        _stateService = stateService;
        RefreshFromState();
    }

    // ─── State sync ──────────────────────────────────────────────

    /// <summary>Refreshes bound properties from the current game state.</summary>
    public void RefreshFromState()
    {
        if (!_stateService.IsInitialised) return;

        var state = _stateService.CurrentState;
        PlayerName = state.PlayerName;
        ManaCrystals = state.ManaCrystals;

        // Load last expedition log entry
        if (state.ExpeditionLog.Count > 0)
        {
            var last = state.ExpeditionLog[^1];
            LastExpeditionLog = $"[{last.ReturnedUTC:MMM dd HH:mm}] {last.FamiliarName}: +{last.ManaCrystalsEarned} mana, {last.ItemNames.Count} items";
        }
    }

    /// <summary>Called every frame to update cooldown UI text.</summary>
    public void UpdateCooldownDisplay(IGameStateService stateService)
    {
        ManaCrystals = stateService.CurrentState.ManaCrystals;

        var onCooldown = Enum.GetValues<SpellGesture>()
            .Where(g => g != SpellGesture.Unknown)
            .Where(g => !stateService.IsSpellReady(g))
            .ToList();

        if (onCooldown.Count == 0)
        {
            CooldownDisplay = "";
        }
        else
        {
            var remaining = stateService.GetSpellCooldownRemaining(onCooldown[0]);
            CooldownDisplay = $"⏳ {remaining:mm\\:ss}";
        }
    }

    // ─── Spell casting ───────────────────────────────────────────

    [RelayCommand]
    private void CastSpell(string gestureName)
    {
        if (!Enum.TryParse<SpellGesture>(gestureName, out var gesture)) return;

        var state = _stateService.CurrentState;
        var power = gesture switch
        {
            SpellGesture.Circle => 10,
            SpellGesture.Triangle => 20,
            SpellGesture.Line => 5,
            SpellGesture.Zigzag => 15,
            SpellGesture.Spiral => 30,
            _ => 0
        };

        if (state.ManaCrystals < power)
        {
            CurrentSpellStatus = $"Not enough mana for {gesture} (need {power})";
            return;
        }

        if (!_stateService.IsSpellReady(gesture))
        {
            var remaining = _stateService.GetSpellCooldownRemaining(gesture);
            CurrentSpellStatus = $"Spell on cooldown — wait {remaining:mm\\:ss}";
            return;
        }

        state.ManaCrystals -= power;
        _stateService.PutSpellOnCooldown(gesture, TimeSpan.FromSeconds(10));

        CurrentSpellStatus = gesture switch
        {
            SpellGesture.Circle => "Circle of Warding cast — protective barrier active",
            SpellGesture.Triangle => "Triangle of Binding cast — essence trapped",
            SpellGesture.Line => "Line of Division cast — obstacles cleaved",
            SpellGesture.Zigzag => "Zigzag of Disruption cast — area scattered",
            SpellGesture.Spiral => "Spiral of Unravelling cast — hidden loot revealed",
            _ => "Spell cast"
        };
    }

    // ─── Save ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveGameAsync()
    {
        await _stateService.SaveAsync();
        CurrentSpellStatus = "Game saved.";
    }
}
