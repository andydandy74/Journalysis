using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Extracts and normalizes exception records from parsed journal lines.
/// </summary>
/// <remarks>
/// Extraction combines timestamp, API-message, and comment-based exception patterns.
/// Some line patterns enrich the previously parsed exception instead of creating a new one.
/// </remarks>
public static class RecordedExceptionAnalyzer
{
    /// <summary>
    /// Builds recorded-exception objects from exception-related journal lines.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>A read-only list of parsed exception records in source order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<RecordedException> GetRecordedExceptions(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var exceptions = new List<RecordedException>();

        // Continuation records update the previously parsed exception,
        // so source order must be preserved.
        foreach (var line in GetExceptionLines(journal))
        {
            var newException = new RecordedException(line);
            var addNewException = true;

            switch (line)
            {
                case JournalTimeStampLine timeStampLine:
                    ParseTimeStampLine(newException, timeStampLine);
                    break;

                case JournalApiMessageLine apiMessageLine:
                    addNewException = ParseApiMessageLine(newException, apiMessageLine, exceptions);
                    break;

                case JournalCommentLine commentLine:
                    addNewException = ParseCommentLine(newException, commentLine, exceptions);
                    break;

                default:
                    newException.Message = line.RawText;
                    break;
            }

            if (addNewException)
            {
                exceptions.Add(newException);
            }
        }

        return exceptions;
    }

    /// <summary>
    /// Collects journal lines that are candidates for exception parsing.
    /// </summary>
    /// <param name="journal">Parsed journal to scan.</param>
    /// <returns>Exception-candidate lines in physical source order.</returns>
    /// <remarks>
    /// Filters intentionally exclude known non-exception noise entries
    /// such as ExceptionPolicy and unhandledExceptionFilter diagnostics.
    /// </remarks>
    private static IReadOnlyList<JournalLine> GetExceptionLines(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var lines = new List<JournalLine>();

        lines.AddRange(
            journal.GetLines<JournalTimeStampLine>()
                .Where(x => (x.Description ?? string.Empty)
                    .Contains("Exception", StringComparison.Ordinal)));

        lines.AddRange(
            journal.GetLines<JournalApiMessageLine>()
                .Where(x => x.IsError &&
                            (x.MessageText ?? string.Empty)
                                .Contains("Exception", StringComparison.Ordinal)));

        lines.AddRange(
            journal.GetLines<JournalCommentLine>()
                .Where(x =>
                {
                    var text = x.RawText ?? string.Empty;
                    return text.Contains("Exception", StringComparison.Ordinal) &&
                           !text.Contains("ExceptionPolicy", StringComparison.Ordinal) &&
                           !text.Contains("Exception occurred", StringComparison.Ordinal) &&
                           !text.Contains("unhandledExceptionFilter", StringComparison.Ordinal);
                }));

        return lines
            .OrderBy(x => x.Number)
            .ToArray();
    }

    /// <summary>
    /// Parses exception information from timestamp descriptions.
    /// </summary>
    /// <param name="exception">Destination exception model.</param>
    /// <param name="line">Source timestamp line.</param>
    /// <remarks>
    /// Supports CLARITY first-chance format, ApplicationException handler format,
    /// ArchiveException markers, and fatal ExceptionCode markers.
    /// </remarks>
    private static void ParseTimeStampLine(RecordedException exception, JournalTimeStampLine line)
    {
        var description = line.Description ?? string.Empty;

        var lp1 = description.Split(
            new[] { "ApplicationException is being thrown on behalf of the function <" },
            2,
            StringSplitOptions.None);

        if (lp1.Length > 1)
        {
            exception.Type = "ApplicationException";
            exception.Message = lp1[0];

            var lp2 = lp1[1].Split(new[] { ">" }, 2, StringSplitOptions.None);
            exception.StackTrace = lp2[0];
            return;
        }

        if (description.Contains("ArchiveException", StringComparison.Ordinal))
        {
            exception.Type = "ArchiveException";
        }
        else if (description.StartsWith("ExceptionCode", StringComparison.Ordinal))
        {
            exception.Type = "Fatal Error";
        }

        exception.Message = description.Replace(
            "CLARITY: First Chance Exception: ",
            string.Empty,
            StringComparison.Ordinal);

        var lp2Type = (exception.Message ?? string.Empty).Split(
            new[] { " :  " },
            2,
            StringSplitOptions.None);

        if (lp2Type.Length > 1 && !lp2Type[0].Contains(' '))
        {
            exception.Type = lp2Type[0];
            exception.Message = lp2Type[1];
        }
    }

    /// <summary>
    /// Parses exception information from API error messages.
    /// </summary>
    /// <param name="exception">Destination exception model.</param>
    /// <param name="line">Source API message line.</param>
    /// <param name="parsedExceptions">Already parsed exceptions used for continuation enrichment.</param>
    /// <returns>
    /// <see langword="true"/> to add <paramref name="exception"/> as a new record;
    /// <see langword="false"/> when information was appended to the previous exception.
    /// </returns>
    private static bool ParseApiMessageLine(
        RecordedException exception,
        JournalApiMessageLine line,
        List<RecordedException> parsedExceptions)
    {
        var messageText = line.MessageText ?? string.Empty;

        if (messageText.StartsWith("An external service execution throws", StringComparison.Ordinal))
        {
            // Append this information to the previous exception.
            if (parsedExceptions.Count == 0)
            {
                // No previous exception to enrich, keep this as a standalone record.
                exception.Message = messageText;
                return true;
            }

            var previous = parsedExceptions[^1];
            var lp1 = messageText.Split(':');

            var previousMessage = previous.Message ?? string.Empty;
            if (previousMessage.Length > 0)
            {
                previousMessage += " ";
            }

            previous.Message = previousMessage + lp1[0];

            if (lp1.Length > 1)
            {
                var lp2 = lp1[1].Split(new[] { "; " }, StringSplitOptions.None);

                if (lp2.Length > 0)
                {
                    previous.AppName = lp2[0]
                        .Replace(" Name(", string.Empty, StringComparison.Ordinal)
                        .Replace(")", string.Empty, StringComparison.Ordinal);
                }

                if (lp2.Length > 3)
                {
                    previous.AppGuid = lp2[2] + ", " + lp2[3];
                }
            }

            return false;
        }

        var lpMain = messageText.Split(new[] { " exception(" }, 2, StringSplitOptions.None);
        exception.Type = lpMain[0];

        if (lpMain.Length > 1)
        {
            var lp2 = lpMain[1].Split(
                new[] { ") was thrown from a handler of " },
                2,
                StringSplitOptions.None);

            exception.Message = lp2[0];

            if (lp2.Length > 1)
            {
                var lp3 = lp2[1].Split(
                    new[] { "event. The API event handler was registered by application " },
                    2,
                    StringSplitOptions.None);

                exception.Event = lp3[0];

                if (lp3.Length > 1)
                {
                    var lp4 = lp3[1].Split(new[] { " (" }, 2, StringSplitOptions.None);
                    exception.AppName = lp4[0];

                    if (lp4.Length > 1)
                    {
                        var lp5 = lp4[1].Split(new[] { ")" }, 2, StringSplitOptions.None);
                        exception.AppGuid = lp5[0];
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Parses exception information from comment-based exception patterns.
    /// </summary>
    /// <param name="exception">Destination exception model.</param>
    /// <param name="line">Source comment line.</param>
    /// <param name="parsedExceptions">Already parsed exceptions used for continuation enrichment.</param>
    /// <returns>
    /// <see langword="true"/> to add <paramref name="exception"/> as a new record;
    /// <see langword="false"/> when information was appended to the previous exception.
    /// </returns>
    private static bool ParseCommentLine(
        RecordedException exception,
        JournalCommentLine line,
        List<RecordedException> parsedExceptions)
    {
        var rawText = line.RawText ?? string.Empty;

        var lp1Managed = rawText.Split(
            new[] { "Exception caught from managed method " },
            2,
            StringSplitOptions.None);

        if (lp1Managed.Length > 1)
        {
            var lp2 = lp1Managed[1].Split(new[] { " <" }, 2, StringSplitOptions.None);
            exception.StackTrace = lp2[0];

            if (lp2.Length > 1)
            {
                var lp3 = lp2[1].Split(new[] { "> <" }, 2, StringSplitOptions.None);
                exception.Type = lp3[0];

                if (lp3.Length > 1)
                {
                    var msg = lp3[1].Trim();
                    if (msg.EndsWith(">", StringComparison.Ordinal))
                    {
                        msg = msg[..^1];
                    }

                    exception.Message = msg;
                }
            }

            return true;
        }

        var lp1ExternalRegistration = rawText.Split(
            new[] { "External Command Registration Exception: " },
            2,
            StringSplitOptions.None);

        var lp2Comment = rawText.Split(new[] { ":< " }, StringSplitOptions.None);

        if (lp1ExternalRegistration.Length > 1)
        {
            exception.Type = "External Command Registration Exception";
            exception.Message = lp1ExternalRegistration[1];
            return true;
        }

        if (rawText.Contains("OpenStream FileException", StringComparison.Ordinal))
        {
            exception.Type = "OpenStream FileException";
            var split = rawText.Split(new[] { "OpenStream FileException" }, 2, StringSplitOptions.None);
            exception.Message = split.Length > 1 ? split[1].Trim() : string.Empty;
            return true;
        }

        if (rawText.Contains("exceptionsInOnOpenDocument", StringComparison.Ordinal))
        {
            // Append this detail to the previous exception.
            if (parsedExceptions.Count == 0)
            {
                exception.Message = rawText;
                return true;
            }

            var lp3 = rawText.Split(new[] { "<<" }, 2, StringSplitOptions.None);
            if (lp3.Length > 1)
            {
                var previousMessage = parsedExceptions[^1].Message ?? string.Empty;
                parsedExceptions[^1].Message = previousMessage + " " + lp3[1];
            }

            return false;
        }

        if (lp2Comment.Length > 1)
        {
            if (lp2Comment[1].Contains("ExceptionProxy:", StringComparison.Ordinal))
            {
                var lp3 = lp2Comment[1].Split(new[] { ": " }, 2, StringSplitOptions.None);
                exception.Type = lp3[0];

                if (lp3.Length > 1)
                {
                    var lp4 = lp3[1].Split(new[] { " at" }, 2, StringSplitOptions.None);
                    exception.Message = lp4[0];

                    if (lp4.Length > 1)
                    {
                        exception.StackTrace = "at" + lp4[1];
                    }
                }
            }
            else
            {
                exception.Message = lp2Comment[1];
            }

            return true;
        }

        exception.Message = rawText;
        return true;
    }
}