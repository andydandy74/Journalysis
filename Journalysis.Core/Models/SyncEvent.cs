using System;

/// <summary>
/// Represents one synchronize-with-central operation derived from session events.
/// </summary>
public sealed class SyncEvent
{
    /// <summary>
    /// Initializes a sync event.
    /// </summary>
    /// <param name="start">Timestamp of the <c>&gt;STC</c> marker.</param>
    public SyncEvent(DateTime start)
    {
        Start = start;
    }

    /// <summary>
    /// Gets the sync duration, when an end marker was recorded.
    /// </summary>
    public TimeSpan? Duration => End - Start;

    /// <summary>
    /// Gets the timestamp of the <c>&lt;STC</c> marker, when recorded.
    /// </summary>
    public DateTime? End { get; internal set; }

    /// <summary>
    /// Gets the number of reload-latest reads performed during this sync.
    /// </summary>
    public int ReloadLatestCount { get; internal set; }

    /// <summary>
    /// Gets the timestamp of the <c>&gt;STC</c> marker.
    /// </summary>
    public DateTime Start { get; }

    public override string ToString() => nameof(SyncEvent);

    /// <summary>
    /// Gets whether the sync gave up waiting for the central root lock.
    /// </summary>
    public bool WasAborted { get; internal set; }
}