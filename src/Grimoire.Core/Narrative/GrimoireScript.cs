namespace Grimoire.Core.Narrative;

/// <summary>
/// Full narrative script for Grimoire: Astral Architect.
/// Contains all chapters, lines, and triggers from the design document.
/// This is the single source of truth for story content.
/// </summary>
public static class GrimoireScript
{
    public static List<NarrativeChapter> GetAllChapters() =>
    [
        GetPrologue(),
        GetChapter1_FirstLight(),
        GetChapter2_AlchemicalCauldron(),
        GetChapter3_VoidLeftBehind()
    ];

    /// <summary>
    /// PROLOGUE — THE FALLING SHARD
    /// </summary>
    private static NarrativeChapter GetPrologue() => new()
    {
        Number = 0,
        Title = "Prologue",
        Subtitle = "The Falling Shard",
        IsUnlocked = true,
        Lines =
        [
            new NarrativeLine
            {
                Id = "prologue_01",
                Speaker = "Narrator",
                Text = "Long before the first candle was lit, the sky held a thousand floating gardens — sanctuaries tethered to nothing but starlight, tended by architects who spoke the old runes.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "game_launch",
                DisplayDurationSeconds = 6f
            },
            new NarrativeLine
            {
                Id = "prologue_02",
                Speaker = "Narrator",
                Text = "One by one, the sanctuaries fell dark. Their keepers vanished. Their familiars scattered into the Void.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "prologue_01_complete",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "prologue_03",
                Speaker = "Narrator",
                Text = "But magic remembers its architects.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "prologue_02_complete",
                DisplayDurationSeconds = 3.5f
            },
            new NarrativeLine
            {
                Id = "prologue_04",
                Speaker = "Narrator",
                Text = "You have been called back — not as a warrior, not as a conqueror — but as a keeper of small, glowing things. A tender of shrines. A friend to creatures who have forgotten warmth.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "prologue_03_complete",
                DisplayDurationSeconds = 6f
            },
            new NarrativeLine
            {
                Id = "prologue_05",
                Speaker = "Narrator",
                Text = "Rebuild what was lost. Hatch what still dreams. Trace the old shapes, and let the sky remember your name.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "prologue_04_complete",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "prologue_06",
                Speaker = "Narrator",
                Text = "Welcome home, Architect.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "prologue_05_complete",
                DisplayDurationSeconds = 3f
            }
        ]
    };

    /// <summary>
    /// CHAPTER 1 — FIRST LIGHT
    /// Tutorial: shrine activation, familiar hatching, first spell.
    /// </summary>
    private static NarrativeChapter GetChapter1_FirstLight() => new()
    {
        Number = 1,
        Title = "Chapter 1",
        Subtitle = "First Light",
        IsUnlocked = true,
        Lines =
        [
            new NarrativeLine
            {
                Id = "ch1_01",
                Speaker = "Narrator",
                Text = "Touch the shrine. It has waited long enough.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "chapter1_start",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch1_02",
                Speaker = "Narrator",
                Text = "There — do you feel that? The Sanctuary remembers you're here.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "shrine_activated",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch1_03",
                Speaker = "Narrator",
                Text = "Something else woke up too.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "mana_shards_collected",
                DisplayDurationSeconds = 3f
            },
            new NarrativeLine
            {
                Id = "ch1_04",
                Speaker = "Narrator",
                Text = "It doesn't have a name yet. That part is yours to give.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "egg_discovered",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch1_05",
                Speaker = "Ember Wisp",
                Text = "*a soft, chiming warble — not words, but clearly a greeting*",
                Type = NarrativeType.Familiar,
                TriggerEvent = "familiar_hatched",
                DisplayDurationSeconds = 3f
            },
            new NarrativeLine
            {
                Id = "ch1_06",
                Speaker = "Narrator",
                Text = "Every familiar answers to the old shapes. Trace a circle on the sky, and see what it remembers.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "familiar_named",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "ch1_07",
                Speaker = "Narrator",
                Text = "That's a Ward. Simple, steady, protective. There are older shapes too — sharper ones — but they can wait.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "first_spell_cast",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "ch1_08",
                Speaker = "Narrator",
                Text = "One shrine lit. One familiar named. One shape remembered. The sky is already a little less empty.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "chapter1_complete",
                DisplayDurationSeconds = 5f
            }
        ]
    };

    /// <summary>
    /// CHAPTER 2 — THE ALCHEMICAL CAULDRON
    /// Tutorial: crafting, first expedition.
    /// </summary>
    private static NarrativeChapter GetChapter2_AlchemicalCauldron() => new()
    {
        Number = 2,
        Title = "Chapter 2",
        Subtitle = "The Alchemical Cauldron",
        Lines =
        [
            new NarrativeLine
            {
                Id = "ch2_01",
                Speaker = "Narrator",
                Text = "Every architect needs a cauldron — for turning raw wonder into something useful.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "chapter2_start",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch2_02",
                Speaker = "Narrator",
                Text = "Mana and Void don't like each other much. That tension is exactly what makes a potion work.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "cauldron_activated",
                DisplayDurationSeconds = 4.5f
            },
            new NarrativeLine
            {
                Id = "ch2_03",
                Speaker = "Narrator",
                Text = "Your familiar is restless. There's more of the Sanctuary out there, scattered across the Void — and it won't gather itself.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "first_potion_crafted",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "ch2_04",
                Speaker = "Narrator",
                Text = "Go rest. Build something. Time moves whether you watch it or not — that's the whole point of a home.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "first_expedition_sent",
                DisplayDurationSeconds = 5f
            }
        ]
    };

    /// <summary>
    /// CHAPTER 3 — WHAT THE VOID LEFT BEHIND
    /// Mid-game: second familiar, distant voice, mystery hook.
    /// </summary>
    private static NarrativeChapter GetChapter3_VoidLeftBehind() => new()
    {
        Number = 3,
        Title = "Chapter 3",
        Subtitle = "What the Void Left Behind",
        Lines =
        [
            new NarrativeLine
            {
                Id = "ch3_01",
                Speaker = "Narrator",
                Text = "Look at that. It found more than shards out there.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "first_expedition_returned",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch3_02",
                Speaker = "Narrator",
                Text = "Not every familiar wakes up eager. Some take longer to trust the light again.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "second_egg_discovered",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "ch3_03",
                Speaker = "Distant Voice",
                Text = "...the sky forgot us first...",
                Type = NarrativeType.DistantVoice,
                TriggerEvent = "second_egg_hatched",
                DisplayDurationSeconds = 4f
            },
            new NarrativeLine
            {
                Id = "ch3_04",
                Speaker = "Narrator",
                Text = "That wasn't your familiar. Something else out there still remembers falling.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "distant_voice_heard",
                DisplayDurationSeconds = 5f
            },
            new NarrativeLine
            {
                Id = "ch3_05",
                Speaker = "Narrator",
                Text = "Whatever it is, it's not for tonight. Tonight, just build. The sky will still be listening tomorrow.",
                Type = NarrativeType.Narrator,
                TriggerEvent = "chapter3_complete",
                DisplayDurationSeconds = 5.5f
            }
        ]
    };
}
