using System;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Routes assembled journal records to their type-specific parsing logic.
/// </summary>
internal static class JournalLineParser
{
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
    /// Extracts the second quoted field as the add-in event message.
    /// </summary>
    private static void ParseAddinEvent(JournalAddinEventLine line)
    {
        var fields = line.RawText.Split('"');

        // Match Python's split('"')[3]: do not remove empty fields or trim text.
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
    /// Extracts API message text and assigns its recognized category.
    /// </summary>
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

        // Preserve Python's split("{ ")[1].split(" }")[0] behavior.
        // A missing closing delimiter leaves the remaining text as the message.
        // A second opening delimiter ends the segment selected by the first split.
        var messageText = openingParts[1]
            .Split(" }", StringSplitOptions.None)[0]
            .Trim();

        if (messageText.StartsWith(": ", StringComparison.Ordinal))
        {
            messageText = messageText[2..];
        }

        line.MessageText = messageText;
        line.MessageType = ParseApiMessageType(messageText);
    }

    /// <summary>
    /// Classifies API messages in the same precedence order as the Python parser.
    /// </summary>
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
    /// Parses [Jrn.BasicFileInfo] key/value attributes, preserving Python semantics.
    /// </summary>
    private static Dictionary<string, string> ParseBasicFileInfoAttributes(string rawText)
    {
        // Preserve Python's [22:] slicing (safe even if text is shorter).
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
                // Python would drop these because zip(*) cannot pair them.
                continue;
            }

            pairs.Add(new KeyValuePair<string, string>(split[0], split[1].Trim()));
        }

        if (pairs.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        // Python dict(zip(...)) keeps the last duplicate key; emulate that behavior.
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            dict[pair.Key] = pair.Value;
        }

        return dict;
    }

    /// <summary>
    /// Extracts command context, description, and command identifier.
    /// </summary>
    private static void ParseCommand(JournalCommandLine line)
    {
        // Preserve Python behavior:
        // c1 = raw.replace("  "," ").split('" , "')
        var c1 = line.RawText
            .Replace("  ", " ", StringComparison.Ordinal)
            .Split("\" , \"", StringSplitOptions.None);

        if (c1.Length == 0)
        {
            return;
        }

        // Preserve Python's [13:] slicing.
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

        // Preserve Python's [:-1] behavior.
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
    /// Maps raw command context text to known command types.
    /// </summary>
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
    /// Extracts recorded journal path, build information, and machine metadata
    /// from comment records.
    /// </summary>
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
    /// Extracts a build-metadata token using the original whitespace rules.
    /// </summary>
    private static string ExtractBuildMetadataToken(string text)
    {
        // Match Python's lstrip().split(" ")[0].strip().
        // The separator is a literal space, not arbitrary whitespace.
        return text.TrimStart()
            .Split(' ', StringSplitOptions.None)[0]
            .Trim();
    }

    /// <summary>
    /// Extracts a data record's key and values.
    /// </summary>
    private static void ParseData(JournalDataLine line)
    {
        var parts = line.RawText.Split(
            "\"  , ",
            StringSplitOptions.None);

        // Preserve Python's [10:] slicing.
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
    /// Extracts a directive's key and values, and updates applicable journal metadata.
    /// </summary>
    private static void ParseDirective(
        JournalDirectiveLine line,
        JournalParseContext context)
    {
        var parts = line.RawText.Split(
            "\"  , ",
            StringSplitOptions.None);

        if (parts.Length > 1)
        {
            // Preserve Python's [15:] slicing, which returns an empty string when
            // the starting position is at or beyond the end of the text.
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
            // the key and values. Preserve the original quote-based fallback.
            var quotedParts = line.RawText.Split('"');

            if (quotedParts.Length > 1)
            {
                line.Key = quotedParts[1];
            }

            if (quotedParts.Length > 2)
            {
                // Unlike the normal branch, Python stores this segment unchanged.
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

            // Python takes at most the first four characters before converting
            // the result to an integer.
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

            // Later directives overwrite earlier metadata, matching Python.
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
    /// Extracts GUI resource usage counters from a journal GUI metrics record.
    /// </summary>
    private static void ParseGuiResourceUsage(JournalGuiResourceUsageLine line)
    {
        // Python behavior:
        // g2 = []
        // for g1 in raw.split(","): g2.append(int(g1.strip().split()[-1]))
        var values = new List<int>();

        foreach (var segment in line.RawText.Split(',', StringSplitOptions.None))
        {
            // Preserve Python split() semantics (whitespace-collapsing tokenization).
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
    /// Extracts the keyboard key token from a journal keyboard event record.
    /// </summary>
    private static void ParseKeyboardEvent(JournalKeyboardEventLine line)
    {
        // Python behavior:
        // line.Key = line.RawText.split('"')[1]
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
    /// Extracts the mouse event type and its integer payload values.
    /// </summary>
    private static void ParseMouseEvent(JournalMouseEventLine line)
    {
        // Preserve Python behavior:
        // m1 = raw.split(" ",1)
        var m1 = line.RawText.Split(" ", 2, StringSplitOptions.None);

        // Preserve Python slicing: m1[0][4:].strip()
        var eventToken = m1[0].Length >= 4 ? m1[0][4..].Trim() : string.Empty;
        line.MouseEventType = ParseMouseEventType(eventToken);

        if (m1.Length <= 1)
        {
            return;
        }

        foreach (var part in m1[1].Split(',', StringSplitOptions.None))
        {
            // Preserve Python cleanup:
            // potential_int = m2.strip().rstrip('\x00')
            var candidate = part.Trim().TrimEnd('\0');

            // Preserve Python behavior: skip empty fragments.
            if (candidate.Length == 0)
            {
                continue;
            }

            // Python calls int(...) directly; malformed values throw.
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
    /// Maps a raw mouse event token to a known mouse event type.
    /// </summary>
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
    /// Extracts virtual-memory and RAM metrics from a journal memory record.
    /// </summary>
    private static void ParseMemoryMetrics(JournalMemoryMetricsLine line)
    {
        string[] tokens;

        // Python behavior:
        // if "Initial VM" in raw: m1 = raw.split(":",2)[2].replace(";","").split()
        // else: m1 = raw.split(":",6)[6].split()
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

        // Python branch mapping preserved exactly.
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
    /// Extracts system-information fields, assigns the item number, and collects
    /// the first matching OS caption.
    /// </summary>
    private static void ParseSystemInformation(
        JournalSystemInformationLine line,
        JournalParseContext context)
    {
        const string marker = ":<    ";

        // Python takes the segment after the last occurrence of the marker.
        // If it is absent, the entire record is used.
        var markerIndex = line.RawText.LastIndexOf(
            marker,
            StringComparison.Ordinal);

        var payload = markerIndex >= 0
            ? line.RawText[(markerIndex + marker.Length)..]
            : line.RawText;

        var fields = payload.Split(" : ", StringSplitOptions.None);

        // Preserve the extracted text without additional trimming. If multiple
        // separators exist, Python uses only the first two segments.
        line.Key = fields.Length > 0 ? fields[0] : string.Empty;
        line.Value = fields.Length > 1 ? fields[1] : null;

        // Preserve the original shared counter: it does not reset when the
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

        // Match the original raw-text condition rather than restricting this to
        // the OperatingSystem section. Only the first matching caption is retained.
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

        // Preserve Python behavior:
        // ts1 = raw.split(";")
        var ts1 = line.RawText.Split(';', StringSplitOptions.None);

        // If this line is cut off, do not try to extract datetime or description.
        if (ts1.Length <= 1)
        {
            return;
        }

        // Preserve Python behavior: DateTime.Parse(ts1[0][3:])
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
    /// Maps the single-character timestamp marker to a known timestamp type.
    /// </summary>
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
    private static void ParseUiEvent(JournalUiEventLine line)
    {
        // Preserve Python behavior:
        // d1 = raw.split(" ",1)
        var d1 = line.RawText.Split(" ", 2, StringSplitOptions.None);

        // Preserve Python slicing: d1[0][4:]
        var eventToken = d1[0].Length >= 4 ? d1[0][4..] : string.Empty;
        line.UiEventType = ParseUiEventType(eventToken);

        // Python assumes there is payload text for most event types.
        // Keep that behavior while still guarding for malformed input.
        if (d1.Length <= 1)
        {
            return;
        }

        // Python skips payload parsing for Maximize/Minimize/Restore.
        if (line.UiEventType is JournalUiEventType.Maximize
            or JournalUiEventType.Minimize
            or JournalUiEventType.Restore)
        {
            return;
        }

        foreach (var d2 in d1[1].Split(',', StringSplitOptions.None))
        {
            // Preserve Python cleanup order:
            // d3 = d2.strip().replace('"','').strip()
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
    /// Maps a raw UI event token to a known UI event type.
    /// </summary>
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
    /// Extracts basic file information from a [Jrn.BasicFileInfo] record.
    /// </summary>
    private static void ParseBasicFileInfo(JournalBasicFileInfoLine line)
    {
        // Python behavior:
        // bfi = list(map(list, zip(*[x.split(":",1) for x in line.RawText[22:].split("Rvt.Attr.")[1:]])))
        // if len(bfi) > 1: bfidict = dict(zip(bfi[0], [x.strip() for x in bfi[1]]))
        var attributes = ParseBasicFileInfoAttributes(line.RawText);

        // Keep Python behavior: missing or empty values do not overwrite nulls.
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

            // Python uses backslash split for file name extraction.
            var fileNameParts = lastSavePath.Split('\\', StringSplitOptions.None);
            line.FileName = fileNameParts[^1];
        }

        if (attributes.TryGetValue("LocaleWhenSaved", out var locale) && locale.Length > 0)
        {
            line.Locale = locale;
        }
    }

    /// <summary>
    /// Extracts worksharing session id, datetime, and descriptive text.
    /// </summary>
    private static void ParseWorksharingEvent(JournalWorksharingEventLine line)
    {
        // Python behavior:
        // ws = line.RawText.split(':< SLOG ')[-1].split()
        var slogParts = line.RawText.Split(":< SLOG ", StringSplitOptions.None);
        var payload = slogParts[^1];

        // Python split() with no separator collapses whitespace.
        var ws = payload.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        if (ws.Length == 0)
        {
            return;
        }

        line.SessionId = ws[0];

        if (ws.Length > 2)
        {
            var dateTimeText = ws[1] + " " + ws[2];

            // Python uses DateTime.Parse without explicit culture.
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
            // Preserve Python's ' '.join(ws[3:]).
            line.Text = string.Join(" ", ws, 3, ws.Length - 3);
        }
    }

    /// <summary>
    /// Appends comma-separated values using the original journal parser's rules.
    /// </summary>
    private static void AppendJournalValues(
        string text,
        System.Collections.Generic.List<string> values)
    {
        // This intentionally is not a CSV parser: commas inside quoted strings
        // still split values, and empty entries are retained, matching Python.
        foreach (var value in text.Split(',', StringSplitOptions.None))
        {
            // Preserve the operation order: trim first, then remove double quotes.
            // Spaces originally inside quotes therefore remain in the result.
            values.Add(value.Trim().Replace("\"", string.Empty, StringComparison.Ordinal));
        }
    }

    private static string BuildParseErrorMessage(JournalLine line, string message)
    {
        return $"{message}{Environment.NewLine}RawText: {line.RawText}";
    }
}