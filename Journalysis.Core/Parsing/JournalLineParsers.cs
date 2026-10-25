using System;
using System.Collections.Generic;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Routes assembled journal records to their type-specific second-pass parsers.
/// </summary>
/// <remarks>
/// Pass 2 populates parsed fields on existing <see cref="JournalLine"/> instances
/// and updates shared metadata in <see cref="JournalParseContext"/>.
/// </remarks>
internal static class JournalLineParser
{
    /// <summary>
    /// Parses one already-classified journal record.
    /// </summary>
    /// <param name="line">Record to parse.</param>
    /// <param name="context">Shared parse context for journal-level metadata/state.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="line"/> or <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// No second-pass handler exists for the concrete record type.
    /// </exception>
    internal static void Parse(
        JournalLine line,
        JournalParseContext context)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(context);

        // Dispatch by concrete type so each parser receives a strongly
        // typed record without reflection or string-based type lookup.
        switch (line)
        {
            case JournalAddinEventLine addinEvent:
                ParseAddinEvent(addinEvent);
                break;

            case JournalApiMessageLine apiMessage:
                ParseApiMessage(apiMessage);
                break;

            case JournalBasicFileInfoLine basicFileInfo:
                ParseBasicFileInfo(basicFileInfo);
                break;

            case JournalCommandLine command:
                ParseCommand(command);
                break;

            case JournalCommentLine comment:
                ParseComment(comment, context);
                break;

            case JournalDataLine data:
                ParseData(data);
                break;

            case JournalDirectiveLine directive:
                ParseDirective(directive, context);
                break;

            case JournalGuiResourceUsageLine guiResourceUsage:
                ParseGuiResourceUsage(guiResourceUsage);
                break;

            case JournalKeyboardEventLine keyboardEvent:
                ParseKeyboardEvent(keyboardEvent);
                break;

            case JournalMemoryMetricsLine memoryMetrics:
                ParseMemoryMetrics(memoryMetrics);
                break;

            case JournalMiscCommandLine:
                // No additional second-pass fields for miscellaneous commands.
                break;

            case JournalMouseEventLine mouseEvent:
                ParseMouseEvent(mouseEvent);
                break;

            case JournalSystemInformationLine systemInformation:
                ParseSystemInformation(systemInformation, context);
                break;

            case JournalTimeStampLine timeStamp:
                ParseTimeStamp(timeStamp);
                break;

            case JournalUiEventLine uiEvent:
                ParseUiEvent(uiEvent);
                break;

            case JournalWorksharingEventLine worksharingEvent:
                ParseWorksharingEvent(worksharingEvent);
                break;

            default:
                // Fail explicitly if a new model type is introduced without
                // considering how it should be handled by the second pass.
                throw new NotSupportedException(
                    $"No second-pass handler exists for '{line.GetType().FullName}'.");
        }
    }

    /// <summary>
    /// Extracts the second quoted field as add-in event message text.
    /// </summary>
    /// <param name="line">Add-in event record.</param>
    /// <exception cref="FormatException">
    /// The expected quoted payload field is missing.
    /// </exception>
    private static void ParseAddinEvent(JournalAddinEventLine line)
    {
        var fields = line.RawText.Split('"');

        // Extract the second quoted field without removing empty fields or trimming text.
        if (fields.Length < 4)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse add-in event at physical line {line.Number}: " +
                    "the second quoted field is missing."));
        }

        line.MessageText = fields[3];
    }

    /// <summary>
    /// Extracts API message text payload and assigns recognized message category.
    /// </summary>
    /// <param name="line">API message record.</param>
    /// <exception cref="FormatException">
    /// The API payload cannot be parsed due to missing required delimiters
    /// or empty payload text.
    /// </exception>
    private static void ParseApiMessage(JournalApiMessageLine line)
    {
        var openingParts = line.RawText.Split(
            "{ ",
            StringSplitOptions.None);

        if (openingParts.Length < 2)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse API message at physical line {line.Number}: " +
                    "the opening message delimiter is missing."));
        }

        // Keep best-effort extraction so partially cut-off lines still preserve
        // useful payload text for analysis.
        var payloadAndTail = openingParts[1];
        var closingParts = payloadAndTail.Split(" }", StringSplitOptions.None);
        var hasClosingDelimiter = closingParts.Length > 1;
        var messageText = closingParts[0].Trim();

        if (messageText.StartsWith(": ", StringComparison.Ordinal))
        {
            messageText = messageText[2..];
        }

        if (messageText.Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse API message at physical line {line.Number}: " +
                    "the API payload is empty."));
        }

        line.MessageText = messageText;
        line.MessageType = ParseApiMessageType(messageText);

        // Report truncation but keep parsed fields assigned above.
        if (!hasClosingDelimiter)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse API message at physical line {line.Number}: " +
                    "the closing message delimiter is missing. " +
                    "The available payload text was preserved."));
        }
    }

    /// <summary>
    /// Maps API message text to a known <see cref="JournalApiMessageType"/>.
    /// </summary>
    /// <param name="messageText">Extracted API message text.</param>
    private static JournalApiMessageType ParseApiMessageType(string messageText)
    {
        if (messageText.StartsWith(
            "Registered an external service",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.RegisteredExternalService;
        }

        if (messageText.StartsWith(
                "Registered an external server",
                StringComparison.Ordinal) ||
            messageText.StartsWith(
                "An external server has been registered",
                StringComparison.Ordinal))
        {
            return JournalApiMessageType.RegisteredExternalServer;
        }

        if (messageText.StartsWith(
            "Starting External DB Application",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.StartingExternalDbApplication;
        }

        if (messageText.StartsWith(
            "Starting External Application",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.StartingExternalApplication;
        }

        if (messageText.StartsWith("Registering", StringComparison.Ordinal))
        {
            return JournalApiMessageType.RegisteringEvent;
        }

        if (messageText.StartsWith(
            "Replacing command id",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.ReplacingCommandId;
        }

        if (messageText.StartsWith(
            "API registering command",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.RegisteringCommandEvent;
        }

        if (messageText.StartsWith("Added pushbutton", StringComparison.Ordinal))
        {
            return JournalApiMessageType.AddedPushbutton;
        }

        if (messageText.StartsWith("Unregistering", StringComparison.Ordinal))
        {
            return JournalApiMessageType.UnregisteringEvent;
        }

        if (messageText.StartsWith(
            "Restoring command id",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.RestoringCommandId;
        }

        if (messageText.StartsWith(
            "API unregistering command",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.UnregisteringCommandEvent;
        }

        if (messageText.Contains(
            "Assembly version conflict",
            StringComparison.Ordinal))
        {
            return JournalApiMessageType.AssemblyVersionConflict;
        }

        if (messageText.StartsWith(
                "An external service execution throws",
                StringComparison.Ordinal) ||
            messageText.StartsWith("System.", StringComparison.Ordinal))
        {
            return JournalApiMessageType.Exception;
        }

        return JournalApiMessageType.Unknown;
    }

    /// <summary>
    /// Parses <c>[Jrn.BasicFileInfo]</c> attribute fragments into key/value pairs.
    /// </summary>
    /// <param name="rawText">Raw basic-file-info record text.</param>
    /// <returns>Attribute dictionary keyed by attribute name.</returns>
    /// <remarks>
    /// Duplicate keys keep the last observed value.
    /// Malformed fragments are ignored.
    /// </remarks>
    private static Dictionary<string, string> ParseBasicFileInfoAttributes(string rawText)
    {
        // Extract the payload starting from index 22 (safe even if text is shorter).
        var payload = rawText.Length > 22 ? rawText[22..] : string.Empty;

        // Split by attribute marker and skip the first preamble segment.
        var attrSegments = payload.Split("Rvt.Attr.", StringSplitOptions.None);
        if (attrSegments.Length <= 1)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        // Build key/value pairs using split(":", 1), skipping malformed fragments.
        var pairs = new List<KeyValuePair<string, string>>();

        for (var i = 1; i < attrSegments.Length; i++)
        {
            var segment = attrSegments[i];
            var split = segment.Split(':', 2, StringSplitOptions.None);

            if (split.Length < 2)
            {
                // Skip malformed fragments that cannot be paired.
                continue;
            }

            pairs.Add(new KeyValuePair<string, string>(split[0], split[1].Trim()));
        }

        if (pairs.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        // Keep the last duplicate key.
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            dict[pair.Key] = pair.Value;
        }

        return dict;
    }

    /// <summary>
    /// Extracts command context, description, and command ID.
    /// </summary>
    /// <param name="line">Command record.</param>
    private static void ParseCommand(JournalCommandLine line)
    {
        // Replace double spaces with single spaces and split by the delimiter.
        var c1 = line.RawText
            .Replace("  ", " ", StringComparison.Ordinal)
            .Split("\" , \"", StringSplitOptions.None);

        if (c1.Length == 0)
        {
            return;
        }

        // Identify the CommandType by taking the substring after the first 13 characters of the first segment.
        line.CommandType = ParseCommandType(
            c1[0][Math.Min(13, c1[0].Length)..]);

        // Only try to process command details if the line is complete.
        if (c1.Length <= 1)
        {
            return;
        }

        var c2 = c1[1].Split(" , ", StringSplitOptions.None);

        if (c2.Length > 0)
        {
            line.CommandDescription = c2[0];
        }

        // Only try to process command ID if line is complete.
        if (c2.Length <= 1)
        {
            return;
        }

        var commandIdCandidate = c2[1].Length > 0
            ? c2[1][..^1]
            : c2[1];

        // Allow for different formatting in Revit 2022.
        if (commandIdCandidate.EndsWith("\"", StringComparison.Ordinal))
        {
            commandIdCandidate = commandIdCandidate[..^1];
        }

        line.CommandId = commandIdCandidate;
    }

    /// <summary>
    /// Maps raw command-context token text to known command types.
    /// </summary>
    /// <param name="value">Raw command-context token.</param>
    private static JournalCommandType ParseCommandType(string value)
    {
        return value switch
        {
            "AccelKey" => JournalCommandType.AccelKey,
            "Internal" => JournalCommandType.Internal,
            "KeyboardShortcut" => JournalCommandType.KeyboardShortcut,
            "PrintPreviewUI" => JournalCommandType.PrintPreviewUi,
            "Ribbon" => JournalCommandType.Ribbon,
            "StartupPage" => JournalCommandType.StartupPage,
            "StatusBar" => JournalCommandType.StatusBar,
            "SystemMenu" => JournalCommandType.SystemMenu,
            _ => JournalCommandType.Unknown
        };
    }

    /// <summary>
    /// Parses comment records for journal-level metadata:
    /// recorded path, build info, and machine-name information.
    /// </summary>
    /// <param name="line">Comment record.</param>
    /// <param name="context">Shared parse context.</param>
    /// <exception cref="FormatException">
    /// Required separators are missing in recognized metadata comment formats.
    /// </exception>
    private static void ParseComment(
        JournalCommentLine line,
        JournalParseContext context)
    {
        var text = line.RawText;

        if (text.Contains("this journal =", StringComparison.Ordinal))
        {
            var fields = text.Split('=', StringSplitOptions.None);
            if (fields.Length < 2)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse recorded journal path at physical line {line.Number}: " +
                        "expected '=' separator."));
            }

            context.RecordedPath = fields[1].Trim();
        }
        else if (text.StartsWith("' Build:", StringComparison.Ordinal))
        {
            var fields = text.Split(':', StringSplitOptions.None);

            if (fields.Length < 4)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse build metadata at physical line {line.Number}: " +
                        "expected build, branch, and release segments."));
            }

            context.Build = ExtractBuildMetadataToken(fields[1]);
            context.Branch = ExtractBuildMetadataToken(fields[2]);
            context.Release = ExtractBuildMetadataToken(fields[3]);
        }
        else if (!context.MachineNameFound &&
                 text.Contains("Additional IP address/name found for host", StringComparison.Ordinal))
        {
            var fields = text.Split(':', StringSplitOptions.None);

            if (fields.Length < 2)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse machine metadata at physical line {line.Number}: " +
                        "expected ':' separator."));
            }

            var words = fields[1].Split(' ', StringSplitOptions.None);
            context.MachineName = words.Length > 0 ? words[^1] : string.Empty;
            context.MachineNameFound = true;
        }
    }

    /// <summary>
    /// Extracts the first build-metadata token from a colon-delimited build segment.
    /// </summary>
    /// <param name="text">Build segment text.</param>
    /// <returns>Trimmed first token.</returns>
    private static string ExtractBuildMetadataToken(string text)
    {
        // The separator is a literal space, not arbitrary whitespace.
        return text.TrimStart()
            .Split(' ', StringSplitOptions.None)[0]
            .Trim();
    }

    /// <summary>
    /// Extracts <see cref="JournalDataLine.Key"/> and value tokens.
    /// </summary>
    /// <param name="line">Data record.</param>
    private static void ParseData(JournalDataLine line)
    {
        var parts = line.RawText.Split(
            "\"  , ",
            StringSplitOptions.None);

        // Identify the key by taking the substring after the first 10 characters of the first segment.
        var keyCandidate = parts[0][Math.Min(10, parts[0].Length)..];

        // Allow for different formatting in Revit 2022.
        if (keyCandidate.StartsWith("\"", StringComparison.Ordinal))
        {
            keyCandidate = keyCandidate[1..];
        }

        line.Key = keyCandidate;

        // If this line is cut off, do not try to extract values.
        if (parts.Length > 1)
        {
            AppendJournalValues(parts[1], line.Values);
        }
    }

    /// <summary>
    /// Extracts directive key/values and updates applicable journal metadata.
    /// </summary>
    /// <param name="line">Directive record.</param>
    /// <param name="context">Shared parse context.</param>
    /// <exception cref="FormatException">
    /// Expected Version/Username directive values are missing or malformed.
    /// </exception>
    private static void ParseDirective(
        JournalDirectiveLine line,
        JournalParseContext context)
    {
        var parts = line.RawText.Split(
            "\"  , ",
            StringSplitOptions.None);

        if (parts.Length > 1)
        {
            // Return an empty string when the starting position is at or beyond the end of the text.
            var keyCandidate = parts[0][Math.Min(15, parts[0].Length)..];

            // Allow for different formatting in Revit 2022.
            if (keyCandidate.StartsWith("\"", StringComparison.Ordinal))
            {
                keyCandidate = keyCandidate[1..];
            }

            line.Key = keyCandidate;
            AppendJournalValues(parts[1], line.Values);
        }
        else
        {
            // Very rare case where a multiline error message is inserted between
            // the key and values.
            var quotedParts = line.RawText.Split('"');

            if (quotedParts.Length > 1)
            {
                line.Key = quotedParts[1];
            }

            if (quotedParts.Length > 2)
            {
                // Unlike the normal branch, store this segment unchanged.
                line.Values.Add(quotedParts[2]);
            }
        }

        if (line.Key == "Version")
        {
            if (line.Values.Count == 0)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse version directive at physical line {line.Number}: " +
                        "the version value is missing."));
            }

            // Take at most the first four characters before converting the result to an integer.
            var value = line.Values[0];
            var versionText = value[..Math.Min(4, value.Length)];

            if (!int.TryParse(
                versionText,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var version))
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse version directive at physical line {line.Number}: " +
                        "the version value does not begin with a valid year."));
            }

            // Later directives overwrite earlier metadata.
            context.Version = version;
        }
        else if (line.Key == "Username")
        {
            if (line.Values.Count == 0)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse username directive at physical line {line.Number}: " +
                        "the username value is missing."));
            }

            context.UserName = line.Values[0];
        }
    }

    /// <summary>
    /// Extracts GUI resource-usage counters from a GUI metrics record.
    /// </summary>
    /// <param name="line">GUI resource-usage record.</param>
    /// <exception cref="FormatException">
    /// Numeric counter parsing fails or expected segments are missing.
    /// </exception>
    private static void ParseGuiResourceUsage(JournalGuiResourceUsageLine line)
    {
        var values = new System.Collections.Generic.List<int>();

        foreach (var segment in line.RawText.Split(',', StringSplitOptions.None))
        {
            // Whitespace-collapsing tokenization.
            var tokens = segment.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                        "a comma-separated segment is empty."));
            }

            try
            {
                values.Add(int.Parse(
                    tokens[^1],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse GUI resource usage at physical line {line.Number}."),
                    ex);
            }
        }

        if (values.Count == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                    "no numeric counters were found."));
        }

        line.Available = values[0];

        if (values.Count > 1)
        {
            line.Used = values[1];

            if (values.Count > 2)
            {
                line.User = values[2];
            }
        }
    }

    /// <summary>
    /// Extracts keyboard key token from a keyboard-event record.
    /// </summary>
    /// <param name="line">Keyboard-event record.</param>
    /// <exception cref="FormatException">Quoted key token is missing.</exception>
    private static void ParseKeyboardEvent(JournalKeyboardEventLine line)
    {
        // Identify the key by taking the substring after the first 10 characters of the first segment.
        var parts = line.RawText.Split('"', StringSplitOptions.None);

        if (parts.Length < 2)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse keyboard event at physical line {line.Number}: " +
                    "the quoted key token is missing."));
        }

        line.Key = parts[1];
    }

    /// <summary>
    /// Extracts mouse-event type and integer payload values.
    /// </summary>
    /// <param name="line">Mouse-event record.</param>
    /// <exception cref="FormatException">One or more payload values cannot be parsed as integers.</exception>
    private static void ParseMouseEvent(JournalMouseEventLine line)
    {
        // Identify the mouse event type by taking the substring after the first 4 characters of the first segment.
        var m1 = line.RawText.Split(" ", 2, StringSplitOptions.None);
        var eventToken = m1[0].Length >= 4 ? m1[0][4..].Trim() : string.Empty;
        line.MouseEventType = ParseMouseEventType(eventToken);

        if (m1.Length <= 1)
        {
            return;
        }

        foreach (var part in m1[1].Split(',', StringSplitOptions.None))
        {
            // Strip whitespace and trailing null characters.
            var candidate = part.Trim().TrimEnd('\0');

            // Skip empty fragments.
            if (candidate.Length == 0)
            {
                continue;
            }

            // Convert to integer.
            try
            {
                line.Data.Add(int.Parse(
                    candidate,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse mouse event value at physical line {line.Number}."),
                    ex);
            }
        }
    }

    /// <summary>
    /// Maps raw mouse-event token text to known mouse-event types.
    /// </summary>
    /// <param name="token">Raw mouse-event token.</param>
    private static JournalMouseEventType ParseMouseEventType(string token)
    {
        return token switch
        {
            "LButtonDblClk" => JournalMouseEventType.LButtonDblClk,
            "LButtonDown" => JournalMouseEventType.LButtonDown,
            "LButtonUp" => JournalMouseEventType.LButtonUp,
            "MButtonDblClk" => JournalMouseEventType.MButtonDblClk,
            "MButtonDown" => JournalMouseEventType.MButtonDown,
            "MButtonUp" => JournalMouseEventType.MButtonUp,
            "MouseMove" => JournalMouseEventType.MouseMove,
            "RButtonDblClk" => JournalMouseEventType.RButtonDblClk,
            "RButtonDown" => JournalMouseEventType.RButtonDown,
            "RButtonUp" => JournalMouseEventType.RButtonUp,
            "Scroll" => JournalMouseEventType.Scroll,
            "Wheel" => JournalMouseEventType.Wheel,
            _ => JournalMouseEventType.Unknown
        };
    }

    /// <summary>
    /// Extracts VM/RAM metrics from memory-metrics records.
    /// </summary>
    /// <param name="line">Memory-metrics record.</param>
    /// <exception cref="FormatException">
    /// Required payload segments are missing or contain unparseable datetime/numeric data.
    /// </exception>
    private static void ParseMemoryMetrics(JournalMemoryMetricsLine line)
    {
        string[] tokens;

        // Extract the relevant portion of the raw text.
        if (line.RawText.Contains("Initial VM", StringComparison.Ordinal))
        {
            var split = line.RawText.Split(':', 3, StringSplitOptions.None);

            if (split.Length < 3)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse memory metrics at physical line {line.Number}: " +
                        "the initial VM payload is incomplete."));
            }

            tokens = split[2]
                .Replace(";", string.Empty, StringComparison.Ordinal)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            var split = line.RawText.Split(':', 7, StringSplitOptions.None);

            if (split.Length < 7)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse memory metrics at physical line {line.Number}: " +
                        "the delta VM payload is incomplete."));
            }

            tokens = split[6]
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }

        var numbers = new List<int>();
        var labels = new List<string>();

        foreach (var token in tokens)
        {
            if (int.TryParse(
                token,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var number))
            {
                numbers.Add(number);
            }
            else if (token is "Avail" or "Used" or "Peak")
            {
                labels.Add(token);
            }
        }

        if (numbers.Count > 0)
        {
            line.VmAvailable = numbers[0];
        }

        if (numbers.Count > 1)
        {
            line.VmUsed = numbers[1];
        }

        if (numbers.Count == 6)
        {
            line.VmPeak = numbers[2];
            line.RamAvailable = numbers[3];
            line.RamUsed = numbers[4];
            line.RamPeak = numbers[5];
        }
        else if (numbers.Count == 5)
        {
            if (labels.Count > 2 && labels[2] == "Avail")
            {
                line.VmPeak = null;
                line.RamAvailable = numbers[2];
                line.RamUsed = numbers[3];
                line.RamPeak = numbers[4];
            }
            else if (labels.Count > 2 && labels[2] == "Peak")
            {
                line.VmPeak = numbers[2];
                line.RamAvailable = numbers[3];
                line.RamUsed = numbers[4];
                line.RamPeak = null;
            }
        }
        else if (numbers.Count == 4)
        {
            line.VmPeak = null;
            line.RamAvailable = numbers[2];
            line.RamUsed = numbers[3];
            line.RamPeak = null;
        }
    }

    /// <summary>
    /// Extracts system-information key/value fields, item numbering, and first OS caption.
    /// </summary>
    /// <param name="line">System-information record.</param>
    /// <param name="context">Shared parse context.</param>
    private static void ParseSystemInformation(
        JournalSystemInformationLine line,
        JournalParseContext context)
    {
        const string marker = ":<    ";

        // Take the segment after the last occurrence of the marker.
        // If it is absent, the entire record is used.
        var markerIndex = line.RawText.LastIndexOf(
            marker,
            StringComparison.Ordinal);

        var payload = markerIndex >= 0
            ? line.RawText[(markerIndex + marker.Length)..]
            : line.RawText;

        var fields = payload.Split(" : ", StringSplitOptions.None);

        // Preserve the extracted text without additional trimming. If multiple
        // separators exist, only the first two segments are used.
        line.Key = fields.Length > 0 ? fields[0] : string.Empty;
        line.Value = fields.Length > 1 ? fields[1] : null;

        // Shared counter - it does not reset when the
        // system-information section changes.
        if ((line.SystemInformationType == JournalSystemInformationType.Processor &&
             line.Key == "AddressWidth") ||
            (line.SystemInformationType == JournalSystemInformationType.VideoController &&
             line.Key == "AdapterCompatibility") ||
            (line.SystemInformationType == JournalSystemInformationType.Printer &&
             line.Key == "Caption") ||
            (line.SystemInformationType == JournalSystemInformationType.PrinterConfiguration &&
             line.Key == "Color"))
        {
            context.SystemInformationItemNumber++;
        }

        // Only the first matching caption is retained.
        if (!context.OsVersionFound &&
            line.RawText.Contains("Caption :", StringComparison.Ordinal))
        {
            var lastColon = line.RawText.LastIndexOf(':');
            context.OsVersion = line.RawText[(lastColon + 1)..].Trim();
            context.OsVersionFound = true;
        }

        line.ItemNumber = context.SystemInformationItemNumber;
    }

    /// <summary>
    /// Extracts timestamp marker, datetime, description, and debug category.
    /// </summary>
    /// <param name="line">Timestamp record.</param>
    /// <exception cref="FormatException">
    /// Prefix is incomplete or datetime cannot be parsed.
    /// </exception>
    private static void ParseTimeStamp(JournalTimeStampLine line)
    {
        if (line.RawText.Length < 2)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse timestamp at physical line {line.Number}: " +
                    "the timestamp prefix is incomplete."));
        }

        line.TimeStampType = ParseTimeStampType(line.RawText[1]);

        // Split the raw text by ';' without removing empty entries.
        var ts1 = line.RawText.Split(';', StringSplitOptions.None);

        // If this line is cut off, do not try to extract datetime or description.
        if (ts1.Length <= 1)
        {
            return;
        }

        // Extract the datetime portion of the timestamp.
        var dateTimeText = ts1[0].Length >= 3 ? ts1[0][3..] : string.Empty;
        try
        {
            line.DateTime = DateTime.Parse(
                dateTimeText,
                System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse timestamp datetime at physical line {line.Number}."),
                ex);
        }

        // Formatting until Revit 2019: contains ':<'
        // Formatting as of Revit 2020: no ':<'
        if (ts1[1].Contains(":<", StringComparison.Ordinal))
        {
            line.Description = ts1[1].Length >= 7
                ? ts1[1][7..].Trim()
                : string.Empty;
        }
        else
        {
            line.Description = ts1[1].Length >= 2
                ? ts1[1][2..].Trim()
                : string.Empty;
        }

        // Debug prefixes are mutually exclusive in the source parser.
        if (line.Description.StartsWith("DBG_ERROR:", StringComparison.Ordinal))
        {
            line.DebugInfoType = JournalDebugInfoType.Error;
            line.Description = line.Description.Replace("DBG_ERROR: ", string.Empty, StringComparison.Ordinal);
        }
        else if (line.Description.StartsWith("DBG_WARN:", StringComparison.Ordinal))
        {
            line.DebugInfoType = JournalDebugInfoType.Warning;
            line.Description = line.Description.Replace("DBG_WARN: ", string.Empty, StringComparison.Ordinal);
        }
        else if (line.Description.StartsWith("DBG_INFO:", StringComparison.Ordinal))
        {
            line.DebugInfoType = JournalDebugInfoType.Info;
            line.Description = line.Description.Replace("DBG_INFO: ", string.Empty, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Maps single-character timestamp marker to known timestamp type.
    /// </summary>
    /// <param name="marker">Timestamp marker character.</param>
    private static JournalTimeStampType ParseTimeStampType(char marker)
    {
        return marker switch
        {
            'C' => JournalTimeStampType.C,
            'E' => JournalTimeStampType.E,
            'H' => JournalTimeStampType.H,
            _ => JournalTimeStampType.Unknown
        };
    }

    /// <summary>
    /// Extracts UI event type and tokenized payload data.
    /// </summary>
    /// <param name="line">UI event record.</param>
    private static void ParseUiEvent(JournalUiEventLine line)
    {
        // Split the raw text by the first space.
        var d1 = line.RawText.Split(" ", 2, StringSplitOptions.None);

        // Extract the event token.
        var eventToken = d1[0].Length >= 4 ? d1[0][4..] : string.Empty;
        line.UiEventType = ParseUiEventType(eventToken);

        // Check if there is payload text for most event types.
        if (d1.Length <= 1)
        {
            return;
        }

        // Skip payload parsing for Maximize/Minimize/Restore.
        if (line.UiEventType is JournalUiEventType.Maximize
            or JournalUiEventType.Minimize
            or JournalUiEventType.Restore)
        {
            return;
        }

        foreach (var d2 in d1[1].Split(',', StringSplitOptions.None))
        {
            // Cleanup order: trim, remove quotes, trim again.
            var d3 = d2.Trim().Replace("\"", string.Empty, StringComparison.Ordinal).Trim();

            if (line.UiEventType is JournalUiEventType.RibbonEvent
                or JournalUiEventType.SBTrayAction)
            {
                foreach (var d4Token in d3.Split(':', StringSplitOptions.None))
                {
                    var d4 = d4Token.Trim();
                    if (d4.Length > 0)
                    {
                        line.Data.Add(d4);
                    }
                }

                continue;
            }

            if (line.UiEventType == JournalUiEventType.Browser)
            {
                foreach (var d4Token in d3.Split(">>", StringSplitOptions.None))
                {
                    var d4 = d4Token.Trim();
                    if (d4.Length > 0)
                    {
                        line.Data.Add(d4);
                    }
                }

                continue;
            }

            if (d3.Length > 0)
            {
                line.Data.Add(d3);
            }
        }
    }

    /// <summary>
    /// Maps raw UI event token text to known UI event types.
    /// </summary>
    /// <param name="token">Raw UI event token.</param>
    private static JournalUiEventType ParseUiEventType(string token)
    {
        return token switch
        {
            "Activate" => JournalUiEventType.Activate,
            "AppButtonEvent" => JournalUiEventType.AppButtonEvent,
            "Browser" => JournalUiEventType.Browser,
            "CheckBox" => JournalUiEventType.CheckBox,
            "Close" => JournalUiEventType.Close,
            "ComboBox" => JournalUiEventType.ComboBox,
            "DropFiles" => JournalUiEventType.DropFiles,
            "Edit" => JournalUiEventType.Edit,
            "Grid" => JournalUiEventType.Grid,
            "ListBox" => JournalUiEventType.ListBox,
            "Maximize" => JournalUiEventType.Maximize,
            "Minimize" => JournalUiEventType.Minimize,
            "PropertiesPalette" => JournalUiEventType.PropertiesPalette,
            "PushButton" => JournalUiEventType.PushButton,
            "RadioButton" => JournalUiEventType.RadioButton,
            "RibbonEvent" => JournalUiEventType.RibbonEvent,
            "SBTrayAction" => JournalUiEventType.SBTrayAction,
            "Size" => JournalUiEventType.Size,
            "SliderCtrl" => JournalUiEventType.SliderCtrl,
            "TabCtrl" => JournalUiEventType.TabCtrl,
            "TreeCtrl" => JournalUiEventType.TreeCtrl,
            "WidgetEvent" => JournalUiEventType.WidgetEvent,
            "Restore" => JournalUiEventType.Restore,
            _ => JournalUiEventType.Unknown
        };
    }

    /// <summary>
    /// Extracts basic-file-info fields from a <c>[Jrn.BasicFileInfo]</c> record.
    /// </summary>
    /// <param name="line">Basic-file-info record.</param>
    private static void ParseBasicFileInfo(JournalBasicFileInfoLine line)
    {
        // Extract basic file information attributes.
        var attributes = ParseBasicFileInfoAttributes(line.RawText);

        // Missing or empty values do not overwrite nulls.
        if (attributes.TryGetValue("Worksharing", out var worksharing) && worksharing.Length > 0)
        {
            line.Worksharing = worksharing;
        }

        if (attributes.TryGetValue("CentralModelPath", out var centralModelPath) && centralModelPath.Length > 0)
        {
            line.CentralModelPath = centralModelPath;
        }

        if (attributes.TryGetValue("LastSavePath", out var lastSavePath) && lastSavePath.Length > 0)
        {
            line.LastSavePath = lastSavePath;

            // Use backslash split for file name extraction.
            var fileNameParts = lastSavePath.Split('\\', StringSplitOptions.None);
            line.FileName = fileNameParts[^1];
        }

        if (attributes.TryGetValue("LocaleWhenSaved", out var locale) && locale.Length > 0)
        {
            line.Locale = locale;
        }
    }

    /// <summary>
    /// Extracts worksharing session ID, datetime, and descriptive text.
    /// </summary>
    /// <param name="line">Worksharing-event record.</param>
    /// <exception cref="FormatException">Worksharing datetime parsing fails.</exception>
    private static void ParseWorksharingEvent(JournalWorksharingEventLine line)
    {
        // Split the raw text by ':< SLOG ' and take the last part.
        var slogParts = line.RawText.Split(":< SLOG ", StringSplitOptions.None);
        var payload = slogParts[^1];

        // Split the payload by whitespace, removing empty entries.
        var ws = payload.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (ws.Length == 0)
        {
            return;
        }

        line.SessionId = ws[0];

        if (ws.Length > 2)
        {
            var dateTimeText = ws[1] + " " + ws[2];

            // Parse the datetime without explicit culture.
            try
            {
                line.DateTime = DateTime.Parse(dateTimeText);
            }
            catch (Exception ex)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse worksharing datetime at physical line {line.Number}."),
                    ex);
            }
        }

        if (ws.Length > 3)
        {
            // Join the remaining parts of the payload with spaces.
            line.Text = string.Join(" ", ws, 3, ws.Length - 3);
        }
    }

    /// <summary>
    /// Appends comma-separated journal values to a destination collection.
    /// </summary>
    /// <param name="text">Raw comma-separated text segment.</param>
    /// <param name="values">Destination value collection.</param>
    /// <remarks>
    /// This intentionally is not a CSV parser:
    /// commas inside quoted strings still split values, and empty entries are retained.
    /// </remarks>
    private static void AppendJournalValues(
        string text,
        System.Collections.Generic.List<string> values)
    {
        // This intentionally is not a CSV parser: commas inside quoted strings
        // still split values, and empty entries are retained.
        foreach (var value in text.Split(',', StringSplitOptions.None))
        {
            // Trim first, then remove double quotes.
            // Spaces originally inside quotes therefore remain in the result.
            values.Add(value.Trim().Replace("\"", string.Empty, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Builds a parse error message including the source raw text.
    /// </summary>
    /// <param name="line">Source record.</param>
    /// <param name="message">Primary error message.</param>
    private static string BuildParseErrorMessage(JournalLine line, string message)
    {
        return $"{message}{Environment.NewLine}RawText: {line.RawText}";
    }
}