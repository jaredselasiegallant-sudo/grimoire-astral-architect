using Grimoire.Core.Enums;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Models;
using Grimoire.Core.Services;
using Grimoire.Core.Events;
using Grimoire.Core.Bonding;
using Grimoire.Core.Grimoire;
using Grimoire.Core.Decay;
using Grimoire.Core.Buildings;
using Microsoft.Extensions.Logging;

namespace Grimoire.App.Services;

/// <summary>
/// Central game state manager. Orchestrates all game systems:
/// save/load, idle rewards, mana regen, spell cooldowns, hatching,
/// corruption, astral events, bonding decay, and grimoire unlocking.
/// </summary>
public sealed class GameStateService : IGameStateService
{
    private readonly IGameRepository _repository;
    private readonly ILogger<GameStateService>? _logger;
    private GameState? _state;

    private readonly Dictionary<SpellGesture, DateTimeOffset> _spellCooldowns = [];
    private ComboTracker? _comboTracker;

    public GameState CurrentState => _state ?? throw new InvalidOperationException("Game state not initialised.");

    public bool IsInitialised => _state is not null;

    public GameStateService(IGameRepository repository, ILogger<GameStateService>? logger = null)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task InitialiseAsync()
    {
        _state = await _repository.LoadGameStateAsync() ?? new GameState();

        // Calculate offline rewards
        var rewards = CalculateOfflineRewards();
        ApplyRewards(rewards);

        // Mana regeneration
        var manaFromRegen = CalculateManaRegen();
        _state.ManaCrystals += manaFromRegen;

        // Corruption decay
        var elapsed = DateTimeOffset.UtcNow - _state.LastSaveUTC;
        var corruptionResult = _state.Corruption.CalculateDecay(elapsed);
        if (corruptionResult.IsVisuallyChanged)
            _logger?.LogInformation("Corruption visually changed: {Prev} -> {New}", corruptionResult.PreviousLevel, corruptionResult.NewLevel);

        // Familiar happiness decay (neglected familiars)
        foreach (var bond in _state.FamiliarBonds.Values)
        {
            var hoursSinceInteraction = bond.LastInteractionUTC.HasValue
                ? (DateTimeOffset.UtcNow - bond.LastInteractionUTC.Value).TotalHours
                : 999;
            if (hoursSinceInteraction > 6)
                bond.DecayHappiness(1);
        }

        // Refresh astral events
        _state.ActiveEvents = AstralEventScheduler.GetTodaysEvents();

        // Check egg hatching
        var hatchedEggs = CheckAndHatchEggs();
        _logger?.LogInformation("Initialised. Mana: {Mana}, Familiars: {Count}, Events: {Events}",
            _state.ManaCrystals, _state.Familiars.Count, _state.ActiveEvents.Count);

        _state.LastSaveUTC = DateTimeOffset.UtcNow;
    }

    public async Task SaveAsync()
    {
        if (_state is null) return;
        _state.LastSaveUTC = DateTimeOffset.UtcNow;
        await _repository.SaveGameStateAsync(_state);
    }

    // ─── Spell Cooldowns ─────────────────────────────────────────

    public bool IsSpellReady(SpellGesture gesture)
    {
        if (!_spellCooldowns.TryGetValue(gesture, out var readyAt)) return true;
        return DateTimeOffset.UtcNow >= readyAt;
    }

    public void PutSpellOnCooldown(SpellGesture gesture, TimeSpan cooldown)
    {
        _spellCooldowns[gesture] = DateTimeOffset.UtcNow + cooldown;
    }

    public TimeSpan GetSpellCooldownRemaining(SpellGesture gesture)
    {
        if (!_spellCooldowns.TryGetValue(gesture, out var readyAt)) return TimeSpan.Zero;
        var remaining = readyAt - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    // ─── Mana Regeneration ───────────────────────────────────────

    public int CalculateManaRegen()
    {
        if (_state is null) return 0;
        var elapsed = DateTimeOffset.UtcNow - _state.LastSaveUTC;
        return IdleRewardCalculator.CalculatePassiveMana(_state.Buildings, elapsed);
    }

    public void TickManaRegen(float deltaTimeSeconds)
    {
        if (_state is null) return;
        var baseMps = _state.Buildings
            .Where(b => b.Type == BuildingType.StarlightObelisk || b.Type == BuildingType.GardenOfWhispers)
            .Sum(b => b.ManaPerSecond * b.Level);

        // Apply synergy bonuses
        var synergizedMps = BuildingSynergyCalculator.CalculateTotalManaPerSecond(_state.Buildings);
        var effectiveMps = Math.Max(baseMps, synergizedMps);

        // Apply event multipliers
        foreach (var evt in _state.ActiveEvents.Where(e => e.IsActive))
        {
            if (evt.Multipliers.TryGetValue("ManaPerSecond", out var mult))
                effectiveMps *= mult;
            if (evt.Multipliers.TryGetValue("AllBonus", out var allMult))
                effectiveMps *= allMult;
        }

        _state.ManaCrystals += (int)(effectiveMps * deltaTimeSeconds);
    }

    // ─── Egg Hatching ────────────────────────────────────────────

    public List<FamiliarEgg> CheckAndHatchEggs()
    {
        if (_state is null) return [];
        var hatched = new List<FamiliarEgg>();

        foreach (var egg in _state.Eggs.Where(e => e.IsIncubating && !e.HasHatched))
        {
            if (egg.HatchReadyUTC is not null && DateTimeOffset.UtcNow >= egg.HatchReadyUTC.Value)
            {
                egg.HasHatched = true;
                egg.IsIncubating = false;
                hatched.Add(egg);

                var familiar = new Familiar
                {
                    Name = $"{egg.Element} Familiar",
                    Type = egg.HatchesInto,
                    Element = egg.Element,
                    Rarity = egg.Rarity,
                    Level = 1,
                    MaxHealth = 100,
                    CurrentHealth = 100,
                    GatheringBonus = 1.0 + (int)egg.Rarity * 0.1
                };
                familiar.InitialiseSpecializations();
                _state.Familiars.Add(familiar);

                // Create bond
                _state.FamiliarBonds[familiar.Id] = new FamiliarBond { FamiliarId = familiar.Id };

                // Unlock Grimoire entry
                var entryId = familiar.Type switch
                {
                    FamiliarType.Wisp => GrmoireEntryId.fam_wisp,
                    FamiliarType.Sprite => GrmoireEntryId.fam_sprite,
                    FamiliarType.Drakling => GrmoireEntryId.fam_drakling,
                    FamiliarType.Mothwing => GrmoireEntryId.fam_mothwing,
                    FamiliarType.Golem => GrmoireEntryId.fam_golem,
                    FamiliarType.Shade => GrmoireEntryId.fam_shade,
                    FamiliarType.Foxfire => GrmoireEntryId.fam_foxfire,
                    _ => GrmoireEntryId.fam_wisp
                };
                _state.Grimoire.Unlock(entryId);
                _logger?.LogInformation("Egg hatched: {Type} ({Element})", egg.HatchesInto, egg.Element);
            }
        }
        return hatched;
    }

    // ─── Expedition Rewards ──────────────────────────────────────

    public List<ExpeditionResult> CalculateOfflineRewards()
    {
        if (_state is null) return [];
        var now = DateTimeOffset.UtcNow;
        var results = new List<ExpeditionResult>();

        foreach (var familiar in _state.Familiars)
        {
            if (!familiar.IsOnExpedition || familiar.ExpeditionReturnUTC is null) continue;
            if (now < familiar.ExpeditionReturnUTC.Value) continue;

            var result = IdleRewardCalculator.Calculate(familiar, now);
            results.Add(result);
        }
        return results;
    }

    private void ApplyRewards(List<ExpeditionResult> rewards)
    {
        if (_state is null) return;

        foreach (var reward in rewards)
        {
            var familiar = _state.Familiars.FirstOrDefault(f => f.Id == reward.FamiliarId);
            if (familiar is null) continue;

            familiar.Experience += reward.ExperienceEarned;
            familiar.IsOnExpedition = false;
            familiar.ExpeditionReturnUTC = null;
            familiar.CurrentHealth = familiar.MaxHealth;

            while (familiar.Experience >= familiar.Level * 100)
            {
                familiar.Experience -= familiar.Level * 100;
                familiar.Level++;
                familiar.MaxHealth += 10;
                familiar.CurrentHealth = familiar.MaxHealth;
                familiar.GatheringBonus += 0.05;
            }

            _state.ManaCrystals += reward.ManaCrystalsEarned;
            _state.TotalExpeditionsCompleted++;

            foreach (var drop in reward.Loot)
            {
                var existing = _state.Inventory.FirstOrDefault(i =>
                    i.Name == drop.ItemName && i.Element == drop.Element);
                if (existing is not null)
                    existing.Quantity += drop.Quantity;
                else
                    _state.Inventory.Add(new InventoryItem
                    {
                        Name = drop.ItemName,
                        Description = "Gathered from an idle expedition.",
                        Element = drop.Element,
                        Rarity = drop.Rarity,
                        Quantity = drop.Quantity,
                        ManaPower = drop.Rarity switch
                        {
                            Rarity.Legendary => 50,
                            Rarity.Epic => 30,
                            Rarity.Rare => 15,
                            Rarity.Uncommon => 8,
                            _ => 3
                        }
                    });

                _state.ExpeditionLog.Add(new ExpeditionLogEntry
                {
                    FamiliarName = familiar.Name,
                    DepartedUTC = reward.DepartedUTC,
                    ReturnedUTC = reward.ReturnedUTC,
                    Duration = reward.Duration,
                    Success = reward.Success,
                    ManaCrystalsEarned = reward.ManaCrystalsEarned,
                    ExperienceEarned = reward.ExperienceEarned,
                    ItemNames = reward.Loot.Select(l => l.ItemName).ToList()
                });
            }

            // Boost corruption reduction from successful expeditions
            _state.Corruption.ReduceCorruption(2);
        }
    }

    // ─── Combo Tracking ──────────────────────────────────────────

    public ComboTracker GetComboTracker()
    {
        _comboTracker ??= new ComboTracker();
        return _comboTracker;
    }

    // ─── Astral Event Multipliers ────────────────────────────────

    public double GetEventMultiplier(string key)
    {
        double multiplier = 1.0;
        foreach (var evt in _state?.ActiveEvents?.Where(e => e.IsActive) ?? [])
        {
            if (evt.Multipliers.TryGetValue(key, out var mult))
                multiplier *= mult;
        }
        return multiplier;
    }

    // ─── Corruption Management ───────────────────────────────────

    public void TickCorruption(float deltaTimeSeconds)
    {
        if (_state is null) return;

        var neglectedCount = _state.Familiars.Count(f =>
            _state.FamiliarBonds.TryGetValue(f.Id, out var bond) && bond.Happiness < 20);

        var anchorCount = _state.Buildings.Count(b => b.Type == BuildingType.VoidAnchor);

        _state.Corruption.NeglectedFamiliarCount = neglectedCount;
        _state.Corruption.VoidAnchorCount = anchorCount;
    }

    public void ReduceCorruption(int amount)
    {
        _state?.Corruption.ReduceCorruption(amount);
    }
}
