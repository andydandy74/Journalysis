using System;
using System.Collections.Generic;
using Journalysis.Core.Models;

namespace Journalysis.Core.Analysis;

/// <summary>
/// Extracts linked-model load sequences from parsed worksharing session events.
/// </summary>
internal static class LoadedLinkAnalyzer
{
    private const string OpenLinkStart = ">OpenLink";
    private const string OpenLinkEnd = "<OpenLink";

    /// <summary>
    /// Gets loaded-link records derived from <c>&gt;OpenLink</c> / <c>&lt;OpenLink</c> event pairs.
    /// </summary>
    /// <param name="session">Parsed worksharing session to analyze.</param>
    /// <returns>Detected link loads in encounter order, including loads without an end marker.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    internal static IReadOnlyList<LoadedLink> GetLoadedLinks(WorksharingSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var links = new List<LoadedLink>();

        foreach (var worksharingEvent in session.Events)
        {
            if (worksharingEvent.Text.StartsWith(OpenLinkStart, StringComparison.Ordinal))
            {
                // The link path is the first double-quoted token, e.g. >OpenLink "C:\path\file.rvt"
                var parts = worksharingEvent.Text.Split('"');
                var path = parts.Length > 1 ? parts[1] : string.Empty;
                links.Add(new LoadedLink(worksharingEvent.DateTime, path));
            }
            else if (worksharingEvent.Text == OpenLinkEnd && links.Count > 0)
            {
                links[^1].LoadEnd = worksharingEvent.DateTime;
            }
        }

        return links;
    }
}