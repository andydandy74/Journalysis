using System;
using System.Collections.Generic;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Extracts synchronize-with-central operations from parsed worksharing session events.
/// </summary>
internal static class SyncEventAnalyzer
{
    private const string SyncStart = ">STC";
    private const string SyncEnd = "<STC";
    private const string SyncReloadLatest = ">STC:RL:Read";
    private const string SyncAborted = ".STC:RL:LockRoot RW gaveUp";

    /// <summary>
    /// Gets synchronize-with-central records derived from <c>&gt;STC</c> / <c>&lt;STC</c> event pairs.
    /// </summary>
    /// <param name="session">Parsed worksharing session to analyze.</param>
    /// <returns>Detected sync operations in encounter order, including operations without an end marker.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    internal static IReadOnlyList<SyncEvent> GetSyncEvents(WorksharingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var syncEvents = new List<SyncEvent>();

        foreach (var worksharingEvent in session.Events)
        {
            if (worksharingEvent.Text == SyncStart)
            {
                syncEvents.Add(new SyncEvent(worksharingEvent.DateTime));
            }
            else if (syncEvents.Count == 0)
            {
                // Ignore sync sub-events that appear before any sync start marker.
                continue;
            }
            else if (worksharingEvent.Text == SyncReloadLatest)
            {
                syncEvents[^1].ReloadLatestCount++;
            }
            else if (worksharingEvent.Text == SyncAborted)
            {
                syncEvents[^1].WasAborted = true;
            }
            else if (worksharingEvent.Text == SyncEnd)
            {
                syncEvents[^1].End = worksharingEvent.DateTime;
            }
        }

        return syncEvents;
    }
}