using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Grimoire.Data.Database;

/// <summary>
/// Creates the SQLite schema and handles migrations between versions.
/// v1: Base schema (GameState, Familiars, InventoryItems, SanctuaryBuildings)
/// v2: Eggs, Recipes, ExpeditionLog, NarrativeProgress, TutorialProgress
/// v3: Bonds, Grimoire, Corruption, Cosmetics, PlayerSpells, Accessibility, AstralEvents
/// v4: Weather, GestureSignature, MemoryEchoes, ArchitecturalRituals, Constellations, AscensionHistory, SanctuaryChronicle
/// </summary>
public static class DatabaseInitializer
{
    private const int CurrentSchemaVersion = 4;

    public static async Task InitialiseAsync(SqliteConnection connection, ILogger? logger = null)
    {
        await CreateMetaTableAsync(connection);
        var version = await GetSchemaVersionAsync(connection);

        if (version == 0)
        {
            logger?.LogInformation("First launch - creating initial schema.");
            await CreateV1SchemaAsync(connection);
            await SeedDefaultDataAsync(connection);
            await SetSchemaVersionAsync(connection, 1);
            version = 1;
        }

        if (version < CurrentSchemaVersion)
        {
            logger?.LogInformation("Migrating schema from v{Version} to v{Target}.", version, CurrentSchemaVersion);
            await RunMigrationsAsync(connection, version, logger);
        }
    }

    private static async Task CreateMetaTableAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS _Meta (Key TEXT PRIMARY KEY, Value TEXT NOT NULL);";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> GetSchemaVersionAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM _Meta WHERE Key = 'SchemaVersion'";
        var result = await cmd.ExecuteScalarAsync();
        return result is null ? 0 : int.Parse(result.ToString()!);
    }

    private static async Task SetSchemaVersionAsync(SqliteConnection connection, int version)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO _Meta (Key, Value) VALUES ('SchemaVersion', @v)";
        cmd.Parameters.AddWithValue("@v", version.ToString());
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RunMigrationsAsync(SqliteConnection connection, int fromVersion, ILogger? logger)
    {
        if (fromVersion < 2)
        {
            logger?.LogInformation("Running migration v1 to v2");
            await RunMigration_V2(connection);
            await SetSchemaVersionAsync(connection, 2);
        }
        if (fromVersion < 3)
        {
            logger?.LogInformation("Running migration v2 to v3");
            await RunMigration_V3(connection);
            await SetSchemaVersionAsync(connection, 3);
        }
        if (fromVersion < 4)
        {
            logger?.LogInformation("Running migration v3 to v4");
            await RunMigration_V4(connection);
            await SetSchemaVersionAsync(connection, 4);
        }
    }

    private static async Task RunMigration_V2(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS FamiliarEggs (
                Id TEXT PRIMARY KEY, HatchesInto INTEGER NOT NULL, Element INTEGER NOT NULL,
                Rarity INTEGER NOT NULL DEFAULT 0, HatchDurationSeconds INTEGER NOT NULL DEFAULT 300,
                IncubationStartUTC TEXT, HatchReadyUTC TEXT,
                IsIncubating INTEGER NOT NULL DEFAULT 0, HasHatched INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS CraftingRecipes (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Description TEXT NOT NULL DEFAULT '',
                IngredientAElement INTEGER NOT NULL, IngredientBElement INTEGER NOT NULL,
                IngredientAMinRarity INTEGER NOT NULL DEFAULT 0, IngredientBMinRarity INTEGER NOT NULL DEFAULT 0,
                OutputName TEXT NOT NULL, OutputElement INTEGER NOT NULL, OutputManaPower INTEGER NOT NULL DEFAULT 0,
                OutputRarity INTEGER NOT NULL DEFAULT 0, OutputQuantity INTEGER NOT NULL DEFAULT 1,
                IsDiscovered INTEGER NOT NULL DEFAULT 0, IsUnlocked INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS ExpeditionLog (
                Id TEXT PRIMARY KEY, FamiliarName TEXT NOT NULL, DepartedUTC TEXT NOT NULL,
                ReturnedUTC TEXT NOT NULL, DurationTicks INTEGER NOT NULL, Success INTEGER NOT NULL DEFAULT 1,
                ManaCrystalsEarned INTEGER NOT NULL DEFAULT 0, ExperienceEarned INTEGER NOT NULL DEFAULT 0,
                ItemNames TEXT NOT NULL DEFAULT '[]', NarrativeNote TEXT
            );
            CREATE TABLE IF NOT EXISTS NarrativeProgress (LineId TEXT PRIMARY KEY, ShownUTC TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS TutorialProgress (StepId TEXT PRIMARY KEY, CompletedUTC TEXT NOT NULL);
            ALTER TABLE GameState ADD COLUMN SchemaVersion INTEGER NOT NULL DEFAULT 2;
            ALTER TABLE GameState ADD COLUMN ManaRegenRate INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE GameState ADD COLUMN TutorialCompleted INTEGER NOT NULL DEFAULT 0;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RunMigration_V3(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS FamiliarBonds (
                FamiliarId TEXT PRIMARY KEY, Happiness INTEGER NOT NULL DEFAULT 50,
                Trust INTEGER NOT NULL DEFAULT 20, Affection INTEGER NOT NULL DEFAULT 0,
                Personality INTEGER NOT NULL DEFAULT 0, PreferredActivity INTEGER NOT NULL DEFAULT 0,
                SessionInteractions INTEGER NOT NULL DEFAULT 0, LastInteractionUTC TEXT,
                UnlockedDialogues TEXT NOT NULL DEFAULT '[]'
            );
            CREATE TABLE IF NOT EXISTS GrimoireEntries (
                EntryId TEXT PRIMARY KEY, IsUnlocked INTEGER NOT NULL DEFAULT 0,
                UnlockedUTC TEXT, Category INTEGER NOT NULL DEFAULT 0, SortOrder INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS CorruptionState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1), CorruptionLevel INTEGER NOT NULL DEFAULT 0,
                BaseDecayRate REAL NOT NULL DEFAULT 0.5
            );
            CREATE TABLE IF NOT EXISTS CosmeticItems (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Description TEXT NOT NULL DEFAULT '',
                Type INTEGER NOT NULL, ColourHex TEXT, EffectId TEXT,
                IsUnlocked INTEGER NOT NULL DEFAULT 0, IsEquipped INTEGER NOT NULL DEFAULT 0,
                UnlockRequirement TEXT
            );
            CREATE TABLE IF NOT EXISTS CosmeticLoadout (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                ActiveShrineSkin TEXT, ActiveParticleColour TEXT, ActiveFamiliarAccessory TEXT,
                ActiveSkyboxTint TEXT, ActiveTrailStyle TEXT, ActiveSanctuaryTheme TEXT
            );
            CREATE TABLE IF NOT EXISTS PlayerSpells (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, ReferenceShape TEXT NOT NULL DEFAULT '[]',
                DetectedArchetype INTEGER NOT NULL, PowerModifier REAL NOT NULL DEFAULT 1.0,
                Element INTEGER NOT NULL, CastCount INTEGER NOT NULL DEFAULT 0,
                CreatedUTC TEXT NOT NULL, IsEquipped INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS AccessibilitySettings (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                GestureAssist INTEGER NOT NULL DEFAULT 0, ColorBlindMode INTEGER NOT NULL DEFAULT 0,
                TextToSpeechEnabled INTEGER NOT NULL DEFAULT 0, IdlePacingMultiplier REAL NOT NULL DEFAULT 1.0,
                HighContrastMode INTEGER NOT NULL DEFAULT 0, ReducedMotion INTEGER NOT NULL DEFAULT 0,
                SubtitleScale REAL NOT NULL DEFAULT 1.0, PersistentTrail INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS AstralEvents (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Description TEXT NOT NULL DEFAULT '',
                EventType INTEGER NOT NULL, Frequency INTEGER NOT NULL,
                DurationHours INTEGER NOT NULL, StartUTC TEXT NOT NULL, EndUTC TEXT NOT NULL,
                SkyTintHex TEXT NOT NULL DEFAULT '#FFFFFF'
            );
            ALTER TABLE GameState ADD COLUMN SanctuaryLevel INTEGER NOT NULL DEFAULT 1;
            ALTER TABLE GameState ADD COLUMN TotalExpeditionsCompleted INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE GameState ADD COLUMN TotalSpellsCast INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE GameState ADD COLUMN TotalRecipesDiscovered INTEGER NOT NULL DEFAULT 0;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task RunMigration_V4(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS WeatherState (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                CurrentWeather INTEGER NOT NULL DEFAULT 0,
                Intensity REAL NOT NULL DEFAULT 0.0,
                StartedUTC TEXT NOT NULL,
                DurationTicks INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS GestureSignature (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                SampleCount INTEGER NOT NULL DEFAULT 0,
                AverageSpeed REAL NOT NULL DEFAULT 0.0,
                AverageAngularVelocity REAL NOT NULL DEFAULT 0.0,
                PreferredStartAngle REAL NOT NULL DEFAULT 0.0,
                AverageDuration REAL NOT NULL DEFAULT 0.0,
                AveragePauseBetweenGestures REAL NOT NULL DEFAULT 0.0,
                ShapeFrequency TEXT NOT NULL DEFAULT '{}',
                TimingHistogram TEXT NOT NULL DEFAULT '{}'
            );
            CREATE TABLE IF NOT EXISTS MemoryEchoes (
                Id TEXT PRIMARY KEY,
                GridX INTEGER NOT NULL DEFAULT 0,
                GridY INTEGER NOT NULL DEFAULT 0,
                RestoredBuilding INTEGER NOT NULL DEFAULT 0,
                DurationSeconds REAL NOT NULL DEFAULT 0.0,
                HasBeenWitnessed INTEGER NOT NULL DEFAULT 0,
                FirstWitnessedUTC TEXT NOT NULL,
                GhostTintHex TEXT NOT NULL DEFAULT '#FFFFFF'
            );
            CREATE TABLE IF NOT EXISTS ArchitecturalRituals (
                Name TEXT PRIMARY KEY,
                DiscoveredUTC TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Constellations (
                Name TEXT PRIMARY KEY,
                Description TEXT NOT NULL DEFAULT '',
                Element INTEGER NOT NULL DEFAULT 0,
                AscendedUTC TEXT NOT NULL,
                OriginalFamiliarName TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS AscensionHistory (
                Id TEXT PRIMARY KEY,
                FamiliarId TEXT NOT NULL,
                FamiliarName TEXT NOT NULL,
                FamiliarType INTEGER NOT NULL DEFAULT 0,
                Element INTEGER NOT NULL DEFAULT 0,
                FinalLevel INTEGER NOT NULL DEFAULT 1,
                FinalBondLevel INTEGER NOT NULL DEFAULT 0,
                TotalAffection INTEGER NOT NULL DEFAULT 0,
                Personality INTEGER NOT NULL DEFAULT 0,
                AscensionUTC TEXT NOT NULL,
                ConstellationName TEXT NOT NULL DEFAULT '',
                ConstellationDescription TEXT NOT NULL DEFAULT '',
                FarewellDialogue TEXT NOT NULL DEFAULT ''
            );
            CREATE TABLE IF NOT EXISTS SanctuaryChronicle (
                Id INTEGER PRIMARY KEY CHECK (Id = 1),
                LastGeneratedUTC TEXT NOT NULL,
                CurrentPage TEXT NOT NULL DEFAULT ''
            );
            ALTER TABLE GameState ADD COLUMN FirstBuildingName TEXT NOT NULL DEFAULT '';
            ALTER TABLE GameState ADD COLUMN FirstBuildingId TEXT NOT NULL DEFAULT '';
            ALTER TABLE GameState ADD COLUMN FirstFamiliarName TEXT NOT NULL DEFAULT '';
            ALTER TABLE GameState ADD COLUMN FirstFamiliarId TEXT NOT NULL DEFAULT '';
            ALTER TABLE GameState ADD COLUMN TotalDuetCasts INTEGER NOT NULL DEFAULT 0;
            ALTER TABLE GameState ADD COLUMN SessionMusicalHarmony REAL NOT NULL DEFAULT 0.5;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateV1SchemaAsync(SqliteConnection connection)
    {
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS GameState (
                PlayerId TEXT PRIMARY KEY, PlayerName TEXT NOT NULL DEFAULT 'Architect',
                ManaCrystals INTEGER NOT NULL DEFAULT 0, TotalPlayTimeSeconds INTEGER NOT NULL DEFAULT 0,
                LastSaveUTC TEXT NOT NULL, FirstLaunchUTC TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Familiars (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Type INTEGER NOT NULL, Element INTEGER NOT NULL,
                Rarity INTEGER NOT NULL DEFAULT 0, Level INTEGER NOT NULL DEFAULT 1, Experience INTEGER NOT NULL DEFAULT 0,
                MaxHealth INTEGER NOT NULL DEFAULT 100, CurrentHealth INTEGER NOT NULL DEFAULT 100,
                GatheringBonus REAL NOT NULL DEFAULT 1.0, LastExpeditionUTC TEXT,
                ExpeditionDurationTicks INTEGER NOT NULL DEFAULT 0, IsOnExpedition INTEGER NOT NULL DEFAULT 0,
                ExpeditionReturnUTC TEXT
            );
            CREATE TABLE IF NOT EXISTS InventoryItems (
                Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Description TEXT NOT NULL DEFAULT '',
                Element INTEGER NOT NULL, Rarity INTEGER NOT NULL DEFAULT 0,
                Quantity INTEGER NOT NULL DEFAULT 1, ManaPower INTEGER NOT NULL DEFAULT 0,
                ValidForBuilding INTEGER
            );
            CREATE TABLE IF NOT EXISTS SanctuaryBuildings (
                Id TEXT PRIMARY KEY, Type INTEGER NOT NULL, Name TEXT NOT NULL,
                GridX INTEGER NOT NULL, GridY INTEGER NOT NULL, Level INTEGER NOT NULL DEFAULT 1,
                MaxLevel INTEGER NOT NULL DEFAULT 10, ManaPerSecond REAL NOT NULL DEFAULT 0.0,
                HabitatSlots INTEGER NOT NULL DEFAULT 0, UpgradeFinishUTC TEXT, LastProductionUTC TEXT
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task SeedDefaultDataAsync(SqliteConnection connection)
    {
        var check = connection.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM GameState";
        var count = (long)(await check.ExecuteScalarAsync() ?? 0L);
        if (count > 0) return;

        var now = DateTimeOffset.UtcNow.ToString("o");
        var seed = connection.CreateCommand();
        seed.CommandText = $"INSERT INTO GameState (PlayerId, PlayerName, ManaCrystals, TotalPlayTimeSeconds, LastSaveUTC, FirstLaunchUTC) VALUES ('{Guid.NewGuid()}', 'Architect', 100, 0, '{now}', '{now}');";
        await seed.ExecuteNonQueryAsync();

        // Seed default accessibility settings
        var accSeed = connection.CreateCommand();
        accSeed.CommandText = "INSERT OR IGNORE INTO AccessibilitySettings (Id) VALUES (1);";
        await accSeed.ExecuteNonQueryAsync();

        // Seed default cosmetic loadout
        var cosSeed = connection.CreateCommand();
        cosSeed.CommandText = "INSERT OR IGNORE INTO CosmeticLoadout (Id) VALUES (1);";
        await cosSeed.ExecuteNonQueryAsync();

        // Seed empty corruption state
        var corSeed = connection.CreateCommand();
        corSeed.CommandText = "INSERT OR IGNORE INTO CorruptionState (Id, CorruptionLevel) VALUES (1, 0);";
        await corSeed.ExecuteNonQueryAsync();

        // Seed default weather state
        var weatherSeed = connection.CreateCommand();
        weatherSeed.CommandText = $"INSERT OR IGNORE INTO WeatherState (Id, CurrentWeather, Intensity, StartedUTC, DurationTicks) VALUES (1, 0, 0.0, '{now}', 0);";
        await weatherSeed.ExecuteNonQueryAsync();

        // Seed default gesture signature
        var gestureSeed = connection.CreateCommand();
        gestureSeed.CommandText = "INSERT OR IGNORE INTO GestureSignature (Id, SampleCount, AverageSpeed, AverageAngularVelocity, PreferredStartAngle, AverageDuration, AveragePauseBetweenGestures, ShapeFrequency, TimingHistogram) VALUES (1, 0, 0.0, 0.0, 0.0, 0.0, 0.0, '{}', '{}');";
        await gestureSeed.ExecuteNonQueryAsync();

        // Seed default sanctuary chronicle
        var chronicleSeed = connection.CreateCommand();
        chronicleSeed.CommandText = $"INSERT OR IGNORE INTO SanctuaryChronicle (Id, LastGeneratedUTC, CurrentPage) VALUES (1, '{now}', '');";
        await chronicleSeed.ExecuteNonQueryAsync();
    }
}
