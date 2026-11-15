using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Identifies pairs of overlapping synchronize-with-central operations in a worksharing log.
/// </summary>
internal static class SyncEventOverlapAnalyzer
{
    /// <summary>
    /// Gets overlapping sync-event pairs across all sessions in the log.
    /// </summary>
    /// <param name="log">Parsed worksharing log to analyze.</param>
    /// <returns>
    /// Overlapping pairs ordered by each event's start, then end timestamp.
    /// Events with identical timestamps retain session and event encounter order.
    /// </returns>
    /// <remarks>
    /// Touching endpoints count as overlaps. Operations without an end marker are excluded.
    /// Aborted operations with end markers and pairs from the same session are included.
    /// Results are individual pairs, not merged groups of overlapping operations.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> is <see langword="null"/>.</exception>
    internal static IReadOnlyList<SyncEventOverlap> GetSyncEventOverlaps(WorksharingLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        var events = new List<(WorksharingSession Session, SyncEvent Event, DateTime End)>();

        foreach (var session in log.Sessions)
        {
            foreach (var syncEvent in session.SyncEvents)
            {
                if (syncEvent.End is { } end)
                {
                    events.Add((session, syncEvent, end));
                }
            }
        }

        var sortedEvents = events
            .OrderBy(entry => entry.Event.Start)
            .ThenBy(entry => entry.End)
            .ToArray();
        var overlaps = new List<SyncEventOverlap>();

        for (var i = 0; i < sortedEvents.Length; i++)
        {
            var first = sortedEvents[i];

            for (var j = i + 1; j < sortedEvents.Length; j++)
            {
                var second = sortedEvents[j];
                if (second.Event.Start > first.End)
                {
                    break;
                }

                overlaps.Add(new SyncEventOverlap(
                    first.Session,
                    first.Event,
                    second.Session,
                    second.Event));
            }
        }

        return overlaps.AsReadOnly();
    }
}
