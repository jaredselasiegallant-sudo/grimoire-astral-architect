using SkiaSharp;

namespace Grimoire.Engine.Music;

/// <summary>
/// Musical Spellcasting — successful gesture traces contribute a note/chord
/// to a generative ambient score. An active building session literally
/// composes music in real time.
/// 
/// Better/faster gestures produce more harmonic sounds.
/// This is a soft skill-expression layer disguised as ambience.
/// </summary>
public sealed class MusicalSpellcaster
{
    private readonly List<MusicalNote> _activeNotes = [];
    private readonly List<ChordProgression> _progressions = [];
    private float _harmonyLevel = 0.5f; // 0 = dissonant, 1 = perfectly harmonic
    private int _totalNotesPlayed;

    // Musical scales (MIDI note offsets from root)
    private static readonly int[] MajorScale = [0, 2, 4, 5, 7, 9, 11];
    private static readonly int[] PentatonicScale = [0, 2, 4, 7, 9];
    private static readonly int[] MinorScale = [0, 2, 3, 5, 7, 8, 10];

    // Gesture-to-note mapping
    private static readonly Dictionary<string, int[]> GestureNotes = new()
    {
        ["Circle"] = [0, 4, 7],       // Major triad (C-E-G)
        ["Triangle"] = [0, 3, 7],     // Minor triad (C-Eb-G)
        ["Line"] = [0, 7],            // Perfect fifth (C-G)
        ["Zigzag"] = [0, 4, 7, 11],  // Major seventh
        ["Spiral"] = [0, 3, 7, 10]   // Minor seventh
    };

    /// <summary>Current harmony level (0-1). Higher = more consonant.</summary>
    public float HarmonyLevel => _harmonyLevel;

    /// <summary>Total notes played this session.</summary>
    public int TotalNotesPlayed => _totalNotesPlayed;

    /// <summary>
    /// Called when a gesture is successfully cast.
    /// Generates musical notes based on gesture quality and type.
    /// </summary>
    public MusicalEvent OnSpellCast(string gestureName, float gestureQuality)
    {
        // Better quality = more notes in the chord
        var baseNotes = GestureNotes.GetValueOrDefault(gestureName, [0]);
        var noteCount = gestureQuality > 0.8f ? baseNotes.Length :
                        gestureQuality > 0.5f ? Math.Max(1, baseNotes.Length - 1) : 1;

        var notes = new List<MusicalNote>();
        var rootOctave = 60; // Middle C (MIDI)

        for (int i = 0; i < noteCount; i++)
        {
            var midiNote = rootOctave + baseNotes[i % baseNotes.Length];
            var velocity = 0.3f + gestureQuality * 0.5f;
            var duration = 1.0f + gestureQuality * 2.0f; // seconds

            var note = new MusicalNote
            {
                MidiNote = midiNote,
                Velocity = velocity,
                Duration = duration,
                TimeRemaining = duration,
                GestureSource = gestureName
            };

            _activeNotes.Add(note);
            notes.Add(note);
            _totalNotesPlayed++;
        }

        // Harmony evolves based on consistency
        _harmonyLevel = Math.Clamp(
            _harmonyLevel + (gestureQuality - 0.5f) * 0.1f,
            0f, 1f);

        return new MusicalEvent
        {
            Notes = notes,
            HarmonyLevel = _harmonyLevel,
            IsConsonant = _harmonyLevel > 0.6f
        };
    }

    /// <summary>Update active notes (decay, remove expired).</summary>
    public void Update(float deltaTime)
    {
        for (int i = _activeNotes.Count - 1; i >= 0; i--)
        {
            _activeNotes[i].TimeRemaining -= deltaTime;
            if (_activeNotes[i].TimeRemaining <= 0)
                _activeNotes.RemoveAt(i);
        }

        // Harmony slowly drifts back toward neutral
        _harmonyLevel += (0.5f - _harmonyLevel) * deltaTime * 0.1f;
    }

    /// <summary>Get the current chord being played.</summary>
    public string GetCurrentChord()
    {
        if (_activeNotes.Count == 0) return "silence";

        var midiNotes = _activeNotes.Select(n => n.MidiNote % 12).Distinct().Order().ToList();
        if (midiNotes.Count == 1) return NoteName(midiNotes[0]);
        if (midiNotes.Count == 2) return $"{NoteName(midiNotes[0])} + {NoteName(midiNotes[1])}";

        return string.Join(" + ", midiNotes.Select(NoteName));
    }

    private static string NoteName(int midiNote) => midiNote switch
    {
        0 => "C", 1 => "C#", 2 => "D", 3 => "D#",
        4 => "E", 5 => "F", 6 => "F#", 7 => "G",
        8 => "G#", 9 => "A", 10 => "A#", 11 => "B",
        _ => "?"
    };
}

public sealed class MusicalNote
{
    public int MidiNote { get; init; }
    public float Velocity { get; init; }
    public float Duration { get; init; }
    public float TimeRemaining { get; set; }
    public required string GestureSource { get; init; }
}

public sealed class MusicalEvent
{
    public List<MusicalNote> Notes { get; init; } = [];
    public float HarmonyLevel { get; init; }
    public bool IsConsonant { get; init; }
}

public sealed class ChordProgression
{
    public string Name { get; init; } = "";
    public int[] ChordRoots { get; init; } = [];
    public float Tempo { get; init; } = 60f;
}
