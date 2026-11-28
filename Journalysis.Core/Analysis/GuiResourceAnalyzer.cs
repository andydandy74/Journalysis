using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// The three per-value GUI resource timelines built from block samples.
/// </summary>
internal sealed record GuiResourceTimeline(
    IReadOnlyList<JournalGuiResourcePoint> GdiAvailable,
    IReadOnlyList<JournalGuiResourcePoint> GdiUsed,
    IReadOnlyList<JournalGuiResourcePoint> UserUsed);

/// <summary>
/// Builds one GUI resource sample per block for each of the three counters.
/// </summary>
internal static class GuiResourceAnalyzer
{
    /// <summary>
    /// Gets per-block timelines from the last GUI resource-usage record in each block.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>Three per-value timelines, each ordered by block number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Blocks without GUI resource-usage records are ignored. Records in block 0
    /// are reported under block 1. A counter that the selected record did not
    /// parse is omitted from that counter's timeline, so the three timelines can
    /// have different lengths.
    /// </remarks>
    internal static GuiResourceTimeline GetTimeline(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var gdiAvailable = new List<JournalGuiResourcePoint>();
        var gdiUsed = new List<JournalGuiResourcePoint>();
        var userUsed = new List<JournalGuiResourcePoint>();

        var sessionStart = journal.Date ?? journal.GetLines<JournalTimeStampLine>()
            .Select(line => line.DateTime)
            .FirstOrDefault(date => date.HasValue);

        var lastByBlock = journal.GetLines<JournalGuiResourceUsageLine>()
            .GroupBy(line => line.Block == 0 ? 1 : line.Block)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var block in lastByBlock.Keys.Order())
        {
            var line = lastByBlock[block];
            var timeStamp = journal.GetDateTimeByBlock(block);
            var minutesSinceStart = timeStamp.HasValue && sessionStart.HasValue
                ? (timeStamp.Value - sessionStart.Value).TotalMinutes
                : (double?)null;

            if (line.Available is { } availableValue)
            {
                gdiAvailable.Add(new JournalGuiResourcePoint(
                    JournalGuiResourceMetric.GdiAvailable, block, timeStamp, minutesSinceStart, availableValue));
            }

            if (line.Used is { } usedValue)
            {
                gdiUsed.Add(new JournalGuiResourcePoint(
                    JournalGuiResourceMetric.GdiUsed, block, timeStamp, minutesSinceStart, usedValue));
            }

            if (line.User is { } userValue)
            {
                userUsed.Add(new JournalGuiResourcePoint(
                    JournalGuiResourceMetric.UserUsed, block, timeStamp, minutesSinceStart, userValue));
            }
        }

        return new GuiResourceTimeline(gdiAvailable, gdiUsed, userUsed);
    }
}
