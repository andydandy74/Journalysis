using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Parses Revit journal text into classified records and journal metadata.
/// </summary>
public sealed class JournalParser
{
    /// <summary>
    /// Parses a journal from a file.
    /// </summary>
    /// <remarks>
    /// The parser owns and disposes the file reader.
    /// The journal's Path property comes from recorded metadata, not this argument.
    /// </remarks>
    public Journal Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path);
        return Parse(reader);
    }

    /// <summary>
    /// Parses journal text from the reader's current position to its end.
    /// </summary>
    /// <remarks>
    /// The caller owns the reader. This method does not dispose it or rewind it.
    /// </remarks>
    public Journal Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var processingStarted = Stopwatch.GetTimestamp();

        // Pass 1: assemble logical records and create their concrete model types.
        var firstPassResult = JournalRecordBuilder.Build(ReadLines(reader));

        // Pass 2: populate record fields and collect journal-level metadata.
        var context = ParseSecondPass(firstPassResult.Records);

        // Construct the journal only after parsing. Its constructor assigns each
        // record's parent journal and collection index.
        var journal = new Journal(
            lines: firstPassResult.Records,
            version: context.Version,
            user: context.UserName,
            blockCount: firstPassResult.BlockCount,
            path: context.RecordedPath,
            build: context.Build,
            branch: context.Branch,
            release: context.Release,
            machine: context.MachineName,
            osVersion: context.OsVersion);

        journal.ProcessingTime = Stopwatch.GetElapsedTime(processingStarted);

        return journal;
    }

    private static JournalParseContext ParseSecondPass(
        IReadOnlyList<JournalLine> records)
    {
        var context = new JournalParseContext();

        // Preserve encounter order because metadata and system-information
        // numbering can depend on previously processed records.
        foreach (var record in records)
        {
            JournalLineParser.Parse(record, context);
        }

        return context;
    }

    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}