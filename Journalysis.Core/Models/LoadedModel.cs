using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents one model load detected in a journal and its related events.
/// </summary>
public sealed class LoadedModel
{
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
    /// Gets the model path as provided by the analysis layer, if available.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Gets the ordered event sequence for this model load.
    /// First entry is expected to be an open event, second is typically "Loaded".
    /// </summary>
    public List<LoadedModelEvent> Events { get; }

    public override string ToString() => nameof(LoadedModel);

    /// <summary>
    /// Gets the first comment that contains "File was saved in" between open and loaded blocks.
    /// The value is computed on first access and then cached.
    /// </summary>
    public string Version =>
        _version ??= GetLinesBetweenOpenAndLoadedBlocks()
            .OfType<JournalCommentLine>()
            .First(x => x.RawText.Contains(":< File was saved in", StringComparison.Ordinal))
            .RawText;

    /// <summary>
    /// Gets comment text between the first two "Document save history" markers
    /// between open and loaded blocks.
    /// The value is computed on first access and then cached.
    /// </summary>
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
    /// Gets the event timestamp when available.
    /// </summary>
    public DateTime? DateTime { get; }

    public override string ToString() => nameof(LoadedModelEvent);
}