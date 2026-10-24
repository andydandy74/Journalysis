using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents one detected model-load sequence in a parsed journal.
/// </summary>
/// <remarks>
/// A sequence is built from one opening event and, when detected, one load-completion event.
/// Additional derived metadata (for example <see cref="Version"/> and
/// <see cref="VersionHistory"/>) is read from comments between the opening and loaded blocks.
/// </remarks>
public sealed class LoadedModel
{
    /// <summary>
    /// Initializes a detected loaded-model sequence.
    /// </summary>
    /// <param name="journal">Parent journal containing the source records.</param>
    /// <param name="path">Detected model path, when available.</param>
    /// <param name="initialEvent">Opening event that starts this sequence.</param>
    public LoadedModel(Journal journal, string? path, LoadedModelEvent initialEvent)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(initialEvent);

        Journal = journal;
        Path = path;
        Events = new List<LoadedModelEvent> { initialEvent };
    }

    private string? _version;
    private IReadOnlyList<string>? _versionHistory;

    /// <summary>
    /// Gets the journal that produced this loaded-model sequence.
    /// </summary>
    public Journal Journal { get; }

    /// <summary>
    /// Gets the detected model path, when available.
    /// </summary>
    /// <remarks>
    /// Path normalization and path-source selection are performed by the analysis layer.
    /// </remarks>
    public string? Path { get; }

    /// <summary>
    /// Gets the ordered lifecycle events for this model load.
    /// </summary>
    /// <remarks>
    /// The first entry is expected to be an open event.
    /// When available, the second entry is typically a <see cref="LoadedModelEventType.Loaded"/> marker.
    /// </remarks>
    public List<LoadedModelEvent> Events { get; }

    public override string ToString() => nameof(LoadedModel);

    /// <summary>
    /// Gets the first comment text containing <c>":&lt; File was saved in"</c>
    /// between the opening and loaded blocks.
    /// </summary>
    /// <remarks>
    /// The value is computed on first access and then cached.
    /// This property assumes the sequence contains at least two events and that the
    /// expected comment exists in the block range.
    /// </remarks>
    public string Version =>
        _version ??= GetLinesBetweenOpenAndLoadedBlocks()
            .OfType<JournalCommentLine>()
            .First(x => x.RawText.Contains(":< File was saved in", StringComparison.Ordinal))
            .RawText;

    /// <summary>
    /// Gets save-history comment lines between the first two
    /// <c>":&lt; Document save history"</c> markers in the open→loaded range.
    /// </summary>
    /// <remarks>
    /// The value is computed on first access and then cached.
    /// This property assumes both markers exist in the block range.
    /// </remarks>
    public IReadOnlyList<string> VersionHistory =>
        _versionHistory ??= GetVersionHistoryInternal();

    private IReadOnlyList<string> GetVersionHistoryInternal()
    {
        var linesInRange = GetLinesBetweenOpenAndLoadedBlocks().ToArray();

        var saveHistoryMarkers = linesInRange
            .OfType<JournalCommentLine>()
            .Where(x => x.RawText.Contains(":< Document save history", StringComparison.Ordinal))
            .Select(x => x.Number)
            .ToArray();

        var start = saveHistoryMarkers[0] + 1;
        var end = saveHistoryMarkers[1];

        return linesInRange
            .OfType<JournalCommentLine>()
            .Where(x => x.Number >= start && x.Number < end)
            .Select(x => x.RawText.Split(":<", StringSplitOptions.None)[^1].Trim())
            .ToArray();
    }

    private IReadOnlyList<JournalLine> GetLinesBetweenOpenAndLoadedBlocks()
    {
        // Get the range of blocks between the first two events.
        var fromBlock = Events[0].JournalLine.Block;
        var toBlock = Events[1].JournalLine.Block;

        var blockCount = toBlock - fromBlock;
        if (blockCount <= 0)
        {
            return Array.Empty<JournalLine>();
        }

        return Journal.GetLinesByBlocks(Enumerable.Range(fromBlock, blockCount));
    }
}

/// <summary>
/// Represents one loaded-model lifecycle event derived from a journal line.
/// </summary>
public sealed class LoadedModelEvent
{
    /// <summary>
    /// Initializes a loaded-model event.
    /// </summary>
    /// <param name="journalLine">Source journal line that triggered this event.</param>
    /// <param name="type">Event type (open method or load-completion marker).</param>
    /// <param name="dateTime">Event timestamp, when available.</param>
    /// <exception cref="ArgumentNullException"><paramref name="journalLine"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined enum value.</exception>
    public LoadedModelEvent(
        JournalLine journalLine,
        LoadedModelEventType type,
        DateTime? dateTime)
    {
        ArgumentNullException.ThrowIfNull(journalLine);

        // An enum can still be cast from an undefined integer value.
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type), type, "The event type must be a defined value.");
        }

        JournalLine = journalLine;
        Type = type;
        DateTime = dateTime;
    }

    /// <summary>
    /// Gets the source journal line that triggered this event.
    /// </summary>
    public JournalLine JournalLine { get; }

    /// <summary>
    /// Gets the model-opening method or load-completion event type.
    /// </summary>
    public LoadedModelEventType Type { get; }

    /// <summary>
    /// Gets the event timestamp, when available.
    /// </summary>
    public DateTime? DateTime { get; }

    public override string ToString() => nameof(LoadedModelEvent);
}