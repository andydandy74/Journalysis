using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Parses Revit journal text into strongly typed records and journal metadata.
/// </summary>
/// <remarks>
/// Parsing is performed in two passes:
/// 1) assemble/classify logical records,
/// 2) parse type-specific fields and collect journal-level metadata.
/// </remarks>
public sealed class JournalParser
{
    /// <summary>
    /// Parses a journal from a file path.
    /// </summary>
    /// <param name="path">Path to a journal text file.</param>
    /// <returns>A parsed <see cref="Journal"/> instance.</returns>
    /// <remarks>
    /// This overload owns and disposes the created file reader.
    /// <see cref="Journal.Path"/> is populated from recorded journal metadata,
    /// not from <paramref name="path"/>.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    public Journal Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path);
        return Parse(reader);
    }

    /// <summary>
    /// Parses journal text from a <see cref="TextReader"/> starting at its current position.
    /// </summary>
    /// <param name="reader">Source reader positioned at the first line to parse.</param>
    /// <returns>A parsed <see cref="Journal"/> instance.</returns>
    /// <remarks>
    /// The caller owns the reader. This method does not dispose or rewind it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
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

    /// <summary>
    /// Executes parser pass 2 over already assembled logical records.
    /// </summary>
    /// <param name="records">Logical records in encounter order.</param>
    /// <returns>Collected parse context with journal-level metadata and shared state.</returns>
    /// <remarks>
    /// Encounter order is preserved because metadata and system-information item
    /// numbering depend on previously processed records.
    /// </remarks>
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

    /// <summary>
    /// Enumerates all remaining lines from the reader.
    /// </summary>
    /// <param name="reader">Source text reader.</param>
    /// <returns>Line sequence from current position to end-of-stream.</returns>
    private static IEnumerable<string> ReadLines(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            yield return line;
        }
    }
}