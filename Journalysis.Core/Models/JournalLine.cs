using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Base type for a parsed journal line.
/// </summary>
public abstract class JournalLine
{
    protected JournalLine(int number, string rawText, int block)
    {
        Number = number;
        RawText = rawText ?? throw new ArgumentNullException(nameof(rawText));
        Block = block;
    }

    public int Number { get; }
    public string RawText { get; internal set; }
    public int Block { get; }
    public int Index { get; internal set; }
    public Journal? Journal { get; internal set; }

    /// <summary>
    /// Gets the journal record category independently of its C# class name.
    /// </summary>
    public abstract JournalLineKind Kind { get; }

    public IReadOnlyList<JournalLine> AllNext()
    {
        if (Journal is null || Index + 1 >= Journal.Lines.Count)
        {
            return Array.Empty<JournalLine>();
        }

        return Journal.Lines.Skip(Index + 1).ToArray();
    }

    public IReadOnlyList<TLine> AllNextOfType<TLine>() where TLine : JournalLine =>
        AllNext().OfType<TLine>().ToArray();

    public IReadOnlyList<JournalLine> AllPrevious()
    {
        if (Journal is null || Index <= 0)
        {
            return Array.Empty<JournalLine>();
        }

        return Journal.Lines.Take(Index).ToArray();
    }

    public IReadOnlyList<TLine> AllPreviousOfType<TLine>() where TLine : JournalLine =>
        AllPrevious().OfType<TLine>().ToArray();

    public DateTime? GetDateTimeOfBlock() => Journal?.GetDateTimeByBlock(Block);

    public JournalLine? Next() =>
        Journal is not null && Index + 1 < Journal.Lines.Count
            ? Journal.Lines[Index + 1]
            : null;

    public TLine? NextOfType<TLine>() where TLine : JournalLine =>
        AllNextOfType<TLine>().FirstOrDefault();

    public JournalLine? Previous() =>
        Journal is not null && Index > 0
            ? Journal.Lines[Index - 1]
            : null;

    public TLine? PreviousOfType<TLine>() where TLine : JournalLine =>
        AllPreviousOfType<TLine>().LastOrDefault();

    public override string ToString() => GetType().Name;
}

public sealed class JournalAddinEventLine : JournalLine
{
    public JournalAddinEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.AddinEvent;
    public string? MessageText { get; set; }
}

public sealed class JournalApiMessageLine : JournalLine
{
    public JournalApiMessageLine(int number, string rawText, int block, bool isError) : base(number, rawText, block)
    {
        IsError = isError;
    }

    public override JournalLineKind Kind => JournalLineKind.ApiMessage;
    public bool IsError { get; }
    public string? MessageText { get; set; }
    public JournalApiMessageType? MessageType { get; set; }
}

public sealed class JournalBasicFileInfoLine : JournalLine
{
    public JournalBasicFileInfoLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.BasicFileInfo;
    public string? Worksharing { get; set; }
    public string? CentralModelPath { get; set; }
    public string? LastSavePath { get; set; }
    public string? Locale { get; set; }
    public string? FileName { get; set; }
}

public sealed class JournalCommandLine : JournalLine
{
    public JournalCommandLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Command;
    public JournalCommandType? CommandType { get; set; }
    public string? CommandDescription { get; set; }
    public string? CommandId { get; set; }
}

public sealed class JournalCommentLine : JournalLine
{
    public JournalCommentLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Comment;
}

public sealed class JournalDataLine : JournalLine
{
    public JournalDataLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Data;
    public string? Key { get; set; }
    public List<string> Values { get; } = [];
}

public sealed class JournalDirectiveLine : JournalLine
{
    public JournalDirectiveLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.Directive;
    public string? Key { get; set; }
    public List<string> Values { get; } = [];
}

public sealed class JournalGuiResourceUsageLine : JournalLine
{
    public JournalGuiResourceUsageLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.GuiResourceUsage;
    public int? Available { get; set; }
    public int? Used { get; set; }
    public int? User { get; set; }
}

public sealed class JournalKeyboardEventLine : JournalLine
{
    public JournalKeyboardEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.KeyboardEvent;
    public string? Key { get; set; }
}

public sealed class JournalMemoryMetricsLine : JournalLine
{
    public JournalMemoryMetricsLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MemoryMetrics;
    public int? VmAvailable { get; set; }
    public int? VmUsed { get; set; }
    public int? VmPeak { get; set; }
    public int? RamAvailable { get; set; }
    public int? RamUsed { get; set; }
    public int? RamPeak { get; set; }
}

public sealed class JournalMiscCommandLine : JournalLine
{
    public JournalMiscCommandLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MiscCommand;
}

public sealed class JournalMouseEventLine : JournalLine
{
    public JournalMouseEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.MouseEvent;
    public JournalMouseEventType? MouseEventType { get; set; }
    public List<int> Data { get; } = [];
}

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
    public JournalSystemInformationType SystemInformationType { get; }
    public int? ItemNumber { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}

public sealed class JournalTimeStampLine : JournalLine
{
    public JournalTimeStampLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.TimeStamp;
    public JournalTimeStampType? TimeStampType { get; set; }
    public DateTime? DateTime { get; set; }
    public string? Description { get; set; }
    public JournalDebugInfoType? DebugInfoType { get; set; }
}

public sealed class JournalUiEventLine : JournalLine
{
    public JournalUiEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.UiEvent;
    public JournalUiEventType? UiEventType { get; set; }
    public List<string> Data { get; } = [];
}

public sealed class JournalWorksharingEventLine : JournalLine
{
    public JournalWorksharingEventLine(int number, string rawText, int block) : base(number, rawText, block)
    {
    }

    public override JournalLineKind Kind => JournalLineKind.WorksharingEvent;
    public string? SessionId { get; set; }
    public DateTime? DateTime { get; set; }
    public string? Text { get; set; }
}