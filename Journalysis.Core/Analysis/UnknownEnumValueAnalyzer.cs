using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Finds journal records whose parsed enum property was left as
/// <c>Unknown</c> because the raw journal token could not be classified.
/// </summary>
/// <remarks>
/// Only explicitly assigned <c>Unknown</c> values are considered; enum
/// properties that were never assigned (and remain <see langword="null"/>)
/// are not included. Command records with an unknown command type also have
/// an associated recoverable parse error.
/// </remarks>
internal static class UnknownEnumValueAnalyzer
{
    /// <summary>
    /// Returns journal records with an enum property set to <c>Unknown</c>,
    /// in physical line order.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>A read-only list of matching records.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    internal static IReadOnlyList<JournalLine> GetUnknownEnumValueLines(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var lines = journal.Lines
            .Where(HasUnknownEnumValue)
            .OrderBy(line => line.Number)
            .ToArray();

        return Array.AsReadOnly(lines);
    }

    private static bool HasUnknownEnumValue(JournalLine line) => line switch
    {
        JournalCommandLine command =>
            command.CommandType == JournalCommandType.Unknown,
        JournalApiMessageLine message =>
            message.MessageType == JournalApiMessageType.Unknown,
        JournalMouseEventLine mouseEvent =>
            mouseEvent.MouseEventType == JournalMouseEventType.Unknown,
        JournalUiEventLine uiEvent =>
            uiEvent.UiEventType == JournalUiEventType.Unknown,
        JournalSystemInformationLine systemInformation =>
            systemInformation.SystemInformationType == JournalSystemInformationType.Unknown,
        _ => false
    };
}
