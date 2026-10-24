using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Extracts loaded-model lifecycles from parsed journal records.
/// </summary>
/// <remarks>
/// Detection is heuristic and based on command/UI open triggers, nearby data records
/// carrying file names, and a subsequent load-completion marker pattern.
/// </remarks>
public static class LoadedModelAnalyzer
{
    /// <summary>
    /// Gets loaded-model sequences detected in a parsed journal.
    /// </summary>
    /// <param name="journal">Parsed journal to analyze.</param>
    /// <returns>A read-only list of detected loaded-model sequences.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="journal"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<LoadedModel> GetLoadedModels(Journal journal)
    {
        ArgumentNullException.ThrowIfNull(journal);

        var events = new List<LoadedModelEvent>();

        // All variations of opening a document via dialog.
        events.AddRange(
            journal.GetLines<JournalCommandLine>()
                .Where(x =>
                    string.Equals(x.CommandId, "ID_REVIT_FILE_OPEN", StringComparison.Ordinal) ||
                    string.Equals(x.CommandId, "ID_APPMENU_PROJECT_OPEN", StringComparison.Ordinal))
                .Select(x => new LoadedModelEvent(x, LoadedModelEventType.OpenDialog, x.GetDateTimeOfBlock())));

        // All variations of opening a document via recent files.
        events.AddRange(
            journal.GetLines<JournalCommandLine>()
                .Where(x =>
                    string.Equals(x.CommandId, "ID_FILE_MRU_FIRST", StringComparison.Ordinal) ||
                    (x.CommandId?.StartsWith("ID_FILE_MRU", StringComparison.Ordinal) ?? false))
                .Select(x => new LoadedModelEvent(x, LoadedModelEventType.OpenRecent, x.GetDateTimeOfBlock())));

        // Models opened via drag & drop.
        events.AddRange(
            journal.GetLines<JournalUiEventLine>()
                .Where(x => string.Equals(x.UiEventType.ToString(), "DropFiles", StringComparison.Ordinal))
                .Select(x => new LoadedModelEvent(x, LoadedModelEventType.OpenDragDrop, x.GetDateTimeOfBlock())));

        // Process candidate events in encounter order.
        var paths = new List<string>();
        var ignoreEventIndexes = new List<int>();

        for (var i = 0; i < events.Count; i++)
        {
            var dataLine = events[i].JournalLine.NextOfType<JournalDataLine>();

            // For workshared models, the second JournalData line may carry the filename.
            if (dataLine is not null &&
                dataLine.Key is not ("File Name" or "Dropped File Name" or "MRUFileName"))
            {
                dataLine = dataLine.NextOfType<JournalDataLine>();
            }

            var nextDataLine = dataLine?.NextOfType<JournalDataLine>();

            // Flag models that do not have a JournalData line carrying the filename.
            if (dataLine is null ||
                dataLine.Key is not ("File Name" or "Dropped File Name" or "MRUFileName"))
            {
                ignoreEventIndexes.Add(i);
            }
            // Flag models canceled in file-open dialog.
            else if (string.Equals(dataLine.Key, "File Name", StringComparison.Ordinal) &&
                     dataLine.Values.Count > 0 &&
                     string.Equals(dataLine.Values[0], "IDCANCEL", StringComparison.Ordinal))
            {
                ignoreEventIndexes.Add(i);
            }
            // Flag models canceled in status bar; otherwise add path.
            else if (nextDataLine is not null)
            {
                if (string.Equals(nextDataLine.Key, "ProgressCancelled", StringComparison.Ordinal))
                {
                    var nextDataLine2 = nextDataLine.NextOfType<JournalDataLine>();
                    if (nextDataLine2 is not null &&
                        string.Equals(nextDataLine2.Key, "TaskDialogResult", StringComparison.Ordinal) &&
                        nextDataLine2.Values.Count > 0 &&
                        string.Equals(nextDataLine2.Values[^1], "IDYES", StringComparison.Ordinal))
                    {
                        ignoreEventIndexes.Add(i);
                    }
                    else if (dataLine.Values.Count > 0)
                    {
                        paths.Add(NormalizePath(journal, dataLine.Values[^1]));
                    }
                }
                else if (dataLine.Values.Count > 0)
                {
                    paths.Add(NormalizePath(journal, dataLine.Values[^1]));
                }
            }
            else if (dataLine.Values.Count > 0)
            {
                paths.Add(NormalizePath(journal, dataLine.Values[^1]));
            }
        }

        // Remove flagged events in reverse order so indexes stay valid.
        foreach (var index in ignoreEventIndexes.OrderByDescending(x => x))
        {
            events.RemoveAt(index);
        }

        // Build LoadedModel objects.
        var loadedModels = new List<LoadedModel>(events.Count);
        for (var i = 0; i < events.Count; i++)
        {
            var path = i < paths.Count ? paths[i] : null;
            loadedModels.Add(new LoadedModel(journal, path, events[i]));
        }

        // Get load-complete markers and add a Loaded event when found.
        for (var i = 0; i < loadedModels.Count; i++)
        {
            var current = events[i].JournalLine;
            JournalTimeStampLine? loadedTimeStamp = null;

            while (true)
            {
                var nextComment = current.NextOfType<JournalCommentLine>();
                if (nextComment is null)
                {
                    break;
                }

                current = nextComment;

                if (!string.Equals(
                        nextComment.RawText,
                        "'***********************************************************",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                loadedTimeStamp = nextComment.NextOfType<JournalTimeStampLine>();
                break;
            }

            if (loadedTimeStamp is not null)
            {
                loadedModels[i].Events.Add(
                    new LoadedModelEvent(
                        loadedTimeStamp,
                        LoadedModelEventType.Loaded,
                        loadedTimeStamp.DateTime));
            }
        }

        return loadedModels;
    }

    /// <summary>
    /// Normalizes a model path against the recorded journal directory when possible.
    /// </summary>
    /// <param name="journal">Parent journal used to resolve base directory.</param>
    /// <param name="path">Raw model path text extracted from journal data lines.</param>
    /// <returns>
    /// A normalized absolute path when base context is available; otherwise the original path.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="journal"/> or <paramref name="path"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Path normalization uses <see cref="Path.GetFullPath(string)"/> after combining
    /// with the directory of <see cref="Journal.Path"/>. If no usable base directory is
    /// available, this method returns <paramref name="path"/> unchanged.
    /// </remarks>
    private static string NormalizePath(Journal journal, string path)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(path);

        // Normalize the path by combining it with the journal's directory and resolving the full path.
        if (string.IsNullOrEmpty(journal.Path))
        {
            return path;
        }

        var baseDirectory = Path.GetDirectoryName(journal.Path);
        if (string.IsNullOrEmpty(baseDirectory))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }
}