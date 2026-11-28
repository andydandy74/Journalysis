using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// The six per-value memory-metric timelines built from block samples.
/// </summary>
internal sealed record MemoryMetricsTimeline(
    IReadOnlyList<JournalMemoryMetricsPoint> RamAvailable,
    IReadOnlyList<JournalMemoryMetricsPoint> RamUsed,
    IReadOnlyList<JournalMemoryMetricsPoint> RamPeak,
    IReadOnlyList<JournalMemoryMetricsPoint> VmAvailable,
    IReadOnlyList<JournalMemoryMetricsPoint> VmUsed,
    IReadOnlyList<JournalMemoryMetricsPoint> VmPeak);

/// <summary>
/// Builds one memory-metric sample per block for each of the six RAM/VM values.
/// </summary>
internal static class MemoryMetricsAnalyzer
{
    /// <summary>
    /// Gets per-block timelines from the last memory-metrics record in each block.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>Six per-value timelines, each ordered by block number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Blocks without memory-metrics records are ignored. Records in block 0
    /// (such as the initial VM line) are reported under block 1. A metric that
    /// the selected record did not parse is omitted from that metric's timeline,
    /// so the six timelines can have different lengths.
    /// </remarks>
    internal static MemoryMetricsTimeline GetTimeline(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var ramAvailable = new List<JournalMemoryMetricsPoint>();
        var ramUsed = new List<JournalMemoryMetricsPoint>();
        var ramPeak = new List<JournalMemoryMetricsPoint>();
        var vmAvailable = new List<JournalMemoryMetricsPoint>();
        var vmUsed = new List<JournalMemoryMetricsPoint>();
        var vmPeak = new List<JournalMemoryMetricsPoint>();

        // The first timestamp line is always block 1, for file and TextReader
        // input alike. Fall back to the first valid timestamp when block 1's
        // timestamp could not be parsed.
        var sessionStart = journal.Date ?? journal.GetLines<JournalTimeStampLine>()
            .Select(line => line.DateTime)
            .FirstOrDefault(date => date.HasValue);

        var lastByBlock = journal.GetLines<JournalMemoryMetricsLine>()
            .GroupBy(line => line.Block == 0 ? 1 : line.Block)
            .ToDictionary(group => group.Key, group => group.Last());

        foreach (var block in lastByBlock.Keys.Order())
        {
            var line = lastByBlock[block];
            var timeStamp = journal.GetDateTimeByBlock(block);
            var minutesSinceStart = timeStamp.HasValue && sessionStart.HasValue
                ? (timeStamp.Value - sessionStart.Value).TotalMinutes
                : (double?)null;

            if (line.RamAvailable is { } ramAvailableValue)
            {
                ramAvailable.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.RamAvailable, block, timeStamp, minutesSinceStart, ramAvailableValue));
            }

            if (line.RamUsed is { } ramUsedValue)
            {
                ramUsed.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.RamUsed, block, timeStamp, minutesSinceStart, ramUsedValue));
            }

            if (line.RamPeak is { } ramPeakValue)
            {
                ramPeak.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.RamPeak, block, timeStamp, minutesSinceStart, ramPeakValue));
            }

            if (line.VmAvailable is { } vmAvailableValue)
            {
                vmAvailable.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.VmAvailable, block, timeStamp, minutesSinceStart, vmAvailableValue));
            }

            if (line.VmUsed is { } vmUsedValue)
            {
                vmUsed.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.VmUsed, block, timeStamp, minutesSinceStart, vmUsedValue));
            }

            if (line.VmPeak is { } vmPeakValue)
            {
                vmPeak.Add(new JournalMemoryMetricsPoint(
                    JournalMemoryMetric.VmPeak, block, timeStamp, minutesSinceStart, vmPeakValue));
            }
        }

        return new MemoryMetricsTimeline(
            ramAvailable, ramUsed, ramPeak, vmAvailable, vmUsed, vmPeak);
    }
}
