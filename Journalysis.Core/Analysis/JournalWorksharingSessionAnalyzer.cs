using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Builds <see cref="WorksharingSession"/> objects from SLOG entries stored in journal lines.
/// </summary>
/// <remarks>
/// The analyzer reconstructs an in-memory mini worksharing log by taking the payload part
/// of each <see cref="JournalWorksharingEventLine"/> (text after <c>:&lt; SLOG </c>) in encounter order,
/// then reuses <see cref="WorksharingLogParser"/> to parse sessions and metadata consistently.
/// </remarks>
internal static class JournalWorksharingSessionAnalyzer
{
    private const string SlogMarker = ":< SLOG ";

    /// <summary>
    /// Extracts worksharing sessions from a parsed journal.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>Detected worksharing sessions in encounter order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    internal static IReadOnlyList<WorksharingSession> GetWorksharingSessions(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var worksharingLines = journal.GetLines<JournalWorksharingEventLine>();
        if (worksharingLines.Count == 0)
        {
            return Array.Empty<WorksharingSession>();
        }

        var miniLog = BuildMiniWorksharingLog(worksharingLines);
        if (string.IsNullOrWhiteSpace(miniLog))
        {
            return Array.Empty<WorksharingSession>();
        }

        var parser = new WorksharingLogParser();
        var parsedLog = parser.Parse(new StringReader(miniLog));

        return parsedLog.Sessions;
    }

    /// <summary>
    /// Reconstructs the textual payload-only worksharing log from journal SLOG lines.
    /// </summary>
    /// <param name="lines">Journal SLOG lines in encounter order.</param>
    /// <returns>Payload-only worksharing log text.</returns>
    private static string BuildMiniWorksharingLog(IReadOnlyList<JournalWorksharingEventLine> lines)
    {
        var builder = new StringBuilder();

        foreach (var line in lines.OrderBy(x => x.Number))
        {
            var payload = ExtractSlogPayload(line.RawText);
            if (payload is null)
            {
                continue;
            }

            builder.AppendLine(payload);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Extracts the text after the SLOG marker.
    /// </summary>
    /// <param name="rawText">Raw journal line text.</param>
    /// <returns>The SLOG payload, or <see langword="null"/> if no payload exists.</returns>
    private static string? ExtractSlogPayload(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var markerIndex = rawText.IndexOf(SlogMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var payloadStart = markerIndex + SlogMarker.Length;
        if (payloadStart >= rawText.Length)
        {
            return null;
        }

        var payload = rawText[payloadStart..].Trim();
        return payload.Length == 0 ? null : payload;
    }
}