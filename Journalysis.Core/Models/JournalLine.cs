using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Base type for one parsed logical journal record.
/// </summary>
/// <remarks>
/// Instances are created during parsing. Navigation helpers rely on
/// <see cref="Journal"/> and <see cref="Index"/>, which are assigned by
/// <see cref="Journal"/> after construction.
/// </remarks>
public abstract class JournalLine
{
    /// <summary>
    /// Initializes a parsed journal record.
    /// </summary>
    /// <param name="number">Original physical line number in the source file.</param>
    /// <param name="rawText">Raw (possibly merged) journal text for this record.</param>
    /// <param name="block">Timestamp-delimited block number this record belongs to.</param>
    protected JournalLine(int number, string rawText, int block)
    {
        Number = number;
        RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        Block = block;
    }

    /// <summary>
    /// Gets the original physical line number in the source file.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// Gets the raw journal text for this logical record.
    /// </summary>
    /// <remarks>
    /// During first-pass parsing, continuation lines may be appended to this value.
    /// </remarks>
    public string RawText { get; internal set; }

    /// <summary>
    /// Gets the timestamp-delimited block number for this record.
    /// </summary>
    public int Block { get; }

    /// <summary>
    /// Gets the zero-based index of this record in <see cref="Journal.Lines"/>.
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    /// Gets the parent journal that owns this record.
    /// </summary>
    public Journal? Journal { get; internal set; }

    /// <summary>
    /// Gets the semantic record category independently of concrete C# type names.
    /// </summary>
    public abstract JournalLineKind Kind { get; }

    /// <summary>
    /// Gets all following records in the same journal.
    /// </summary>
    /// <returns>
    /// A snapshot of all records after this record, or an empty collection when unavailable.
    /// </returns>
    public IReadOnlyList<JournalLine> AllNext()
    {
        if (Journal is null || Index + 1 >= Journal.Lines.Count)
        {
            return Array.Empty<JournalLine>();
        }

        return Journal.Lines.Skip(Index + 1).ToArray();
    }

    /// <summary>
    /// Gets all following records of a specific type.
    /// </summary>
    /// <typeparam name="TLine">Requested line type.</typeparam>
    public IReadOnlyList<TLine> AllNextOfType<TLine>() where TLine : JournalLine =>
        AllNext().OfType<TLine>().ToArray();

    /// <summary>
    /// Gets all preceding records in the same journal.
    /// </summary>
    /// <returns>
    /// A snapshot of all records before this record, or an empty collection when unavailable.
    /// </returns>
    public IReadOnlyList<JournalLine> AllPrevious()
    {
        if (Journal is null || Index <= 0)
        {
            return Array.Empty<JournalLine>();
        }

        return Journal.Lines.Take(Index).ToArray();
    }

    /// <summary>
    /// Gets all preceding records of a specific type.
    /// </summary>
    /// <typeparam name="TLine">Requested line type.</typeparam>
    public IReadOnlyList<TLine> AllPreviousOfType<TLine>() where TLine : JournalLine =>
        AllPrevious().OfType<TLine>().ToArray();

    /// <summary>
    /// Gets the timestamp for this record's block.
    /// </summary>
    /// <returns>
    /// Block timestamp when available; otherwise <see langword="null"/>.
    /// </returns>
    public DateTime? GetDateTimeOfBlock() => Journal?.GetDateTimeByBlock(Block);

    /// <summary>
    /// Gets the next record in the same journal.
    /// </summary>
    /// <returns>The next record, or <see langword="null"/> when none exists.</returns>
    public JournalLine? Next() =>
        Journal is not null && Index + 1 < Journal.Lines.Count
            ? Journal.Lines[Index + 1]
            : null;

    /// <summary>
    /// Gets the next record of a specific type.
    /// </summary>
    /// <typeparam name="TLine">Requested line type.</typeparam>
    /// <returns>The first following record of type <typeparamref name="TLine"/>, or <see langword="null"/>.</returns>
    public TLine? NextOfType<TLine>() where TLine : JournalLine =>
        AllNextOfType<TLine>().FirstOrDefault();

    /// <summary>
    /// Gets the previous record in the same journal.
    /// </summary>
    /// <returns>The previous record, or <see langword="null"/> when none exists.</returns>
    public JournalLine? Previous() =>
        Journal is not null && Index > 0
            ? Journal.Lines[Index - 1]
            : null;

    /// <summary>
    /// Gets the previous record of a specific type.
    /// </summary>
    /// <typeparam name="TLine">Requested line type.</typeparam>
    /// <returns>The last preceding record of type <typeparamref name="TLine"/>, or <see langword="null"/>.</returns>
    public TLine? PreviousOfType<TLine>() where TLine : JournalLine =>
        AllPreviousOfType<TLine>().LastOrDefault();

    public override string ToString() => GetType().Name;
}

/// <summary>
/// Parsed <c>Jrn.AddInEvent</c> record.
/// Contains add-in-provided playback-relevant event text.
/// </summary>
public sealed class JournalAddinEventLine : JournalLine
{
    public JournalAddinEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.AddinEvent;

    /// <summary>
    /// Gets or sets the extracted add-in message payload.
    /// </summary>
    public string? MessageText { get; set; }
}

/// <summary>
/// Parsed API success/error message comment (<c>API_SUCCESS</c> / <c>API_ERROR</c>).
/// </summary>
public sealed class JournalApiMessageLine : JournalLine
{
    public JournalApiMessageLine(int number, string rawText, int block, bool isError) : base(number, rawText, block)
    {
        IsError = isError;
    }

    public override JournalLineKind Kind => JournalLineKind.ApiMessage;

    /// <summary>
    /// Gets whether this API message is marked as an error.
    /// </summary>
    public bool IsError { get; }

    /// <summary>
    /// Gets or sets the extracted API message text inside braces.
    /// </summary>
    public string? MessageText { get; set; }

    /// <summary>
    /// Gets or sets the classified API message category.
    /// </summary>
    public JournalApiMessageType? MessageType { get; set; }
}

/// <summary>
/// Parsed <c>[Jrn.BasicFileInfo]</c> metadata comment.
/// </summary>
public sealed class JournalBasicFileInfoLine : JournalLine
{
    public JournalBasicFileInfoLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.BasicFileInfo;

    /// <summary>
    /// Gets or sets worksharing mode text (for example: Central, Local, Not enabled).
    /// </summary>
    public string? Worksharing { get; set; }

    /// <summary>
    /// Gets or sets the extracted central model path.
    /// </summary>
    public string? CentralModelPath { get; set; }

    /// <summary>
    /// Gets or sets the extracted last-save path.
    /// </summary>
    public string? LastSavePath { get; set; }

    /// <summary>
    /// Gets or sets locale text recorded when the model was saved.
    /// </summary>
    public string? Locale { get; set; }

    /// <summary>
    /// Gets or sets file name extracted from <see cref="LastSavePath"/>.
    /// </summary>
    public string? FileName { get; set; }
}

/// <summary>
/// Parsed <c>Jrn.Command</c> record for built-in Revit commands.
/// </summary>
public sealed class JournalCommandLine : JournalLine
{
    public JournalCommandLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Command;

    /// <summary>
    /// Gets or sets the parsed command context type.
    /// </summary>
    public JournalCommandType? CommandType { get; set; }

    /// <summary>
    /// Gets or sets command description text.
    /// </summary>
    public string? CommandDescription { get; set; }

    /// <summary>
    /// Gets or sets built-in command ID (for example: <c>ID_APP_EXIT</c>).
    /// </summary>
    public string? CommandId { get; set; }
}

/// <summary>
/// Parsed comment record that does not match a more specific comment subtype.
/// </summary>
public sealed class JournalCommentLine : JournalLine
{
    public JournalCommentLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Comment;
}

/// <summary>
/// Parsed <c>Jrn.Data</c> playback data record.
/// </summary>
public sealed class JournalDataLine : JournalLine
{
    public JournalDataLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Data;

    /// <summary>
    /// Gets or sets the data key.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets parsed value tokens associated with <see cref="Key"/>.
    /// </summary>
    public List<string> Values { get; } = [];
}

/// <summary>
/// Parsed <c>Jrn.Directive</c> record used for startup/session directives.
/// </summary>
public sealed class JournalDirectiveLine : JournalLine
{
    public JournalDirectiveLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Directive;

    /// <summary>
    /// Gets or sets the directive key (for example: Version, Username).
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets parsed value tokens associated with <see cref="Key"/>.
    /// </summary>
    public List<string> Values { get; } = [];
}

/// <summary>
/// Parsed GUI resource-usage comment (GDI counters).
/// </summary>
public sealed class JournalGuiResourceUsageLine : JournalLine
{
    public JournalGuiResourceUsageLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.GuiResourceUsage;

    /// <summary>
    /// Gets or sets available GUI-resource count.
    /// </summary>
    public int? Available { get; set; }

    /// <summary>
    /// Gets or sets used GUI-resource count.
    /// </summary>
    public int? Used { get; set; }

    /// <summary>
    /// Gets or sets user-related GUI-resource count.
    /// </summary>
    public int? User { get; set; }
}

/// <summary>
/// Parsed <c>Jrn.Key</c> keyboard event record.
/// </summary>
public sealed class JournalKeyboardEventLine : JournalLine
{
    public JournalKeyboardEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.KeyboardEvent;

    /// <summary>
    /// Gets or sets extracted keyboard token (for example: <c>VK_DOWN</c>).
    /// </summary>
    public string? Key { get; set; }
}

/// <summary>
/// Parsed memory-metrics comment with VM/RAM usage values.
/// </summary>
public sealed class JournalMemoryMetricsLine : JournalLine
{
    public JournalMemoryMetricsLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MemoryMetrics;

    /// <summary>
    /// Gets or sets available virtual memory.
    /// </summary>
    public int? VmAvailable { get; set; }

    /// <summary>
    /// Gets or sets used virtual memory.
    /// </summary>
    public int? VmUsed { get; set; }

    /// <summary>
    /// Gets or sets peak virtual memory.
    /// </summary>
    public int? VmPeak { get; set; }

    /// <summary>
    /// Gets or sets available RAM.
    /// </summary>
    public int? RamAvailable { get; set; }

    /// <summary>
    /// Gets or sets used RAM.
    /// </summary>
    public int? RamUsed { get; set; }

    /// <summary>
    /// Gets or sets peak RAM.
    /// </summary>
    public int? RamPeak { get; set; }
}

/// <summary>
/// Parsed playback-relevant command line that does not match other command-like types.
/// </summary>
public sealed class JournalMiscCommandLine : JournalLine
{
    public JournalMiscCommandLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MiscCommand;
}

/// <summary>
/// Parsed mouse event record (for example: move, click, wheel, scroll).
/// </summary>
public sealed class JournalMouseEventLine : JournalLine
{
    public JournalMouseEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MouseEvent;

    /// <summary>
    /// Gets or sets parsed mouse event type token.
    /// </summary>
    public JournalMouseEventType? MouseEventType { get; set; }

    /// <summary>
    /// Gets parsed integer payload values for this mouse event.
    /// </summary>
    public List<int> Data { get; } = [];
}

/// <summary>
/// Parsed system-information key/value line inside a system-information section.
/// </summary>
public sealed class JournalSystemInformationLine : JournalLine
{
    public JournalSystemInformationLine(
        int number,
        string rawText,
        int block,
        JournalSystemInformationType systemInformationType)
        : base(number, rawText, block)
    {
        SystemInformationType = systemInformationType;
    }

    public override JournalLineKind Kind => JournalLineKind.SystemInformation;

    /// <summary>
    /// Gets the current system-information section type.
    /// </summary>
    public JournalSystemInformationType SystemInformationType { get; }

    /// <summary>
    /// Gets or sets parser-assigned section item number for multi-device sections.
    /// </summary>
    public int? ItemNumber { get; set; }

    /// <summary>
    /// Gets or sets extracted key text.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets extracted value text.
    /// </summary>
    public string? Value { get; set; }
}

/// <summary>
/// Parsed timestamp record that begins a new logical block.
/// </summary>
public sealed class JournalTimeStampLine : JournalLine
{
    public JournalTimeStampLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.TimeStamp;

    /// <summary>
    /// Gets or sets parsed timestamp marker type (C/E/H).
    /// </summary>
    public JournalTimeStampType? TimeStampType { get; set; }

    /// <summary>
    /// Gets or sets parsed timestamp value.
    /// </summary>
    public DateTime? DateTime { get; set; }

    /// <summary>
    /// Gets or sets optional timestamp description text.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets optional debug classification inferred from description prefixes.
    /// </summary>
    public JournalDebugInfoType? DebugInfoType { get; set; }
}

/// <summary>
/// Parsed UI event record (Jrn.* UI interactions).
/// </summary>
public sealed class JournalUiEventLine : JournalLine
{
    public JournalUiEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.UiEvent;

    /// <summary>
    /// Gets or sets parsed UI event type token.
    /// </summary>
    public JournalUiEventType? UiEventType { get; set; }

    /// <summary>
    /// Gets parsed UI payload tokens extracted from event text.
    /// </summary>
    public List<string> Data { get; } = [];
}

/// <summary>
/// Parsed worksharing SLOG event comment.
/// </summary>
public sealed class JournalWorksharingEventLine : JournalLine
{
    public JournalWorksharingEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.WorksharingEvent;

    /// <summary>
    /// Gets or sets extracted worksharing session ID.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets extracted worksharing timestamp.
    /// </summary>
    public DateTime? DateTime { get; set; }

    /// <summary>
    /// Gets or sets extracted worksharing event text payload.
    /// </summary>
    public string? Text { get; set; }
}