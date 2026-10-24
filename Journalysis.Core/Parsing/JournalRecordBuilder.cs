using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// First parser pass: assembles multiline journal records and classifies them
/// into the correct <see cref="JournalLine"/> subtype.
/// </summary>
internal static class JournalRecordBuilder
{
    internal static JournalRecordBuilderResult Build(IEnumerable<string> physicalLines)
    {
        ArgumentNullException.ThrowIfNull(physicalLines);

        var records = new List<JournalLine>();
        var state = new JournalRecordBuilderState();

        foreach (var rawPhysicalLine in physicalLines)
        {
            state.PhysicalLineNumber++;

            var line = NormalizeLine(rawPhysicalLine);

            // Ignore empty/very short lines.
            if (line.Length < 2)
            {
                continue;
            }

            // The logic relies on these ordered checks:
            // 1) classify known new records
            // 2) append multiline continuations
            // 3) special handling for apostrophe lines / sysinfo
            // 4) fallback to misc command
            if (TryCreateNewRecord(line, state, records))
            {
                continue;
            }

            if (TryAppendContinuation(line, records))
            {
                continue;
            }

            if (line[0] == '\'')
            {
                HandleApostropheLine(line, state, records);
            }
            else
            {
                records.Add(new JournalMiscCommandLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            }
        }

        return new JournalRecordBuilderResult(records, state.CurrentBlock);
    }

    private static string NormalizeLine(string? line)
    {
        // Trim leading spaces and remove trailing newline and null characters.
        return (line ?? string.Empty)
            .TrimStart()
            .TrimEnd('\n')
            .TrimEnd('\0');
    }

    private static bool TryCreateNewRecord(
        string line,
        JournalRecordBuilderState state,
        List<JournalLine> records)
    {
        if (IsTimeStampStart(line))
        {
            state.CurrentBlock++;
            records.Add(new JournalTimeStampLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.Contains(":< API_SUCCESS { ", StringComparison.Ordinal))
        {
            records.Add(new JournalApiMessageLine(state.PhysicalLineNumber, line, state.CurrentBlock, isError: false));
            return true;
        }

        if (line.Contains(":< API_ERROR { ", StringComparison.Ordinal))
        {
            records.Add(new JournalApiMessageLine(state.PhysicalLineNumber, line, state.CurrentBlock, isError: true));
            return true;
        }

        if (line.Contains(":: Delta VM: ", StringComparison.Ordinal) ||
            line.StartsWith("' 0:< Initial VM: ", StringComparison.Ordinal))
        {
            records.Add(new JournalMemoryMetricsLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.Contains(":< GUI Resource Usage GDI: ", StringComparison.Ordinal))
        {
            records.Add(new JournalGuiResourceUsageLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.StartsWith("' [Jrn.BasicFileInfo]", StringComparison.Ordinal))
        {
            records.Add(new JournalBasicFileInfoLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.StartsWith("Jrn.Data ", StringComparison.Ordinal))
        {
            records.Add(new JournalDataLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.StartsWith("Jrn.Directive ", StringComparison.Ordinal))
        {
            records.Add(new JournalDirectiveLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.StartsWith("Jrn.Command ", StringComparison.Ordinal))
        {
            records.Add(new JournalCommandLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            if (state.CommandCount < 2)
            {
                state.CommandCount++;
            }

            return true;
        }

        if (line.StartsWith("Jrn.Key ", StringComparison.Ordinal))
        {
            records.Add(new JournalKeyboardEventLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.StartsWith("Jrn.AddInEvent ", StringComparison.Ordinal))
        {
            records.Add(new JournalAddinEventLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (StartsWithAny(line, MousePrefixes))
        {
            records.Add(new JournalMouseEventLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (StartsWithAny(line, UiPrefixes))
        {
            records.Add(new JournalUiEventLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        if (line.Contains(":< SLOG $", StringComparison.Ordinal))
        {
            records.Add(new JournalWorksharingEventLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return true;
        }

        return false;
    }

    private static bool TryAppendContinuation(string line, List<JournalLine> records)
    {
        if (records.Count == 0)
        {
            return false;
        }

        // Timestamp lines must be handled as new records
        // by TryCreateNewRecord and never consumed as continuation text.
        if (IsTimeStampStart(line))
        {
            return false;
        }

        var previous = records[^1];

        // Append linebreaks to previous line if previous raw text ends with "_".
        if (previous.RawText.Length > 0 && previous.RawText[^1] == '_')
        {
            previous.RawText = previous.RawText[..^1] + line;
            return true;
        }

        // Append linebreaks in commands.
        if (line[0] == ',')
        {
            previous.RawText = (previous.RawText + line).Replace(" _,", ",", StringComparison.Ordinal);
            return true;
        }

        return false;
    }

    private static void HandleApostropheLine(
        string line,
        JournalRecordBuilderState state,
        List<JournalLine> records)
    {
        if (records.Count == 0)
        {
            records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return;
        }

        var previous = records[^1];

        // Append linebreaks in API messages.
        if (line.Length > 1 &&
            line[1] != ' ' &&
            previous is JournalApiMessageLine &&
            !previous.RawText.EndsWith("}", StringComparison.Ordinal))
        {
            previous.RawText += " " + line[1..];
            return;
        }

        if (line.Length > 1 &&
            line[1] != ' ' &&
            previous is JournalTimeStampLine)
        {
            previous.RawText += " " + line[1..];
            return;
        }

        if (line.StartsWith("' ", StringComparison.Ordinal) &&
            previous is JournalCommentLine &&
            !line.Contains(":<", StringComparison.Ordinal))
        {
            previous.RawText += " " + line[2..].TrimStart();
            return;
        }

        if (state.SystemInformationStarted)
        {
            if (line.Contains(":< PROCESSOR INFORMATION:", StringComparison.Ordinal))
            {
                state.CurrentSystemInformationType = JournalSystemInformationType.Processor;
                records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                return;
            }

            if (line.Contains(":< VIDEO CONTROLLER INFORMATION:", StringComparison.Ordinal))
            {
                state.CurrentSystemInformationType = JournalSystemInformationType.VideoController;
                records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                return;
            }

            if (line.Contains(":< PRINTER INFORMATION:", StringComparison.Ordinal))
            {
                state.CurrentSystemInformationType = JournalSystemInformationType.Printer;
                records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                return;
            }

            if (line.Contains(":< PRINTER CONFIGURATION INFORMATION:", StringComparison.Ordinal))
            {
                state.CurrentSystemInformationType = JournalSystemInformationType.PrinterConfiguration;
                records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                return;
            }

            if (line.Contains(" INFORMATION:", StringComparison.Ordinal))
            {
                state.CurrentSystemInformationType = JournalSystemInformationType.Unknown;
                records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                return;
            }

            if (line.Contains(":<    ", StringComparison.Ordinal))
            {
                var afterMarker = line[(line.IndexOf(":<    ", StringComparison.Ordinal) + 6)..];

                if (afterMarker.StartsWith(" ", StringComparison.Ordinal))
                {
                    records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
                }
                else
                {
                    records.Add(new JournalSystemInformationLine(
                        state.PhysicalLineNumber,
                        line,
                        state.CurrentBlock,
                        state.CurrentSystemInformationType));
                }

                return;
            }

            records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
            return;
        }

        if (line.Contains(":< OPERATING SYSTEM INFORMATION:", StringComparison.Ordinal))
        {
            state.SystemInformationStarted = true;
            state.CurrentSystemInformationType = JournalSystemInformationType.OperatingSystem;
        }

        records.Add(new JournalCommentLine(state.PhysicalLineNumber, line, state.CurrentBlock));
    }

    private static bool IsTimeStampStart(string line) =>
        line.StartsWith("'C ", StringComparison.Ordinal) ||
        line.StartsWith("'H ", StringComparison.Ordinal) ||
        line.StartsWith("'E ", StringComparison.Ordinal) ||
        line.StartsWith("' C ", StringComparison.Ordinal) ||
        line.StartsWith("' H ", StringComparison.Ordinal) ||
        line.StartsWith("' E ", StringComparison.Ordinal);

    private static bool StartsWithAny(string line, string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly string[] MousePrefixes =
    Enum.GetValues<JournalMouseEventType>()
        .Where(type => type != JournalMouseEventType.Unknown)
        .Select(type => $"Jrn.{type}")
        .ToArray();

    private static readonly string[] UiPrefixes =
    Enum.GetValues<JournalUiEventType>()
        .Where(type => type != JournalUiEventType.Unknown)
        .Select(type => $"Jrn.{type}")
        .ToArray();
}

internal sealed class JournalRecordBuilderState
{
    public int PhysicalLineNumber { get; set; }
    public int CurrentBlock { get; set; }
    public int CommandCount { get; set; }
    public bool SystemInformationStarted { get; set; }
    public JournalSystemInformationType CurrentSystemInformationType { get; set; } =
        JournalSystemInformationType.Unknown;
}

internal sealed class JournalRecordBuilderResult
{
    public JournalRecordBuilderResult(IReadOnlyList<JournalLine> records, int blockCount)
    {
        Records = records;
        BlockCount = blockCount;
    }

    public IReadOnlyList<JournalLine> Records { get; }
    public int BlockCount { get; }
}