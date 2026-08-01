namespace Grimoire.Core.Enums;

/// <summary>Seasons derived from real-world calendar dates.</summary>
public enum Season
{
    Spring,  // March-May
    Summer,  // June-August
    Autumn,  // September-November
    Winter   // December-February
}

/// <summary>Time of day zones for skybox and soundscape.</summary>
public enum TimeOfDay
{
    DeepNight,  // 0-4
    Dawn,       // 4-7
    Morning,    // 7-12
    Afternoon,  // 12-17
    Dusk,       // 17-19
    Night       // 19-24
}
