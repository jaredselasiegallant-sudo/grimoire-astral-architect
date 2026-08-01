using System.Text.Json;
using Grimoire.Core.Accessibility;
using Grimoire.Core.Bonding;
using Grimoire.Core.Cosmetics;
using Grimoire.Core.Decay;
using Grimoire.Core.Enums;
using Grimoire.Core.Events;
using Grimoire.Core.Grimoire;
using Grimoire.Core.Interfaces;
using Grimoire.Core.Memories;
using Grimoire.Core.Models;
using Grimoire.Core.Signature;
using Grimoire.Core.Weather;
using Grimoire.Data.Database;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Grimoire.Data.Repositories;

/// <summary>
/// Concrete SQLite implementation of the game repository.
/// Maps between Core domain models and SQLite rows.
/// </summary>
public sealed class GameRepository : IGameRepository, IDisposable
{
    private readonly DatabaseContext _db;
    private readonly ILogger<GameRepository>? _logger;

    public GameRepository(string dbPath, ILogger<GameRepository>? logger = null)
    {
        _logger = logger;
        _db = new DatabaseContext(dbPath, logger as ILogger<DatabaseContext>);
    }

    public async Task InitialiseAsync()
    {
        var conn = await _db.OpenAsync();
        await DatabaseInitializer.InitialiseAsync(conn, _logger);
    }

    // ─── Game State ───────────────────────────────────────────────

    public async Task<GameState?> LoadGameStateAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM GameState LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var state = new GameState
        {
            PlayerId = Guid.Parse(reader.GetString("PlayerId")),
            PlayerName = reader.GetString("PlayerName"),
            ManaCrystals = reader.GetInt32("ManaCrystals"),
            TotalPlayTimeSeconds = reader.GetInt32("TotalPlayTimeSeconds"),
            LastSaveUTC = DateTimeOffset.Parse(reader.GetString("LastSaveUTC")),
            FirstLaunchUTC = DateTimeOffset.Parse(reader.GetString("FirstLaunchUTC"))
        };

        state.Familiars = await GetFamiliarsAsync();
        state.Inventory = await GetInventoryAsync();
        state.Buildings = await GetBuildingsAsync();
        state.Eggs = await GetEggsAsync();
        state.Recipes = await GetRecipesAsync();
        state.ExpeditionLog = await GetExpeditionLogAsync();
        state.ShownNarrativeLines = await GetShownNarrativeLinesAsync();
        state.CompletedTutorialSteps = await GetCompletedTutorialStepsAsync();

        // v3 systems
        state.FamiliarBonds = await GetBondsAsync();
        state.Grimoire = await LoadGrimoireJournalAsync();
        state.Corruption = await GetCorruptionAsync();
        state.Cosmetics = await GetLoadoutAsync();
        state.OwnedCosmetics = await GetCosmeticsAsync();
        state.PlayerSpells = await GetPlayerSpellsAsync();
        state.Accessibility = await GetAccessibilityAsync();
        state.ActiveEvents = await GetAstralEventsAsync();

        // v4 systems
        try { state.Weather = await GetWeatherStateAsync(); } catch { state.Weather = new WeatherState(); }
        try { state.Signature = await GetGestureSignatureAsync(); } catch { state.Signature = new GestureSignature(); }
        state.WitnessedEchoes = await GetMemoryEchoesAsync();
        state.DiscoveredRituals = await GetDiscoveredRitualsAsync();
        var constellations = await GetConstellationsAsync();
        state.Constellations = new SanctuaryConstellations { Stars = constellations };
        state.AscensionHistory = await GetAscensionHistoryAsync();

        try
        {
            var conn2 = _db.GetConnection();
            var cmd2 = conn2.CreateCommand();
            cmd2.CommandText = "SELECT * FROM GameState LIMIT 1";
            using var r2 = await cmd2.ExecuteReaderAsync();
            if (await r2.ReadAsync())
            {
                var ord = r2.GetOrdinal;
                if (ord("SanctuaryLevel") >= 0) state.SanctuaryLevel = r2.GetInt32(ord("SanctuaryLevel"));
                if (ord("TotalExpeditionsCompleted") >= 0) state.TotalExpeditionsCompleted = r2.GetInt32(ord("TotalExpeditionsCompleted"));
                if (ord("TotalSpellsCast") >= 0) state.TotalSpellsCast = r2.GetInt32(ord("TotalSpellsCast"));
                if (ord("TotalRecipesDiscovered") >= 0) state.TotalRecipesDiscovered = r2.GetInt32(ord("TotalRecipesDiscovered"));
                if (ord("FirstBuildingName") >= 0) state.FirstBuildingName = r2.IsDBNull(ord("FirstBuildingName")) ? null : r2.GetString(ord("FirstBuildingName"));
                if (ord("FirstBuildingId") >= 0) state.FirstBuildingId = r2.IsDBNull(ord("FirstBuildingId")) ? null : Guid.Parse(r2.GetString(ord("FirstBuildingId")));
                if (ord("FirstFamiliarName") >= 0) state.FirstFamiliarName = r2.IsDBNull(ord("FirstFamiliarName")) ? null : r2.GetString(ord("FirstFamiliarName"));
                if (ord("FirstFamiliarId") >= 0) state.FirstFamiliarId = r2.IsDBNull(ord("FirstFamiliarId")) ? null : Guid.Parse(r2.GetString(ord("FirstFamiliarId")));
            }
        }
        catch { }

        return state;
    }

    public async Task SaveGameStateAsync(GameState state)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO GameState (PlayerId, PlayerName, ManaCrystals, TotalPlayTimeSeconds, LastSaveUTC, FirstLaunchUTC,
                SanctuaryLevel, TotalExpeditionsCompleted, TotalSpellsCast, TotalRecipesDiscovered,
                FirstBuildingName, FirstBuildingId, FirstFamiliarName, FirstFamiliarId)
            VALUES (@id, @name, @mana, @play, @save, @first,
                @sancLevel, @expComp, @spells, @recipes,
                @bldgName, @bldgId, @famName, @famId);
        ";
        cmd.Parameters.AddWithValue("@id", state.PlayerId.ToString());
        cmd.Parameters.AddWithValue("@name", state.PlayerName);
        cmd.Parameters.AddWithValue("@mana", state.ManaCrystals);
        cmd.Parameters.AddWithValue("@play", state.TotalPlayTimeSeconds);
        cmd.Parameters.AddWithValue("@save", state.LastSaveUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@first", state.FirstLaunchUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@sancLevel", state.SanctuaryLevel);
        cmd.Parameters.AddWithValue("@expComp", state.TotalExpeditionsCompleted);
        cmd.Parameters.AddWithValue("@spells", state.TotalSpellsCast);
        cmd.Parameters.AddWithValue("@recipes", state.TotalRecipesDiscovered);
        cmd.Parameters.AddWithValue("@bldgName", (object?)state.FirstBuildingName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@bldgId", (object?)state.FirstBuildingId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@famName", (object?)state.FirstFamiliarName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@famId", (object?)state.FirstFamiliarId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        // Persist child collections
        foreach (var f in state.Familiars) await UpsertFamiliarAsync(f);
        foreach (var i in state.Inventory) await UpsertInventoryItemAsync(i);
        foreach (var b in state.Buildings) await UpsertBuildingAsync(b);
        foreach (var e in state.Eggs) await UpsertEggAsync(e);
        foreach (var r in state.Recipes) await UpsertRecipeAsync(r);

        // Replace expedition log
        var delLog = conn.CreateCommand();
        delLog.CommandText = "DELETE FROM ExpeditionLog";
        await delLog.ExecuteNonQueryAsync();
        foreach (var entry in state.ExpeditionLog) await InsertExpeditionLogAsync(entry);

        // Replace narrative progress
        var delNarr = conn.CreateCommand();
        delNarr.CommandText = "DELETE FROM NarrativeProgress";
        await delNarr.ExecuteNonQueryAsync();
        foreach (var lineId in state.ShownNarrativeLines) await InsertNarrativeLineAsync(lineId);

        // Replace tutorial progress
        var delTut = conn.CreateCommand();
        delTut.CommandText = "DELETE FROM TutorialProgress";
        await delTut.ExecuteNonQueryAsync();
        foreach (var stepId in state.CompletedTutorialSteps) await InsertTutorialStepAsync(stepId);

        // v3 systems
        foreach (var bond in state.FamiliarBonds.Values) await UpsertBondAsync(bond);
        await SaveGrimoireJournalAsync(state.Grimoire);
        await UpsertCorruptionAsync(state.Corruption);
        await UpsertLoadoutAsync(state.Cosmetics);
        foreach (var cosmetic in state.OwnedCosmetics) await UpsertCosmeticAsync(cosmetic);
        foreach (var spell in state.PlayerSpells) await UpsertPlayerSpellAsync(spell);
        await UpsertAccessibilityAsync(state.Accessibility);
        foreach (var evt in state.ActiveEvents) await UpsertAstralEventAsync(evt);

        // v4 systems
        await UpsertWeatherStateAsync(state.Weather);
        await UpsertGestureSignatureAsync(state.Signature);
        foreach (var echo in state.WitnessedEchoes) await UpsertMemoryEchoAsync(echo);
        await DeleteAllDiscoveredRitualsAsync();
        foreach (var ritual in state.DiscoveredRituals) await InsertDiscoveredRitualAsync(ritual);
        foreach (var c in state.Constellations.Stars) await InsertConstellationAsync(c);
        foreach (var a in state.AscensionHistory) await InsertAscensionAsync(a);
    }

    // ─── Familiars ────────────────────────────────────────────────

    public async Task<List<Familiar>> GetFamiliarsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Familiars";

        var list = new List<Familiar>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Familiar
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Type = (FamiliarType)reader.GetInt32("Type"),
                Element = (ElementType)reader.GetInt32("Element"),
                Rarity = (Rarity)reader.GetInt32("Rarity"),
                Level = reader.GetInt32("Level"),
                Experience = reader.GetInt32("Experience"),
                MaxHealth = reader.GetInt32("MaxHealth"),
                CurrentHealth = reader.GetInt32("CurrentHealth"),
                GatheringBonus = reader.GetDouble("GatheringBonus"),
                LastExpeditionUTC = reader.IsDBNull(reader.GetOrdinal("LastExpeditionUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("LastExpeditionUTC")),
                ExpeditionDuration = TimeSpan.FromTicks(reader.GetInt64("ExpeditionDurationTicks")),
                IsOnExpedition = reader.GetInt32("IsOnExpedition") == 1,
                ExpeditionReturnUTC = reader.IsDBNull(reader.GetOrdinal("ExpeditionReturnUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("ExpeditionReturnUTC"))
            });
        }
        return list;
    }

    public async Task UpsertFamiliarAsync(Familiar f)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Familiars
                (Id, Name, Type, Element, Rarity, Level, Experience, MaxHealth, CurrentHealth,
                 GatheringBonus, LastExpeditionUTC, ExpeditionDurationTicks, IsOnExpedition, ExpeditionReturnUTC)
            VALUES
                (@id, @name, @type, @element, @rarity, @level, @xp, @maxHp, @curHp,
                 @bonus, @lastExp, @expTicks, @isExp, @returnUtc);
        ";
        cmd.Parameters.AddWithValue("@id", f.Id.ToString());
        cmd.Parameters.AddWithValue("@name", f.Name);
        cmd.Parameters.AddWithValue("@type", (int)f.Type);
        cmd.Parameters.AddWithValue("@element", (int)f.Element);
        cmd.Parameters.AddWithValue("@rarity", (int)f.Rarity);
        cmd.Parameters.AddWithValue("@level", f.Level);
        cmd.Parameters.AddWithValue("@xp", f.Experience);
        cmd.Parameters.AddWithValue("@maxHp", f.MaxHealth);
        cmd.Parameters.AddWithValue("@curHp", f.CurrentHealth);
        cmd.Parameters.AddWithValue("@bonus", f.GatheringBonus);
        cmd.Parameters.AddWithValue("@lastExp", (object?)f.LastExpeditionUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@expTicks", f.ExpeditionDuration.Ticks);
        cmd.Parameters.AddWithValue("@isExp", f.IsOnExpedition ? 1 : 0);
        cmd.Parameters.AddWithValue("@returnUtc", (object?)f.ExpeditionReturnUTC?.ToString("o") ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteFamiliarAsync(Guid id)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Familiars WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Inventory ────────────────────────────────────────────────

    public async Task<List<InventoryItem>> GetInventoryAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM InventoryItems";

        var list = new List<InventoryItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new InventoryItem
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Description = reader.GetString("Description"),
                Element = (ElementType)reader.GetInt32("Element"),
                Rarity = (Rarity)reader.GetInt32("Rarity"),
                Quantity = reader.GetInt32("Quantity"),
                ManaPower = reader.GetInt32("ManaPower"),
                ValidForBuilding = reader.IsDBNull(reader.GetOrdinal("ValidForBuilding"))
                    ? null : (BuildingType)reader.GetInt32("ValidForBuilding")
            });
        }
        return list;
    }

    public async Task UpsertInventoryItemAsync(InventoryItem item)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO InventoryItems
                (Id, Name, Description, Element, Rarity, Quantity, ManaPower, ValidForBuilding)
            VALUES
                (@id, @name, @desc, @element, @rarity, @qty, @power, @bldg);
        ";
        cmd.Parameters.AddWithValue("@id", item.Id.ToString());
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@desc", item.Description);
        cmd.Parameters.AddWithValue("@element", (int)item.Element);
        cmd.Parameters.AddWithValue("@rarity", (int)item.Rarity);
        cmd.Parameters.AddWithValue("@qty", item.Quantity);
        cmd.Parameters.AddWithValue("@power", item.ManaPower);
        cmd.Parameters.AddWithValue("@bldg", (object?)(item.ValidForBuilding.HasValue ? item.ValidForBuilding.Value : DBNull.Value)!);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteInventoryItemAsync(Guid id)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM InventoryItems WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Buildings ────────────────────────────────────────────────

    public async Task<List<SanctuaryBuilding>> GetBuildingsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM SanctuaryBuildings";

        var list = new List<SanctuaryBuilding>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new SanctuaryBuilding
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Type = (BuildingType)reader.GetInt32("Type"),
                Name = reader.GetString("Name"),
                GridX = reader.GetInt32("GridX"),
                GridY = reader.GetInt32("GridY"),
                Level = reader.GetInt32("Level"),
                MaxLevel = reader.GetInt32("MaxLevel"),
                ManaPerSecond = reader.GetDouble("ManaPerSecond"),
                HabitatSlots = reader.GetInt32("HabitatSlots"),
                UpgradeFinishUTC = reader.IsDBNull(reader.GetOrdinal("UpgradeFinishUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("UpgradeFinishUTC")),
                LastProductionUTC = reader.IsDBNull(reader.GetOrdinal("LastProductionUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("LastProductionUTC"))
            });
        }
        return list;
    }

    public async Task UpsertBuildingAsync(SanctuaryBuilding b)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO SanctuaryBuildings
                (Id, Type, Name, GridX, GridY, Level, MaxLevel, ManaPerSecond, HabitatSlots, UpgradeFinishUTC, LastProductionUTC)
            VALUES
                (@id, @type, @name, @gx, @gy, @lvl, @max, @mps, @slots, @upg, @prod);
        ";
        cmd.Parameters.AddWithValue("@id", b.Id.ToString());
        cmd.Parameters.AddWithValue("@type", (int)b.Type);
        cmd.Parameters.AddWithValue("@name", b.Name);
        cmd.Parameters.AddWithValue("@gx", b.GridX);
        cmd.Parameters.AddWithValue("@gy", b.GridY);
        cmd.Parameters.AddWithValue("@lvl", b.Level);
        cmd.Parameters.AddWithValue("@max", b.MaxLevel);
        cmd.Parameters.AddWithValue("@mps", b.ManaPerSecond);
        cmd.Parameters.AddWithValue("@slots", b.HabitatSlots);
        cmd.Parameters.AddWithValue("@upg", (object?)b.UpgradeFinishUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@prod", (object?)b.LastProductionUTC?.ToString("o") ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteBuildingAsync(Guid id)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SanctuaryBuildings WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Eggs ─────────────────────────────────────────────────────

    public async Task<List<FamiliarEgg>> GetEggsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM FamiliarEggs";

        var list = new List<FamiliarEgg>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new FamiliarEgg
            {
                Id = Guid.Parse(reader.GetString("Id")),
                HatchesInto = (FamiliarType)reader.GetInt32("HatchesInto"),
                Element = (ElementType)reader.GetInt32("Element"),
                Rarity = (Rarity)reader.GetInt32("Rarity"),
                HatchDurationSeconds = reader.GetInt32("HatchDurationSeconds"),
                IncubationStartUTC = reader.IsDBNull(reader.GetOrdinal("IncubationStartUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("IncubationStartUTC")),
                HatchReadyUTC = reader.IsDBNull(reader.GetOrdinal("HatchReadyUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("HatchReadyUTC")),
                IsIncubating = reader.GetInt32("IsIncubating") == 1,
                HasHatched = reader.GetInt32("HasHatched") == 1
            });
        }
        return list;
    }

    public async Task UpsertEggAsync(FamiliarEgg egg)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO FamiliarEggs
                (Id, HatchesInto, Element, Rarity, HatchDurationSeconds, IncubationStartUTC, HatchReadyUTC, IsIncubating, HasHatched)
            VALUES
                (@id, @hatch, @element, @rarity, @dur, @start, @ready, @inc, @done);
        ";
        cmd.Parameters.AddWithValue("@id", egg.Id.ToString());
        cmd.Parameters.AddWithValue("@hatch", (int)egg.HatchesInto);
        cmd.Parameters.AddWithValue("@element", (int)egg.Element);
        cmd.Parameters.AddWithValue("@rarity", (int)egg.Rarity);
        cmd.Parameters.AddWithValue("@dur", egg.HatchDurationSeconds);
        cmd.Parameters.AddWithValue("@start", (object?)egg.IncubationStartUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ready", (object?)egg.HatchReadyUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@inc", egg.IsIncubating ? 1 : 0);
        cmd.Parameters.AddWithValue("@done", egg.HasHatched ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Recipes ──────────────────────────────────────────────────

    public async Task<List<CraftingRecipe>> GetRecipesAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CraftingRecipes";

        var list = new List<CraftingRecipe>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CraftingRecipe
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Description = reader.GetString("Description"),
                IngredientAElement = (ElementType)reader.GetInt32("IngredientAElement"),
                IngredientBElement = (ElementType)reader.GetInt32("IngredientBElement"),
                IngredientAMinRarity = (Rarity)reader.GetInt32("IngredientAMinRarity"),
                IngredientBMinRarity = (Rarity)reader.GetInt32("IngredientBMinRarity"),
                OutputName = reader.GetString("OutputName"),
                OutputElement = (ElementType)reader.GetInt32("OutputElement"),
                OutputManaPower = reader.GetInt32("OutputManaPower"),
                OutputRarity = (Rarity)reader.GetInt32("OutputRarity"),
                OutputQuantity = reader.GetInt32("OutputQuantity"),
                IsDiscovered = reader.GetInt32("IsDiscovered") == 1,
                IsUnlocked = reader.GetInt32("IsUnlocked") == 1
            });
        }
        return list;
    }

    public async Task UpsertRecipeAsync(CraftingRecipe recipe)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO CraftingRecipes
                (Id, Name, Description, IngredientAElement, IngredientBElement,
                 IngredientAMinRarity, IngredientBMinRarity, OutputName, OutputElement,
                 OutputManaPower, OutputRarity, OutputQuantity, IsDiscovered, IsUnlocked)
            VALUES
                (@id, @name, @desc, @elemA, @elemB, @rarA, @rarB, @out, @outElem,
                 @outPow, @outRar, @outQty, @disc, @unlock);
        ";
        cmd.Parameters.AddWithValue("@id", recipe.Id.ToString());
        cmd.Parameters.AddWithValue("@name", recipe.Name);
        cmd.Parameters.AddWithValue("@desc", recipe.Description);
        cmd.Parameters.AddWithValue("@elemA", (int)recipe.IngredientAElement);
        cmd.Parameters.AddWithValue("@elemB", (int)recipe.IngredientBElement);
        cmd.Parameters.AddWithValue("@rarA", (int)recipe.IngredientAMinRarity);
        cmd.Parameters.AddWithValue("@rarB", (int)recipe.IngredientBMinRarity);
        cmd.Parameters.AddWithValue("@out", recipe.OutputName);
        cmd.Parameters.AddWithValue("@outElem", (int)recipe.OutputElement);
        cmd.Parameters.AddWithValue("@outPow", recipe.OutputManaPower);
        cmd.Parameters.AddWithValue("@outRar", (int)recipe.OutputRarity);
        cmd.Parameters.AddWithValue("@outQty", recipe.OutputQuantity);
        cmd.Parameters.AddWithValue("@disc", recipe.IsDiscovered ? 1 : 0);
        cmd.Parameters.AddWithValue("@unlock", recipe.IsUnlocked ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Expedition Log ───────────────────────────────────────────

    public async Task<List<ExpeditionLogEntry>> GetExpeditionLogAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM ExpeditionLog ORDER BY ReturnedUTC DESC LIMIT 50";

        var list = new List<ExpeditionLogEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var itemNamesStr = reader.GetString("ItemNames");
            var itemNames = System.Text.Json.JsonSerializer.Deserialize<List<string>>(itemNamesStr) ?? [];

            list.Add(new ExpeditionLogEntry
            {
                Id = Guid.Parse(reader.GetString("Id")),
                FamiliarName = reader.GetString("FamiliarName"),
                DepartedUTC = DateTimeOffset.Parse(reader.GetString("DepartedUTC")),
                ReturnedUTC = DateTimeOffset.Parse(reader.GetString("ReturnedUTC")),
                Duration = TimeSpan.FromTicks(reader.GetInt64("DurationTicks")),
                Success = reader.GetInt32("Success") == 1,
                ManaCrystalsEarned = reader.GetInt32("ManaCrystalsEarned"),
                ExperienceEarned = reader.GetInt32("ExperienceEarned"),
                ItemNames = itemNames,
                NarrativeNote = reader.IsDBNull(reader.GetOrdinal("NarrativeNote"))
                    ? null : reader.GetString("NarrativeNote")
            });
        }
        return list;
    }

    public async Task InsertExpeditionLogAsync(ExpeditionLogEntry entry)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ExpeditionLog
                (Id, FamiliarName, DepartedUTC, ReturnedUTC, DurationTicks, Success, ManaCrystalsEarned, ExperienceEarned, ItemNames, NarrativeNote)
            VALUES
                (@id, @name, @dep, @ret, @dur, @succ, @mana, @xp, @items, @note);
        ";
        cmd.Parameters.AddWithValue("@id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("@name", entry.FamiliarName);
        cmd.Parameters.AddWithValue("@dep", entry.DepartedUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@ret", entry.ReturnedUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@dur", entry.Duration.Ticks);
        cmd.Parameters.AddWithValue("@succ", entry.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("@mana", entry.ManaCrystalsEarned);
        cmd.Parameters.AddWithValue("@xp", entry.ExperienceEarned);
        cmd.Parameters.AddWithValue("@items", System.Text.Json.JsonSerializer.Serialize(entry.ItemNames));
        cmd.Parameters.AddWithValue("@note", (object?)entry.NarrativeNote ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Narrative Progress ───────────────────────────────────────

    public async Task<List<string>> GetShownNarrativeLinesAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT LineId FROM NarrativeProgress";

        var list = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(reader.GetString("LineId"));
        return list;
    }

    public async Task InsertNarrativeLineAsync(string lineId)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO NarrativeProgress (LineId, ShownUTC) VALUES (@id, @now)";
        cmd.Parameters.AddWithValue("@id", lineId);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Tutorial Progress ────────────────────────────────────────

    public async Task<List<string>> GetCompletedTutorialStepsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT StepId FROM TutorialProgress";

        var list = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(reader.GetString("StepId"));
        return list;
    }

    public async Task InsertTutorialStepAsync(string stepId)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO TutorialProgress (StepId, CompletedUTC) VALUES (@id, @now)";
        cmd.Parameters.AddWithValue("@id", stepId);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Familiar Bonds ──────────────────────────────────────────

    public async Task<Dictionary<Guid, FamiliarBond>> GetBondsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM FamiliarBonds";

        var dict = new Dictionary<Guid, FamiliarBond>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dialoguesStr = reader.GetString("UnlockedDialogues");
            var dialogues = JsonSerializer.Deserialize<List<string>>(dialoguesStr) ?? [];
            var familiarId = Guid.Parse(reader.GetString("FamiliarId"));

            dict[familiarId] = new FamiliarBond
            {
                FamiliarId = familiarId,
                Happiness = reader.GetInt32("Happiness"),
                Trust = reader.GetInt32("Trust"),
                Affection = reader.GetInt32("Affection"),
                Personality = (FamiliarPersonality)reader.GetInt32("Personality"),
                PreferredActivity = (BondingActivity)reader.GetInt32("PreferredActivity"),
                SessionInteractions = reader.GetInt32("SessionInteractions"),
                LastInteractionUTC = reader.IsDBNull(reader.GetOrdinal("LastInteractionUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("LastInteractionUTC")),
                UnlockedDialogues = dialogues
            };
        }
        return dict;
    }

    public async Task UpsertBondAsync(FamiliarBond bond)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO FamiliarBonds
                (FamiliarId, Happiness, Trust, Affection, Personality, PreferredActivity,
                 SessionInteractions, LastInteractionUTC, UnlockedDialogues)
            VALUES
                (@famId, @happy, @trust, @affection, @personality, @activity,
                 @session, @lastInt, @dialogues);
        ";
        cmd.Parameters.AddWithValue("@famId", bond.FamiliarId.ToString());
        cmd.Parameters.AddWithValue("@happy", bond.Happiness);
        cmd.Parameters.AddWithValue("@trust", bond.Trust);
        cmd.Parameters.AddWithValue("@affection", bond.Affection);
        cmd.Parameters.AddWithValue("@personality", (int)bond.Personality);
        cmd.Parameters.AddWithValue("@activity", (int)bond.PreferredActivity);
        cmd.Parameters.AddWithValue("@session", bond.SessionInteractions);
        cmd.Parameters.AddWithValue("@lastInt", (object?)bond.LastInteractionUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@dialogues", JsonSerializer.Serialize(bond.UnlockedDialogues));
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Grimoire Journal ────────────────────────────────────────

    public async Task<GrimoireJournal> LoadGrimoireJournalAsync()
    {
        var journal = new GrimoireJournal();
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM GrimoireEntries";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entry = new GrimoireEntry
            {
                Id = reader.GetString("Id"),
                IsUnlocked = reader.GetInt32("IsUnlocked") == 1,
                UnlockedUTC = reader.IsDBNull(reader.GetOrdinal("UnlockedUTC"))
                    ? null : DateTimeOffset.Parse(reader.GetString("UnlockedUTC")),
                Category = (GrmoireCategory)reader.GetInt32("Category"),
                SortOrder = reader.GetInt32("SortOrder")
            };
            journal.Entries[entry.Id] = entry;
        }
        return journal;
    }

    public async Task<List<GrimoireEntry>> GetGrimoireEntriesAsync()
    {
        var journal = await LoadGrimoireJournalAsync();
        return journal.Entries.Values.ToList();
    }

    public async Task UpsertGrimoireEntryAsync(GrimoireEntry entry)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO GrimoireEntries
                (Id, IsUnlocked, UnlockedUTC, Category, SortOrder)
            VALUES
                (@id, @unlocked, @unlockUtc, @category, @sort);
        ";
        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.Parameters.AddWithValue("@unlocked", entry.IsUnlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("@unlockUtc", (object?)entry.UnlockedUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@category", (int)entry.Category);
        cmd.Parameters.AddWithValue("@sort", entry.SortOrder);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveGrimoireJournalAsync(GrimoireJournal journal)
    {
        foreach (var entry in journal.Entries.Values)
            await UpsertGrimoireEntryAsync(entry);
    }

    // ─── Corruption State ────────────────────────────────────────

    public async Task<CorruptionState> GetCorruptionAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CorruptionState LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new CorruptionState();

        return new CorruptionState
        {
            CorruptionLevel = reader.GetInt32("CorruptionLevel"),
            BaseDecayRate = reader.GetDouble("BaseDecayRate"),
            NeglectedFamiliarCount = reader.GetInt32("NeglectedFamiliarCount"),
            IsolatedBuildingCount = reader.GetInt32("IsolatedBuildingCount"),
            VoidAnchorCount = reader.GetInt32("VoidAnchorCount")
        };
    }

    public async Task UpsertCorruptionAsync(CorruptionState corruption)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO CorruptionState
                (Id, CorruptionLevel, BaseDecayRate, NeglectedFamiliarCount, IsolatedBuildingCount, VoidAnchorCount)
            VALUES
                (1, @level, @rate, @neglected, @isolated, @anchors);
        ";
        cmd.Parameters.AddWithValue("@level", corruption.CorruptionLevel);
        cmd.Parameters.AddWithValue("@rate", corruption.BaseDecayRate);
        cmd.Parameters.AddWithValue("@neglected", corruption.NeglectedFamiliarCount);
        cmd.Parameters.AddWithValue("@isolated", corruption.IsolatedBuildingCount);
        cmd.Parameters.AddWithValue("@anchors", corruption.VoidAnchorCount);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Cosmetic Items ──────────────────────────────────────────

    public async Task<List<CosmeticItem>> GetCosmeticsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CosmeticItems";

        var list = new List<CosmeticItem>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CosmeticItem
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                Description = reader.GetString("Description"),
                Type = (CosmeticType)reader.GetInt32("Type"),
                ColourHex = reader.IsDBNull(reader.GetOrdinal("ColourHex"))
                    ? null : reader.GetString("ColourHex"),
                EffectId = reader.IsDBNull(reader.GetOrdinal("EffectId"))
                    ? null : reader.GetString("EffectId"),
                IsUnlocked = reader.GetInt32("IsUnlocked") == 1,
                IsEquipped = reader.GetInt32("IsEquipped") == 1,
                UnlockRequirement = reader.IsDBNull(reader.GetOrdinal("UnlockRequirement"))
                    ? null : reader.GetString("UnlockRequirement")
            });
        }
        return list;
    }

    public async Task UpsertCosmeticAsync(CosmeticItem item)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO CosmeticItems
                (Id, Name, Description, Type, ColourHex, EffectId, IsUnlocked, IsEquipped, UnlockRequirement)
            VALUES
                (@id, @name, @desc, @type, @colour, @effect, @unlocked, @equipped, @req);
        ";
        cmd.Parameters.AddWithValue("@id", item.Id.ToString());
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@desc", item.Description);
        cmd.Parameters.AddWithValue("@type", (int)item.Type);
        cmd.Parameters.AddWithValue("@colour", (object?)item.ColourHex ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@effect", (object?)item.EffectId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@unlocked", item.IsUnlocked ? 1 : 0);
        cmd.Parameters.AddWithValue("@equipped", item.IsEquipped ? 1 : 0);
        cmd.Parameters.AddWithValue("@req", (object?)item.UnlockRequirement ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Cosmetic Loadout ────────────────────────────────────────

    public async Task<CosmeticLoadout> GetLoadoutAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM CosmeticLoadout LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new CosmeticLoadout();

        return new CosmeticLoadout
        {
            ActiveShrineSkin = reader.IsDBNull(reader.GetOrdinal("ActiveShrineSkin"))
                ? null : reader.GetString("ActiveShrineSkin"),
            ActiveParticleColour = reader.IsDBNull(reader.GetOrdinal("ActiveParticleColour"))
                ? null : reader.GetString("ActiveParticleColour"),
            ActiveFamiliarAccessory = reader.IsDBNull(reader.GetOrdinal("ActiveFamiliarAccessory"))
                ? null : reader.GetString("ActiveFamiliarAccessory"),
            ActiveSkyboxTint = reader.IsDBNull(reader.GetOrdinal("ActiveSkyboxTint"))
                ? null : reader.GetString("ActiveSkyboxTint"),
            ActiveTrailStyle = reader.IsDBNull(reader.GetOrdinal("ActiveTrailStyle"))
                ? null : reader.GetString("ActiveTrailStyle"),
            ActiveSanctuaryTheme = reader.IsDBNull(reader.GetOrdinal("ActiveSanctuaryTheme"))
                ? null : reader.GetString("ActiveSanctuaryTheme")
        };
    }

    public async Task UpsertLoadoutAsync(CosmeticLoadout loadout)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO CosmeticLoadout
                (Id, ActiveShrineSkin, ActiveParticleColour, ActiveFamiliarAccessory,
                 ActiveSkyboxTint, ActiveTrailStyle, ActiveSanctuaryTheme)
            VALUES
                (1, @shrine, @particle, @accessory, @skybox, @trail, @theme);
        ";
        cmd.Parameters.AddWithValue("@shrine", (object?)loadout.ActiveShrineSkin ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@particle", (object?)loadout.ActiveParticleColour ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@accessory", (object?)loadout.ActiveFamiliarAccessory ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@skybox", (object?)loadout.ActiveSkyboxTint ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@trail", (object?)loadout.ActiveTrailStyle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@theme", (object?)loadout.ActiveSanctuaryTheme ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Player Spells ───────────────────────────────────────────

    public async Task<List<PlayerSpell>> GetPlayerSpellsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM PlayerSpells";

        var list = new List<PlayerSpell>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var shapeStr = reader.GetString("ReferenceShape");
            var shape = JsonSerializer.Deserialize<List<System.Numerics.Vector2>>(shapeStr) ?? [];

            list.Add(new PlayerSpell
            {
                Id = Guid.Parse(reader.GetString("Id")),
                Name = reader.GetString("Name"),
                ReferenceShape = shape,
                DetectedArchetype = (SpellArchetype)reader.GetInt32("DetectedArchetype"),
                PowerModifier = reader.GetFloat("PowerModifier"),
                Element = (ElementType)reader.GetInt32("Element"),
                CastCount = reader.GetInt32("CastCount"),
                CreatedUTC = DateTimeOffset.Parse(reader.GetString("CreatedUTC")),
                IsEquipped = reader.GetInt32("IsEquipped") == 1
            });
        }
        return list;
    }

    public async Task UpsertPlayerSpellAsync(PlayerSpell spell)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO PlayerSpells
                (Id, Name, ReferenceShape, DetectedArchetype, PowerModifier, Element, CastCount, CreatedUTC, IsEquipped)
            VALUES
                (@id, @name, @shape, @archetype, @power, @element, @casts, @created, @equipped);
        ";
        cmd.Parameters.AddWithValue("@id", spell.Id.ToString());
        cmd.Parameters.AddWithValue("@name", spell.Name);
        cmd.Parameters.AddWithValue("@shape", JsonSerializer.Serialize(spell.ReferenceShape));
        cmd.Parameters.AddWithValue("@archetype", (int)spell.DetectedArchetype);
        cmd.Parameters.AddWithValue("@power", spell.PowerModifier);
        cmd.Parameters.AddWithValue("@element", (int)spell.Element);
        cmd.Parameters.AddWithValue("@casts", spell.CastCount);
        cmd.Parameters.AddWithValue("@created", spell.CreatedUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@equipped", spell.IsEquipped ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Accessibility Settings ──────────────────────────────────

    public async Task<AccessibilitySettings> GetAccessibilityAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AccessibilitySettings LIMIT 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new AccessibilitySettings();

        return new AccessibilitySettings
        {
            GestureAssist = (GestureAssistMode)reader.GetInt32("GestureAssist"),
            ColorBlindMode = (ColorBlindMode)reader.GetInt32("ColorBlindMode"),
            TextToSpeechEnabled = reader.GetInt32("TextToSpeechEnabled") == 1,
            IdlePacingMultiplier = reader.GetFloat("IdlePacingMultiplier"),
            HighContrastMode = reader.GetInt32("HighContrastMode") == 1,
            ReducedMotion = reader.GetInt32("ReducedMotion") == 1,
            SubtitleScale = reader.GetFloat("SubtitleScale"),
            PersistentTrail = reader.GetInt32("PersistentTrail") == 1
        };
    }

    public async Task UpsertAccessibilityAsync(AccessibilitySettings settings)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO AccessibilitySettings
                (Id, GestureAssist, ColorBlindMode, TextToSpeechEnabled, IdlePacingMultiplier,
                 HighContrastMode, ReducedMotion, SubtitleScale, PersistentTrail)
            VALUES
                (1, @gesture, @colour, @tts, @pacing, @contrast, @motion, @subtitle, @trail);
        ";
        cmd.Parameters.AddWithValue("@gesture", (int)settings.GestureAssist);
        cmd.Parameters.AddWithValue("@colour", (int)settings.ColorBlindMode);
        cmd.Parameters.AddWithValue("@tts", settings.TextToSpeechEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@pacing", settings.IdlePacingMultiplier);
        cmd.Parameters.AddWithValue("@contrast", settings.HighContrastMode ? 1 : 0);
        cmd.Parameters.AddWithValue("@motion", settings.ReducedMotion ? 1 : 0);
        cmd.Parameters.AddWithValue("@subtitle", settings.SubtitleScale);
        cmd.Parameters.AddWithValue("@trail", settings.PersistentTrail ? 1 : 0);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Astral Events ───────────────────────────────────────────

    public async Task<List<AstralEvent>> GetAstralEventsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AstralEvents";

        var list = new List<AstralEvent>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var multiStr = reader.GetString("Multipliers");
            var multipliers = JsonSerializer.Deserialize<Dictionary<string, double>>(multiStr) ?? [];

            list.Add(new AstralEvent
            {
                Id = reader.GetString("Id"),
                Name = reader.GetString("Name"),
                Description = reader.GetString("Description"),
                Type = (AstralEventType)reader.GetInt32("Type"),
                Frequency = (EventTypeFrequency)reader.GetInt32("Frequency"),
                DurationHours = reader.GetInt32("DurationHours"),
                StartUTC = DateTimeOffset.Parse(reader.GetString("StartUTC")),
                EndUTC = DateTimeOffset.Parse(reader.GetString("EndUTC")),
                Multipliers = multipliers,
                SkyTintHex = reader.GetString("SkyTintHex")
            });
        }
        return list;
    }

    public async Task UpsertAstralEventAsync(AstralEvent evt)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO AstralEvents
                (Id, Name, Description, Type, Frequency, DurationHours, StartUTC, EndUTC, Multipliers, SkyTintHex)
            VALUES
                (@id, @name, @desc, @type, @freq, @dur, @start, @end, @multi, @tint);
        ";
        cmd.Parameters.AddWithValue("@id", evt.Id);
        cmd.Parameters.AddWithValue("@name", evt.Name);
        cmd.Parameters.AddWithValue("@desc", evt.Description);
        cmd.Parameters.AddWithValue("@type", (int)evt.Type);
        cmd.Parameters.AddWithValue("@freq", (int)evt.Frequency);
        cmd.Parameters.AddWithValue("@dur", evt.DurationHours);
        cmd.Parameters.AddWithValue("@start", evt.StartUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@end", evt.EndUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@multi", JsonSerializer.Serialize(evt.Multipliers));
        cmd.Parameters.AddWithValue("@tint", evt.SkyTintHex);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteExpiredEventsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM AstralEvents WHERE EndUTC < @now";
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Weather State (v4) ───────────────────────────────────────

    public async Task<WeatherState> GetWeatherStateAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM WeatherState WHERE Id = 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new WeatherState();

        return new WeatherState
        {
            CurrentWeather = (WeatherType)reader.GetInt32("CurrentWeather"),
            Intensity = reader.GetFloat("Intensity"),
            StartedUTC = DateTimeOffset.Parse(reader.GetString("StartedUTC")),
            Duration = TimeSpan.FromTicks(reader.GetInt64("DurationTicks"))
        };
    }

    public async Task UpsertWeatherStateAsync(WeatherState ws)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO WeatherState
                (Id, CurrentWeather, Intensity, StartedUTC, DurationTicks)
            VALUES
                (1, @weather, @intensity, @started, @durTicks);
        ";
        cmd.Parameters.AddWithValue("@weather", (int)ws.CurrentWeather);
        cmd.Parameters.AddWithValue("@intensity", ws.Intensity);
        cmd.Parameters.AddWithValue("@started", ws.StartedUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@durTicks", ws.Duration.Ticks);
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Gesture Signature (v4) ──────────────────────────────────

    public async Task<GestureSignature> GetGestureSignatureAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM GestureSignature WHERE Id = 1";

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new GestureSignature();

        var shapeStr = reader.GetString("ShapeFrequency");
        var shapeFreq = JsonSerializer.Deserialize<Dictionary<string, int>>(shapeStr) ?? [];
        var timingStr = reader.GetString("TimingHistogram");
        var timingHist = JsonSerializer.Deserialize<float[]>(timingStr) ?? new float[10];

        return new GestureSignature
        {
            SampleCount = reader.GetInt32("SampleCount"),
            AverageSpeed = reader.GetFloat("AverageSpeed"),
            AverageAngularVelocity = reader.GetFloat("AverageAngularVelocity"),
            PreferredStartAngle = reader.GetFloat("PreferredStartAngle"),
            AverageDuration = reader.GetFloat("AverageDuration"),
            AveragePauseBetweenGestures = reader.GetFloat("AveragePauseBetweenGestures"),
            ShapeFrequency = shapeFreq,
            TimingHistogram = timingHist
        };
    }

    public async Task UpsertGestureSignatureAsync(GestureSignature sig)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO GestureSignature
                (Id, SampleCount, AverageSpeed, AverageAngularVelocity, PreferredStartAngle,
                 AverageDuration, AveragePauseBetweenGestures, ShapeFrequency, TimingHistogram)
            VALUES
                (1, @samples, @speed, @angVel, @startAngle, @duration, @pause, @shapes, @timing);
        ";
        cmd.Parameters.AddWithValue("@samples", sig.SampleCount);
        cmd.Parameters.AddWithValue("@speed", sig.AverageSpeed);
        cmd.Parameters.AddWithValue("@angVel", sig.AverageAngularVelocity);
        cmd.Parameters.AddWithValue("@startAngle", sig.PreferredStartAngle);
        cmd.Parameters.AddWithValue("@duration", sig.AverageDuration);
        cmd.Parameters.AddWithValue("@pause", sig.AveragePauseBetweenGestures);
        cmd.Parameters.AddWithValue("@shapes", JsonSerializer.Serialize(sig.ShapeFrequency));
        cmd.Parameters.AddWithValue("@timing", JsonSerializer.Serialize(sig.TimingHistogram));
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Memory Echoes (v4) ──────────────────────────────────────

    public async Task<List<MemoryEcho>> GetMemoryEchoesAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM MemoryEchoes";

        var list = new List<MemoryEcho>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var restoredOrd = reader.GetOrdinal("RestoredBuilding");
            var restored = reader.IsDBNull(restoredOrd)
                ? (BuildingType?)null : (BuildingType)reader.GetInt32(restoredOrd);
            var witnessedOrd = reader.GetOrdinal("FirstWitnessedUTC");
            var witnessed = reader.IsDBNull(witnessedOrd)
                ? (DateTimeOffset?)null : DateTimeOffset.Parse(reader.GetString(witnessedOrd));
            var tintOrd = reader.GetOrdinal("GhostTintHex");
            var tint = reader.IsDBNull(tintOrd) ? null : reader.GetString(tintOrd);
            var descOrd = reader.GetOrdinal("VisualDescription");
            var desc = reader.IsDBNull(descOrd) ? "" : reader.GetString(descOrd);

            list.Add(new MemoryEcho
            {
                Id = Guid.Parse(reader.GetString("Id")),
                GridX = reader.GetInt32("GridX"),
                GridY = reader.GetInt32("GridY"),
                RestoredBuilding = restored,
                VisualDescription = desc,
                DurationSeconds = reader.GetFloat("DurationSeconds"),
                HasBeenWitnessed = reader.GetInt32("HasBeenWitnessed") == 1,
                FirstWitnessedUTC = witnessed,
                GhostTintHex = tint ?? "#A0C0FF"
            });
        }
        return list;
    }

    public async Task<MemoryEcho?> GetMemoryEchoByIdAsync(Guid id)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM MemoryEchoes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var restoredOrd = reader.GetOrdinal("RestoredBuilding");
        var restored = reader.IsDBNull(restoredOrd)
            ? (BuildingType?)null : (BuildingType)reader.GetInt32(restoredOrd);
        var witnessedOrd = reader.GetOrdinal("FirstWitnessedUTC");
        var witnessed = reader.IsDBNull(witnessedOrd)
            ? (DateTimeOffset?)null : DateTimeOffset.Parse(reader.GetString(witnessedOrd));
        var tintOrd = reader.GetOrdinal("GhostTintHex");
        var tint = reader.IsDBNull(tintOrd) ? null : reader.GetString(tintOrd);
        var descOrd = reader.GetOrdinal("VisualDescription");
        var desc = reader.IsDBNull(descOrd) ? "" : reader.GetString(descOrd);

        return new MemoryEcho
        {
            Id = Guid.Parse(reader.GetString("Id")),
            GridX = reader.GetInt32("GridX"),
            GridY = reader.GetInt32("GridY"),
            RestoredBuilding = restored,
            VisualDescription = desc,
            DurationSeconds = reader.GetFloat("DurationSeconds"),
            HasBeenWitnessed = reader.GetInt32("HasBeenWitnessed") == 1,
            FirstWitnessedUTC = witnessed,
            GhostTintHex = tint ?? "#A0C0FF"
        };
    }

    public async Task UpsertMemoryEchoAsync(MemoryEcho echo)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO MemoryEchoes
                (Id, GridX, GridY, RestoredBuilding, VisualDescription, DurationSeconds,
                 HasBeenWitnessed, FirstWitnessedUTC, GhostTintHex)
            VALUES
                (@id, @gx, @gy, @bldg, @desc, @dur, @witnessed, @witnessedUtc, @tint);
        ";
        cmd.Parameters.AddWithValue("@id", echo.Id.ToString());
        cmd.Parameters.AddWithValue("@gx", echo.GridX);
        cmd.Parameters.AddWithValue("@gy", echo.GridY);
        cmd.Parameters.AddWithValue("@bldg", (object?)(echo.RestoredBuilding.HasValue ? (int)echo.RestoredBuilding.Value : (int?)null) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desc", echo.VisualDescription);
        cmd.Parameters.AddWithValue("@dur", echo.DurationSeconds);
        cmd.Parameters.AddWithValue("@witnessed", echo.HasBeenWitnessed ? 1 : 0);
        cmd.Parameters.AddWithValue("@witnessedUtc", (object?)echo.FirstWitnessedUTC?.ToString("o") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@tint", echo.GhostTintHex);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteMemoryEchoAsync(Guid id)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM MemoryEchoes WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Discovered Rituals (v4) ─────────────────────────────────

    public async Task<HashSet<string>> GetDiscoveredRitualsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name FROM ArchitecturalRituals";

        var set = new HashSet<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            set.Add(reader.GetString("Name"));
        return set;
    }

    public async Task InsertDiscoveredRitualAsync(string ritualName)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO ArchitecturalRituals (Name, DiscoveredUTC) VALUES (@name, @now)";
        cmd.Parameters.AddWithValue("@name", ritualName);
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.UtcNow.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAllDiscoveredRitualsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM ArchitecturalRituals";
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Constellations (v4) ─────────────────────────────────────

    public async Task<List<Constellation>> GetConstellationsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Constellations";

        var list = new List<Constellation>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var origOrd = reader.GetOrdinal("OriginalFamiliarName");
            var origName = reader.IsDBNull(origOrd) ? "" : reader.GetString(origOrd);

            list.Add(new Constellation
            {
                Name = reader.GetString("Name"),
                Description = reader.GetString("Description"),
                Element = (ElementType)reader.GetInt32("Element"),
                AscendedUTC = DateTimeOffset.Parse(reader.GetString("AscendedUTC")),
                OriginalFamiliarName = origName
            });
        }
        return list;
    }

    public async Task InsertConstellationAsync(Constellation c)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Constellations
                (Name, Description, Element, AscendedUTC, OriginalFamiliarName)
            VALUES
                (@name, @desc, @element, @ascended, @origName);
        ";
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@desc", c.Description);
        cmd.Parameters.AddWithValue("@element", (int)c.Element);
        cmd.Parameters.AddWithValue("@ascended", c.AscendedUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@origName", c.OriginalFamiliarName);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAllConstellationsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Constellations";
        await cmd.ExecuteNonQueryAsync();
    }

    // ─── Ascension History (v4) ──────────────────────────────────

    public async Task<List<AscensionEvent>> GetAscensionHistoryAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AscensionHistory";

        var list = new List<AscensionEvent>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var farewellStr = reader.GetString("FarewellDialogue");
            var farewell = JsonSerializer.Deserialize<List<string>>(farewellStr) ?? [];

            list.Add(new AscensionEvent
            {
                Id = Guid.Parse(reader.GetString("Id")),
                FamiliarId = Guid.Parse(reader.GetString("FamiliarId")),
                FamiliarName = reader.GetString("FamiliarName"),
                FamiliarType = (FamiliarType)reader.GetInt32("FamiliarType"),
                Element = (ElementType)reader.GetInt32("Element"),
                FinalLevel = reader.GetInt32("FinalLevel"),
                FinalBondLevel = reader.GetInt32("FinalBondLevel"),
                TotalAffection = reader.GetInt32("TotalAffection"),
                Personality = (FamiliarPersonality)reader.GetInt32("Personality"),
                AscensionUTC = DateTimeOffset.Parse(reader.GetString("AscensionUTC")),
                ConstellationName = reader.GetString("ConstellationName"),
                ConstellationDescription = reader.GetString("ConstellationDescription"),
                FarewellDialogue = farewell
            });
        }
        return list;
    }

    public async Task InsertAscensionAsync(AscensionEvent evt)
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO AscensionHistory
                (Id, FamiliarId, FamiliarName, FamiliarType, Element, FinalLevel, FinalBondLevel,
                 TotalAffection, Personality, AscensionUTC, ConstellationName, ConstellationDescription, FarewellDialogue)
            VALUES
                (@id, @famId, @famName, @famType, @element, @finalLvl, @finalBond,
                 @affection, @personality, @ascended, @constName, @constDesc, @farewell);
        ";
        cmd.Parameters.AddWithValue("@id", evt.Id.ToString());
        cmd.Parameters.AddWithValue("@famId", evt.FamiliarId.ToString());
        cmd.Parameters.AddWithValue("@famName", evt.FamiliarName);
        cmd.Parameters.AddWithValue("@famType", (int)evt.FamiliarType);
        cmd.Parameters.AddWithValue("@element", (int)evt.Element);
        cmd.Parameters.AddWithValue("@finalLvl", evt.FinalLevel);
        cmd.Parameters.AddWithValue("@finalBond", evt.FinalBondLevel);
        cmd.Parameters.AddWithValue("@affection", evt.TotalAffection);
        cmd.Parameters.AddWithValue("@personality", (int)evt.Personality);
        cmd.Parameters.AddWithValue("@ascended", evt.AscensionUTC.ToString("o"));
        cmd.Parameters.AddWithValue("@constName", evt.ConstellationName);
        cmd.Parameters.AddWithValue("@constDesc", evt.ConstellationDescription);
        cmd.Parameters.AddWithValue("@farewell", JsonSerializer.Serialize(evt.FarewellDialogue));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAllAscensionsAsync()
    {
        var conn = _db.GetConnection();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM AscensionHistory";
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
