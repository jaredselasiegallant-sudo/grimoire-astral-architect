namespace Grimoire.Core.Chronicle;

/// <summary>
/// The Sanctuary Chronicle — auto-generates a short narrative prose page
/// summarizing the sanctuary's current state in the Grimoire's codex voice.
/// Players can export/screenshot this as a personalized epilogue.
/// </summary>
public static class ChronicleGenerator
{
    private static readonly string[] OpeningLines =
    [
        "The sanctuary breathes softly under the {season} sky.",
        "Starlight pools in familiar patterns across the {season} grounds.",
        "A quiet {timeOfDay} settles over the sanctuary.",
        "The air hums with {season} energy, thick and warm."
    ];

    private static readonly string[] BuildingLines =
    [
        "{count} structure{s} stand{s} where once there was only silence.",
        "The architect has raised {count} testament{s} to what was lost — and what is being found again.",
        "{count} building{s} pulse{s} with quiet purpose."
    ];

    private static readonly string[] FamiliarLines =
    [
        "{count} familiar{s} drift{s} through the corridors of light.",
        "The sanctuary is home to {count} soul{s} that have chosen to stay.",
        "{count} glow{s} — small, certain, alive — move{s} through the gardens."
    ];

    private static readonly string[] CorruptionLines =
    [
        "The Void presses at the edges, but the lights hold.",
        "Faint corruption lingers in the corners, a reminder of what was.",
        "The sanctuary stands clean and bright, the Void held at bay."
    ];

    private static readonly string[] ClosingLines =
    [
        "This is not the end of the story. It is the part where the story remembers how to begin.",
        "The sky watches. The sky waits. The sky remembers your name.",
        "Tomorrow, the stars will shift again. The sanctuary will be here.",
        "What was forgotten is being remembered, one small light at a time."
    ];

    /// <summary>
    /// Generate a chronicle page summarizing the current sanctuary state.
    /// </summary>
    public static string Generate(ChronicleContext ctx)
    {
        var rng = new Random(ctx.Seed);
        var paragraphs = new List<string>();

        // Opening
        var opening = PickRandom(OpeningLines, rng)
            .Replace("{season}", ctx.Season.ToLowerInvariant())
            .Replace("{timeOfDay}", ctx.TimeOfDay.ToLowerInvariant());
        paragraphs.Add(ParagraphCase(opening));

        // Building summary
        if (ctx.BuildingCount > 0)
        {
            var buildingLine = PickRandom(BuildingLines, rng)
                .Replace("{count}", ctx.BuildingCount.ToString())
                .Replace("{s}", ctx.BuildingCount == 1 ? "" : "s");
            paragraphs.Add(ParagraphCase(buildingLine));
        }

        // First building memory
        if (ctx.FirstBuildingName is not null)
        {
            paragraphs.Add($"The first to rise was the {ctx.FirstBuildingName} — placed with uncertain hands, in uncertain light. It still glows a little brighter than the rest.");
        }

        // Familiar summary
        if (ctx.FamiliarCount > 0)
        {
            var familiarLine = PickRandom(FamiliarLines, rng)
                .Replace("{count}", ctx.FamiliarCount.ToString())
                .Replace("{s}", ctx.FamiliarCount == 1 ? "" : "s")
                .Replace("{n}", ctx.FamiliarCount == 1 ? "has" : "have");
            paragraphs.Add(ParagraphCase(familiarLine));
        }

        // First familiar memory
        if (ctx.FirstFamiliarName is not null)
        {
            paragraphs.Add($"The first to be named was {ctx.FirstFamiliarName}. The others seem to look to it, sometimes, as if asking whether it is safe to stay. It always says yes.");
        }

        // Bond level
        if (ctx.HighestBondLevel > 5)
        {
            paragraphs.Add($"One familiar has reached a bond of extraordinary depth — level {ctx.HighestBondLevel}. It no longer follows. It chooses to be near.");
        }

        // Corruption
        if (ctx.CorruptionLevel > 30)
        {
            paragraphs.Add(PickRandom(CorruptionLines, rng));
        }
        else if (ctx.CorruptionLevel < 10)
        {
            paragraphs.Add("The sanctuary is pristine. Not a trace of the Void remains within its borders.");
        }

        // Grimoire progress
        if (ctx.GrimoirePercent > 0)
        {
            paragraphs.Add($"The Grimoire is {ctx.GrimoirePercent:F0}% complete — {(ctx.GrimoirePercent > 70 ? "most secrets have been uncovered" : "many pages still wait to be filled")}.{(ctx.GrimoirePercent > 90 ? " Almost nothing remains unknown." : "")}");
        }

        // Expedition history
        if (ctx.TotalExpeditions > 0)
        {
            paragraphs.Add($"{ctx.TotalExpeditions} expedition{(ctx.TotalExpeditions == 1 ? "" : "s")} have returned with stories from beyond the sanctuary's edge.");
        }

        // Closing
        paragraphs.Add(PickRandom(ClosingLines, rng));

        return string.Join("\n\n", paragraphs);
    }

    private static string PickRandom(string[] options, Random rng) => options[rng.Next(options.Length)];

    private static string ParagraphCase(string s) =>
        char.ToUpper(s[0]) + s[1..];
}

/// <summary>Data needed to generate a chronicle page.</summary>
public sealed class ChronicleContext
{
    public string Season { get; init; } = "Spring";
    public string TimeOfDay { get; init; } = "Morning";
    public int BuildingCount { get; init; }
    public string? FirstBuildingName { get; init; }
    public int FamiliarCount { get; init; }
    public string? FirstFamiliarName { get; init; }
    public int HighestBondLevel { get; init; }
    public int CorruptionLevel { get; init; }
    public double GrimoirePercent { get; init; }
    public int TotalExpeditions { get; init; }
    public int TotalSpellsCast { get; init; }
    public int Seed { get; init; }
}
