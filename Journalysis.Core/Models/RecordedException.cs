using System;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents an exception-like condition parsed from journal records.
/// </summary>
/// <remarks>
/// Instances are built by exception analysis over timestamp, API-message, and comment lines.
/// Depending on source format, some fields may remain <see langword="null"/>.
/// </remarks>
public sealed class RecordedException
{
    /// <summary>
    /// Initializes a recorded-exception model from the source journal line.
    /// </summary>
    /// <param name="journalLine">The journal line that introduced this exception record.</param>
    /// <exception cref="ArgumentNullException"><paramref name="journalLine"/> is <see langword="null"/>.</exception>
    public RecordedException(JournalLine journalLine)
    {
        ArgumentNullException.ThrowIfNull(journalLine);

        JournalLine = journalLine;
    }

    /// <summary>
    /// Gets the journal line that introduced this exception record.
    /// </summary>
    public JournalLine JournalLine { get; }

    /// <summary>
    /// Gets the parsed exception type.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>"Unknown"</c> when a type cannot be inferred from source text.
    /// </remarks>
    public string Type { get; internal set; } = "Unknown";

    /// <summary>
    /// Gets the parsed exception message text, when available.
    /// </summary>
    public string? Message { get; internal set; }

    /// <summary>
    /// Gets the associated API event name, when available.
    /// </summary>
    public string? Event { get; internal set; }

    /// <summary>
    /// Gets the associated application name, when available.
    /// </summary>
    public string? AppName { get; internal set; }

    /// <summary>
    /// Gets the associated application GUID text, when available.
    /// </summary>
    public string? AppGuid { get; internal set; }

    /// <summary>
    /// Gets the parsed stack trace or managed-method descriptor, when available.
    /// </summary>
    public string? StackTrace { get; internal set; }

    public override string ToString() => nameof(RecordedException);
}