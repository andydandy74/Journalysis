using System;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents an exception recorded in a journal.
/// </summary>
public sealed class RecordedException
{
    public RecordedException(JournalLine journalLine)
    {
        ArgumentNullException.ThrowIfNull(journalLine);

        JournalLine = journalLine;
    }

    /// <summary>
    /// Gets the journal line that introduced the exception.
    /// </summary>
    public JournalLine JournalLine { get; }

    /// <summary>
    /// Gets the exception type, or "Unknown" if it could not be identified.
    /// </summary>
    public string Type { get; internal set; } = "Unknown";

    /// <summary>
    /// Gets the exception message, if available.
    /// </summary>
    public string? Message { get; internal set; }

    /// <summary>
    /// Gets the API event associated with the exception, if available.
    /// </summary>
    public string? Event { get; internal set; }

    /// <summary>
    /// Gets the associated application name, if available.
    /// </summary>
    public string? AppName { get; internal set; }

    /// <summary>
    /// Gets the application identifier text recorded in the journal.
    /// </summary>
    public string? AppGuid { get; internal set; }

    /// <summary>
    /// Gets the recorded stack trace or managed-method description, if available.
    /// </summary>
    public string? StackTrace { get; internal set; }

    public override string ToString() => nameof(RecordedException);
}