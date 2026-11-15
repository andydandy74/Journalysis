using System;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents one timestamped event line recorded for a worksharing session.
/// </summary>
public sealed class WorksharingEvent
{
    /// <summary>
    /// Initializes a worksharing event.
    /// </summary>
    /// <param name="dateTime">Event timestamp.</param>
    /// <param name="text">Event text, for example <c>&gt;STC</c> or <c>&lt;Open</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public WorksharingEvent(DateTime dateTime, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        DateTime = dateTime;
        Text = text;
    }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    public DateTime DateTime { get; }

    /// <summary>
    /// Gets the event text.
    /// </summary>
    /// <remarks>
    /// A leading <c>&gt;</c> marks the start of an operation, <c>&lt;</c> its end,
    /// and <c>.</c> an informational marker.
    /// </remarks>
    public string Text { get; }

    public override string ToString() => nameof(WorksharingEvent);
}