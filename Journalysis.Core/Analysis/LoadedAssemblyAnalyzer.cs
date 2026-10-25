using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Extracts loaded assemblies and correlates related API messages.
/// </summary>
/// <remarks>
/// Correlation is heuristic and based on parsed API message text patterns
/// (GUID, application name, command replacement IDs, and assembly path/file name).
/// </remarks>
public static class LoadedAssemblyAnalyzer
{
    /// <summary>
    /// Finds loaded assemblies from API messages and assembly-load comments.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>A read-only list of discovered assemblies in initial-event order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<LoadedAssembly> GetLoadedAssemblies(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var assemblies = GetLoadedAssembliesFromApiMessages(journal)
            .Concat(GetLoadedAssembliesFromComments(journal))
            .OrderBy(assembly => assembly.Events[0].Index)
            .ToArray();

        return Array.AsReadOnly(assemblies);
    }

    /// <summary>
    /// Discovers add-in assemblies from startup API messages and attaches related
    /// registration, command, and exception messages.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>Assemblies discovered from API messages, with correlated events.</returns>
    private static IReadOnlyList<LoadedAssembly> GetLoadedAssembliesFromApiMessages(Journal journal)
    {
        var messages = journal.GetLines<JournalApiMessageLine>();
        var assemblies = new List<LoadedAssembly>();
        var replacedCommands = new Dictionary<string, LoadedAssembly>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            var assembly = TryReadLoadedAssembly(message);
            if (assembly is not null)
            {
                assemblies.Add(assembly);
            }
        }

        foreach (var message in messages)
        {
            var text = message.MessageText;
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            LoadedAssembly? owner = null;

            if (text.StartsWith("Registering", StringComparison.Ordinal) ||
                text.StartsWith("Unregistering", StringComparison.Ordinal) ||
                text.StartsWith("API registering", StringComparison.Ordinal) ||
                text.StartsWith("API unregistering", StringComparison.Ordinal))
            {
                const string marker = "by application ";
                var markerIndex = text.LastIndexOf(marker, StringComparison.Ordinal);

                if (markerIndex < 0)
                {
                    continue;
                }

                var application = text[(markerIndex + marker.Length)..];
                var guidStart = application.LastIndexOf(" (", StringComparison.Ordinal);
                var guidEnd = application.LastIndexOf(')');

                if (guidStart < 0 || guidEnd <= guidStart + 2)
                {
                    continue;
                }

                var name = application[..guidStart];
                var guid = application[(guidStart + 2)..guidEnd];

                owner = assemblies.FirstOrDefault(assembly =>
                    string.Equals(assembly.Guid, guid, StringComparison.Ordinal));

                owner ??= assemblies.FirstOrDefault(assembly =>
                    assembly.Guid is null &&
                    string.Equals(assembly.Name, name, StringComparison.Ordinal));

                if (owner is not null)
                {
                    owner.Guid ??= guid;
                }
            }
            else if (text.StartsWith("Replacing", StringComparison.Ordinal))
            {
                var name = ExtractQuotedValue(text, "from application '");
                var commandId = ExtractQuotedValue(text, "Replacing command id '");

                if (name is null || commandId is null)
                {
                    continue;
                }

                owner = assemblies.FirstOrDefault(assembly =>
                    string.Equals(assembly.Name, name, StringComparison.Ordinal));

                if (owner is not null)
                {
                    replacedCommands[commandId] = owner;
                }
            }
            else if (text.StartsWith("Restoring", StringComparison.Ordinal))
            {
                var commandId = ExtractQuotedValue(text, "Restoring command id '");

                if (commandId is not null)
                {
                    replacedCommands.TryGetValue(commandId, out owner);
                }
            }
            else if (text.StartsWith("Added pushbutton", StringComparison.Ordinal))
            {
                var separator = text.LastIndexOf(", ", StringComparison.Ordinal);
                var path = separator >= 0 ? text[(separator + 2)..] : text;

                if (path.StartsWith("assembly: ", StringComparison.Ordinal))
                {
                    path = path["assembly: ".Length..];
                }
                else if (path.StartsWith("assembly ", StringComparison.Ordinal))
                {
                    path = path["assembly ".Length..];
                }

                owner = assemblies.FirstOrDefault(assembly =>
                    string.Equals(assembly.Path, path, StringComparison.Ordinal));

                var fileName = GetJournalFileName(path);
                owner ??= assemblies.FirstOrDefault(assembly =>
                    string.Equals(assembly.Filename, fileName, StringComparison.Ordinal));
            }
            else if (text.StartsWith("System.", StringComparison.Ordinal))
            {
                const string suffix = "). Changes made by this handler are going to be discarded.";
                var suffixIndex = text.IndexOf(suffix, StringComparison.Ordinal);

                if (suffixIndex < 0)
                {
                    continue;
                }

                var precedingText = text[..suffixIndex];
                var guidStart = precedingText.LastIndexOf(" (", StringComparison.Ordinal);

                if (guidStart >= 0)
                {
                    var guid = precedingText[(guidStart + 2)..];
                    owner = assemblies.FirstOrDefault(assembly =>
                        string.Equals(assembly.Guid, guid, StringComparison.Ordinal));
                }
            }

            owner?.Events.Add(message);
        }

        return assemblies.AsReadOnly();
    }

    /// <summary>
    /// Discovers assemblies from comments that explicitly report an assembly load.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>Assemblies discovered from assembly-load comments.</returns>
    private static IReadOnlyList<LoadedAssembly> GetLoadedAssembliesFromComments(Journal journal)
    {
        var assemblies = new List<LoadedAssembly>();

        foreach (var comment in journal.GetLines<JournalCommentLine>())
        {
            var assembly = TryReadLoadedAssemblyFromComment(comment);
            if (assembly is not null)
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies.AsReadOnly();
    }

    /// <summary>
    /// Extracts a single-quoted value following a marker.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="marker">Marker immediately preceding the quoted value.</param>
    /// <returns>Extracted value, or <see langword="null"/> when not found.</returns>
    private static string? ExtractQuotedValue(string text, string marker)
    {
        var start = text.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = text.IndexOf('\'', start);
        return end >= start ? text[start..end] : null;
    }

    /// <summary>
    /// Extracts file name from a journal-reported path using either slash style.
    /// </summary>
    /// <param name="path">Path text from journal message.</param>
    private static string GetJournalFileName(string path)
    {
        var separator = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        return path[(separator + 1)..];
    }

    /// <summary>
    /// Attempts to create a <see cref="LoadedAssembly"/> from a startup API message.
    /// </summary>
    /// <param name="message">API message candidate.</param>
    /// <returns>A populated assembly model when parsing succeeds; otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// Supports both "Starting External Application" and
    /// "Starting External DB Application" style payloads.
    /// </remarks>
    private static LoadedAssembly? TryReadLoadedAssembly(JournalApiMessageLine message)
    {
        var text = message.MessageText;
        if (text is null || !text.StartsWith("Starting", StringComparison.Ordinal))
        {
            return null;
        }

        var applicationMarker = text.LastIndexOf("Application: ", StringComparison.Ordinal);
        if (applicationMarker < 0 ||
            !TrySplitOnce(
                text[(applicationMarker + "Application: ".Length)..],
                ", Class: ",
                out var name,
                out var classAndVendor) ||
            !TrySplitOnce(classAndVendor, ", Vendor : ", out var className, out var vendorAndAssembly) ||
            !TrySplitOnce(vendorAndAssembly, ", Assembly: ", out var vendorText, out var assemblyText))
        {
            return null;
        }

        name = name.Trim();
        className = className.Trim();
        vendorText = vendorText.Trim();

        string path;
        string? version = null;

        if (TrySplitOnce(assemblyText, ",", out path, out var versionText))
        {
            var nextComma = versionText.IndexOf(',');
            versionText = (nextComma >= 0 ? versionText[..nextComma] : versionText).Trim();

            const string versionPrefix = "Assembly Version: ";
            version = versionText.StartsWith(versionPrefix, StringComparison.Ordinal)
                ? versionText[versionPrefix.Length..]
                : versionText;
        }
        else
        {
            path = assemblyText;
        }

        path = path.Trim();

        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(className) ||
            string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var vendor = vendorText;
        string? vendorDetails = null;
        var detailsStart = vendorText.IndexOf('(');

        if (detailsStart >= 0)
        {
            vendor = vendorText[..detailsStart].Trim();
            var detailsEnd = vendorText.LastIndexOf(')');

            if (detailsEnd > detailsStart)
            {
                var details = vendorText[(detailsStart + 1)..detailsEnd].Trim();
                vendorDetails = details.Length > 0 ? details : null;
            }
        }

        if (string.IsNullOrWhiteSpace(vendor))
        {
            vendor = null;
        }

        return new LoadedAssembly(
            name,
            className,
            path,
            version,
            vendor,
            vendorDetails,
            message);
    }

    /// <summary>
    /// Attempts to extract an assembly identity and path from an assembly-load comment.
    /// </summary>
    /// <param name="comment">Comment record to inspect.</param>
    /// <returns>
    /// A populated assembly model, or <see langword="null"/> when the expected
    /// structure or required fields are missing.
    /// </returns>
    /// <remarks>
    /// Application class and vendor information are not supplied by this comment format.
    /// Incomplete records are skipped without throwing.
    /// </remarks>
    private static LoadedAssembly? TryReadLoadedAssemblyFromComment(JournalCommentLine comment)
    {
        const string marker = ":< The requested assembly '";
        var markerIndex = comment.RawText.IndexOf(marker, StringComparison.Ordinal);

        if (markerIndex < 0)
        {
            return null;
        }

        var assemblyAndTail = comment.RawText[(markerIndex + marker.Length)..];

        // Require the expected structure so unrelated quoted comment text is not
        // mistaken for an assembly identity or file path.
        if (!TrySplitOnce(
                assemblyAndTail,
                "' was loaded to the context '",
                out var identity,
                out var contextAndPath) ||
            !TrySplitOnce(
                contextAndPath,
                "' from '",
                out _,
                out var pathAndTail))
        {
            return null;
        }

        // A closing quote is required: otherwise a cut-off path could look valid.
        var pathEnd = pathAndTail.IndexOf('\'');
        if (pathEnd < 0)
        {
            return null;
        }

        var path = pathAndTail[..pathEnd].Trim();
        var identityParts = identity.Split(',');
        var name = identityParts[0].Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string? version = null;

        foreach (var part in identityParts.Skip(1))
        {
            const string versionPrefix = "Version=";
            var attribute = part.Trim();

            if (!attribute.StartsWith(versionPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var value = attribute[versionPrefix.Length..].Trim();
            version = value.Length > 0 ? value : null;
            break;
        }

        return new LoadedAssembly(
            name: name,
            className: null,
            path: path,
            version: version,
            vendor: null,
            vendorDetails: null,
            initialEvent: comment);
    }

    /// <summary>
    /// Splits text on the first occurrence of a separator.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <param name="separator">Separator to locate.</param>
    /// <param name="before">Text before separator when found.</param>
    /// <param name="after">Text after separator when found.</param>
    /// <returns><see langword="true"/> when separator is found; otherwise <see langword="false"/>.</returns>
    private static bool TrySplitOnce(
        string text,
        string separator,
        out string before,
        out string after)
    {
        var index = text.IndexOf(separator, StringComparison.Ordinal);
        if (index < 0)
        {
            before = string.Empty;
            after = string.Empty;
            return false;
        }

        before = text[..index];
        after = text[(index + separator.Length)..];
        return true;
    }
}