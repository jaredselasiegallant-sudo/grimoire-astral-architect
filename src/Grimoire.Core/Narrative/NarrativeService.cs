namespace Grimoire.Core.Narrative;

/// <summary>
/// Manages narrative progression, tracking which lines have been shown
/// and providing the next line to display when a trigger event fires.
/// </summary>
public sealed class NarrativeService
{
    private readonly List<NarrativeChapter> _chapters;
    private readonly HashSet<string> _shownLines = [];
    private readonly Queue<NarrativeLine> _pendingDisplay = [];

    /// <summary>Fired when a new line should be displayed on screen.</summary>
    public event Action<NarrativeLine>? OnNarrativeReady;

    /// <summary>Fired when a chapter is completed.</summary>
    public event Action<NarrativeChapter>? OnChapterCompleted;

    public IReadOnlyList<NarrativeChapter> Chapters => _chapters;

    public NarrativeService()
    {
        _chapters = GrimoireScript.GetAllChapters();
    }

    /// <summary>
    /// Call this whenever a gameplay event occurs.
    /// If the event matches a narrative trigger, the corresponding line is queued.
    /// </summary>
    public void FireEvent(string eventName)
    {
        foreach (var chapter in _chapters.Where(c => c.IsUnlocked))
        {
            foreach (var line in chapter.Lines)
            {
                if (line.TriggerEvent == eventName && !_shownLines.Contains(line.Id))
                {
                    _pendingDisplay.Enqueue(line);
                }
            }

            // Check if all lines in the chapter have been triggered
            if (chapter.Lines.All(l => _shownLines.Contains(l.Id)) && !chapter.IsComplete)
            {
                chapter.IsComplete = true;
                OnChapterCompleted?.Invoke(chapter);

                // Unlock next chapter
                var next = _chapters.FirstOrDefault(c => c.Number == chapter.Number + 1);
                if (next is not null)
                    next.IsUnlocked = true;
            }
        }

        // Process display queue
        ProcessDisplayQueue();
    }

    /// <summary>Mark a line as displayed (called after the UI finishes showing it).</summary>
    public void MarkLineShown(string lineId)
    {
        _shownLines.Add(lineId);

        // Fire the "line_complete" event so chained triggers work
        FireEvent($"{lineId}_complete");
    }

    /// <summary>Get the next line waiting to be displayed, or null.</summary>
    public NarrativeLine? GetNextPending()
    {
        return _pendingDisplay.Count > 0 ? _pendingDisplay.Dequeue() : null;
    }

    /// <summary>Check if a specific line has already been shown.</summary>
    public bool HasLineBeenShown(string lineId) => _shownLines.Contains(lineId);

    /// <summary>Serialise shown lines for save/load persistence.</summary>
    public List<string> GetShownLineIds() => [.. _shownLines];

    /// <summary>Restore shown lines from a save.</summary>
    public void RestoreShownLines(IEnumerable<string> lineIds)
    {
        foreach (var id in lineIds)
            _shownLines.Add(id);
    }

    private void ProcessDisplayQueue()
    {
        while (_pendingDisplay.Count > 0)
        {
            var line = _pendingDisplay.Peek();
            OnNarrativeReady?.Invoke(line);
            break; // Show one at a time; MarkLineShown will re-trigger processing
        }
    }
}
