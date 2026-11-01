using System;
using System.Collections.Generic;
using System.Text;
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
    /// The second quoted payload field is missing, or its closing quote is missing.
    /// Available second-field text is preserved when closing quote is missing.
    /// </exception>
    private static void ParseAddinEvent(JournalAddinEventLine line)
    {
        var fields = line.RawText.Split('"');

        // Need at least:
        // [0]=prefix, [1]=first field, [2]=separator, [3]=second field
        if (fields.Length < 4)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse add-in event at physical line {line.Number}: " +
                    "the second quoted field is missing."));
        }

        // Preserve available text (can be partial when the closing quote is missing).
        line.MessageText = fields[3];

        // A complete second quoted field requires the trailing segment after its closing quote.
        if (fields.Length < 5)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse add-in event at physical line {line.Number}: " +
                    "the closing quote of the second field is missing. " +
                    "The available message text was preserved."));
        }
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
    /// Extracts basic-file-info fields from a <c>[Jrn.BasicFileInfo]</c> record.
    /// </summary>
    /// <param name="line">Basic-file-info record.</param>
    private static void ParseBasicFileInfo(JournalBasicFileInfoLine line)
    {
        var attributes = ParseBasicFileInfoAttributes(line.RawText);

        if (attributes.TryGetValue("Worksharing", out var worksharing) &&
            worksharing.Length > 0)
        {
            line.Worksharing = worksharing;
        }

        if (attributes.TryGetValue("CentralModelPath", out var centralModelPath) &&
            centralModelPath.Length > 0)
        {
            line.CentralModelPath = centralModelPath;
        }

        if (attributes.TryGetValue("LastSavePath", out var lastSavePath) &&
            lastSavePath.Length > 0)
        {
            line.LastSavePath = lastSavePath;

            var fileNameParts = lastSavePath.Split(
                '\\',
                StringSplitOptions.None);

            line.FileName = fileNameParts[^1];
        }

        if (attributes.TryGetValue("LocaleWhenSaved", out var locale) &&
            locale.Length > 0)
        {
            line.Locale = locale;
        }

        if (attributes.TryGetValue("FileExt", out var fileExtension) &&
            fileExtension.Length > 0)
        {
            line.FileExtension = fileExtension;
        }

        if (attributes.TryGetValue("RevitBuildVersion", out var buildVersion) &&
            buildVersion.Length > 0)
        {
            const string versionPrefix = "Autodesk Revit ";
            var versionStart = buildVersion.IndexOf(
                versionPrefix,
                StringComparison.Ordinal);

            if (versionStart >= 0)
            {
                versionStart += versionPrefix.Length;

                var versionEnd = buildVersion.IndexOf(
                    ' ',
                    versionStart);

                if (versionEnd < 0)
                {
                    versionEnd = buildVersion.Length;
                }

                var versionText = buildVersion[
                    versionStart..versionEnd];

                if (int.TryParse(
                        versionText,
                        System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var version))
                {
                    line.Version = version;
                }
            }

            const string buildMarker = "(Build:";
            var buildStart = buildVersion.IndexOf(
                buildMarker,
                StringComparison.Ordinal);

            if (buildStart >= 0)
            {
                buildStart += buildMarker.Length;

                var buildEnd = buildVersion.LastIndexOf(')');

                if (buildEnd < buildStart)
                {
                    buildEnd = buildVersion.Length;
                }

                var build = buildVersion[
                    buildStart..buildEnd].Trim();

                if (build.Length > 0)
                {
                    line.Build = build;
                }
            }
        }
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
    private static Dictionary<string, string> ParseBasicFileInfoAttributes(
    string rawText)
    {
        const string recordMarker = "[Jrn.BasicFileInfo]";

        var markerIndex = rawText.IndexOf(
            recordMarker,
            StringComparison.Ordinal);

        if (markerIndex < 0)
        {
            return new Dictionary<string, string>(
                StringComparer.Ordinal);
        }

        var payload = rawText[
            (markerIndex + recordMarker.Length)..];

        var attrSegments = payload.Split(
            "Rvt.Attr.",
            StringSplitOptions.None);

        var dictionary = new Dictionary<string, string>(
            StringComparer.Ordinal);

        for (var i = 1; i < attrSegments.Length; i++)
        {
            var split = attrSegments[i].Split(
                ':',
                2,
                StringSplitOptions.None);

            if (split.Length < 2)
            {
                continue;
            }

            var key = split[0].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            dictionary[key] = split[1].Trim();
        }

        return dictionary;
    }

    /// <summary>
    /// Extracts command context, description, and command ID.
    /// </summary>
    /// <param name="line">Command record.</param>
    private static void ParseCommand(JournalCommandLine line)
    {
        // Replace double spaces with single spaces and split by the delimiter.
        var commandParts = line.RawText
            .Replace("  ", " ", StringComparison.Ordinal)
            .Split("\" , \"", StringSplitOptions.None);

        if (commandParts.Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the command type is missing."));
        }

        const string commandPrefix = "Jrn.Command \"";

        var commandTypeSegment = commandParts[0];

        if (!commandTypeSegment.StartsWith(commandPrefix, StringComparison.Ordinal) ||
            !commandTypeSegment.EndsWith("\"", StringComparison.Ordinal) ||
            commandTypeSegment.Length <= commandPrefix.Length)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the quoted command type is incomplete."));
        }

        var commandTypeText = commandTypeSegment[commandPrefix.Length..^1];
        line.CommandType = ParseCommandType(commandTypeText);

        // A complete command type without a payload is still malformed.
        if (commandParts.Length <= 1)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the command description and command ID are missing."));
        }

        var payloadParts = commandParts[1]
            .Split(" , ", StringSplitOptions.None);

        if (payloadParts.Length == 0 ||
            !payloadParts[0].StartsWith("\"", StringComparison.Ordinal))
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the quoted command description is incomplete."));
        }

        line.CommandDescription = payloadParts[0];

        if (payloadParts.Length <= 1)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the command ID is missing or incomplete."));
        }

        var rawCommandId = payloadParts[1];

        if (!rawCommandId.EndsWith("\"", StringComparison.Ordinal))
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    "the quoted command ID is incomplete."));
        }

        var commandId = rawCommandId[..^1];

        // Allow for different formatting in Revit 2022.
        if (commandId.EndsWith("\"", StringComparison.Ordinal))
        {
            commandId = commandId[..^1];
        }

        line.CommandId = commandId;

        if (line.CommandType == JournalCommandType.Unknown)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse command at physical line {line.Number}: " +
                    $"unknown command type '{commandTypeText}'."));
        }
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
            var recordedPath = fields.Length > 1
                ? fields[1].Trim()
                : null;

            context.RecordedPath = string.IsNullOrEmpty(recordedPath)
                ? null
                : recordedPath;

            if (context.RecordedPath is null)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse recorded journal path at physical line {line.Number}: " +
                        "the journal path is missing."));
            }

            return;
        }

        if (text.StartsWith("' Build:", StringComparison.Ordinal))
        {
            var fields = text.Split(':', StringSplitOptions.None);

            context.Build = fields.Length > 1
                ? ExtractBuildMetadataToken(fields[1])
                : null;

            context.Branch = fields.Length > 2
                ? ExtractBuildMetadataToken(fields[2])
                : null;

            context.Release = fields.Length > 3
                ? ExtractBuildMetadataToken(fields[3])
                : null;

            if (context.Build is null ||
                context.Branch is null ||
                context.Release is null)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse build metadata at physical line {line.Number}: " +
                        "at least one of build, branch, or release is missing."));
            }

            return;
        }

        if (!context.MachineNameFound &&
            text.Contains(
                "Additional IP address/name found for host",
                StringComparison.Ordinal))
        {
            const string marker = "Additional IP address/name found for host";
            var markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
            var hostAndAddress = text[(markerIndex + marker.Length)..].TrimStart();

            var colonIndex = hostAndAddress.IndexOf(':');

            if (colonIndex >= 0)
            {
                var machineName = hostAndAddress[..colonIndex].Trim();

                context.MachineName = string.IsNullOrEmpty(machineName)
                    ? null
                    : machineName;

                if (context.MachineName is null)
                {
                    throw new FormatException(
                        BuildParseErrorMessage(
                            line,
                            $"Cannot parse machine metadata at physical line {line.Number}: " +
                            "the machine name is missing before the colon."));
                }

                context.MachineNameFound = true;
                return;
            }

            // Without the colon we cannot know whether the machine name is complete.
            // Preserve the recognizable partial value, but report the malformed record.
            context.MachineName = string.IsNullOrEmpty(hostAndAddress)
                ? null
                : hostAndAddress;

            context.MachineNameFound = context.MachineName is not null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse machine metadata at physical line {line.Number}: " +
                    "the colon after the machine name is missing."));
        }
    }

    /// <summary>
    /// Extracts the first build-metadata token from a colon-delimited build segment.
    /// </summary>
    /// <param name="text">Build segment text.</param>
    /// <returns>Trimmed first token.</returns>
    private static string? ExtractBuildMetadataToken(string text)
    {
        // The separator is a literal space, not arbitrary whitespace.
        var token = text.TrimStart()
            .Split(' ', StringSplitOptions.None)[0]
            .Trim();

        return token.Length == 0 ? null : token;
    }

    /// <summary>
    /// Extracts <see cref="JournalDataLine.Key"/> and value tokens.
    /// </summary>
    /// <param name="line">Data record.</param>
    private static void ParseData(JournalDataLine line)
    {
        const string dataPrefix = "Jrn.Data";

        var rawText = line.RawText;
        var index = dataPrefix.Length;

        while (index < rawText.Length &&
               char.IsWhiteSpace(rawText[index]))
        {
            index++;
        }

        if (index >= rawText.Length ||
            rawText[index] != '"')
        {
            line.Key = null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse data at physical line {line.Number}: " +
                    "the quoted data key is missing."));
        }

        var keyStart = ++index;
        var keyEnd = rawText.IndexOf('"', keyStart);

        if (keyEnd < 0)
        {
            line.Key = rawText[keyStart..];

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse data at physical line {line.Number}: " +
                    "the quoted data key is incomplete."));
        }

        var key = rawText[keyStart..keyEnd];

        if (key.Length == 0)
        {
            line.Key = null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse data at physical line {line.Number}: " +
                    "the data key is empty."));
        }

        line.Key = key;
        index = keyEnd + 1;

        while (index < rawText.Length &&
               char.IsWhiteSpace(rawText[index]))
        {
            index++;
        }

        if (index >= rawText.Length ||
            rawText[index] != ',')
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse data at physical line {line.Number}: " +
                    "the data values are missing."));
        }

        ParseMultiValues(
            line,
            rawText[(index + 1)..],
            line.Values);
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
        const string directivePrefix = "Jrn.Directive";

        var rawText = line.RawText;
        var index = directivePrefix.Length;

        while (index < rawText.Length &&
               char.IsWhiteSpace(rawText[index]))
        {
            index++;
        }

        if (index >= rawText.Length ||
            rawText[index] != '"')
        {
            line.Key = null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse directive at physical line {line.Number}: " +
                    "the quoted directive key is missing."));
        }

        var keyStart = ++index;
        var keyEnd = rawText.IndexOf('"', keyStart);

        if (keyEnd < 0)
        {
            line.Key = rawText[keyStart..];

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse directive at physical line {line.Number}: " +
                    "the quoted directive key is incomplete."));
        }

        var key = rawText[keyStart..keyEnd];

        if (key.Length == 0)
        {
            line.Key = null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse directive at physical line {line.Number}: " +
                    "the directive key is empty."));
        }

        line.Key = key;
        index = keyEnd + 1;

        while (index < rawText.Length &&
               char.IsWhiteSpace(rawText[index]))
        {
            index++;
        }

        if (index < rawText.Length &&
            rawText[index] == ',')
        {
            ParseMultiValues(
                line,
                rawText[(index + 1)..],
                line.Values);
        }
        else
        {
            // Preserve the existing fallback behavior for an error message
            // between the key and the values.
            var quotedParts = rawText.Split('"');

            if (quotedParts.Length > 2)
            {
                var errorMessage = quotedParts[2];
                line.Values.Add(
                    errorMessage.Length == 0
                        ? null
                        : errorMessage);

                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse directive at physical line {line.Number}: " +
                        "an error message was stored as a value between " +
                        "the key and the values."));
            }

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse directive at physical line {line.Number}: " +
                    "the directive values are missing."));
        }

        if (line.Key == "Version")
        {
            if (line.Values.Count == 0 ||
                line.Values[0] is not { Length: > 0 } value)
            {
                context.Version = null;

                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse version directive at physical line {line.Number}: " +
                        "the version value is missing."));
            }

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

            context.Version = version;
        }
        else if (line.Key == "Username")
        {
            if (line.Values.Count == 0 ||
                line.Values[0] is null)
            {
                context.UserName = null;

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
        var segments = line.RawText.Split(',', StringSplitOptions.None);

        if (segments.Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                    "the resource usage payload is missing."));
        }

        static int ParseCounter(
            JournalGuiResourceUsageLine line,
            string segment,
            string counterName)
        {
            var tokens = segment
                .Trim()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            var counterIndex = Array.IndexOf(tokens, counterName);

            if (counterIndex < 0)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                        $"the '{counterName}' counter is missing."));
            }

            var valueIndex = counterIndex + 1;

            if (valueIndex >= tokens.Length ||
                !int.TryParse(
                    tokens[valueIndex],
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                        $"the value for '{counterName}' is missing or invalid."));
            }

            return value;
        }

        // Preserve successfully parsed values if a later part of the line is truncated.
        line.Available = ParseCounter(line, segments[0], "Avail");

        if (segments.Length < 2)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                    "the 'Used' counter is missing."));
        }

        line.Used = ParseCounter(line, segments[1], "Used");

        if (segments.Length < 3)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse GUI resource usage at physical line {line.Number}: " +
                    "the 'User' counter is missing."));
        }

        line.User = ParseCounter(line, segments[2], "Used");
    }

    /// <summary>
    /// Extracts keyboard key token from a keyboard-event record.
    /// </summary>
    /// <param name="line">Keyboard-event record.</param>
    /// <exception cref="FormatException">Quoted key token is missing.</exception>
    private static void ParseKeyboardEvent(JournalKeyboardEventLine line)
    {
        var openingQuote = line.RawText.IndexOf('"');

        if (openingQuote < 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse keyboard event at physical line {line.Number}: " +
                    "the quoted key token is missing."));
        }

        var closingQuote = line.RawText.IndexOf('"', openingQuote + 1);

        if (closingQuote < 0)
        {
            line.Key = line.RawText[(openingQuote + 1)..];

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse keyboard event at physical line {line.Number}: " +
                    "the quoted key token is incomplete."));
        }

        var key = line.RawText[(openingQuote + 1)..closingQuote];

        if (key.Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse keyboard event at physical line {line.Number}: " +
                    "the quoted key token is empty."));
        }

        line.Key = key;
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
        var rawText = line.RawText;

        var initialMarkerIndex = rawText.IndexOf(
            "Initial VM:",
            StringComparison.Ordinal);

        var deltaMarkerIndex = rawText.IndexOf(
            "Delta VM:",
            StringComparison.Ordinal);

        var isDelta = deltaMarkerIndex >= 0;
        var markerIndex = isDelta
            ? deltaMarkerIndex
            : initialMarkerIndex;

        var marker = isDelta
            ? "Delta VM:"
            : "Initial VM:";

        if (markerIndex < 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse memory metrics at physical line {line.Number}: " +
                    "the memory metrics payload is missing."));
        }

        var tokens = rawText[(markerIndex + marker.Length)..]
            .Split(
                [' ', '\t', ',', ';'],
                StringSplitOptions.RemoveEmptyEntries);

        var index = 0;
        var isRam = false;
        var metricFound = false;

        static FormatException IncompleteLine(JournalMemoryMetricsLine line)
        {
            return new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse memory metrics at physical line {line.Number}: " +
                    "the line is incomplete."));
        }

        static FormatException InvalidValue(
            JournalMemoryMetricsLine line,
            string value)
        {
            return new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse memory metrics at physical line {line.Number}: " +
                    $"'{value}' is not a valid integer."));
        }

        while (index < tokens.Length)
        {
            var token = tokens[index++];

            if (token == "RAM:")
            {
                isRam = true;
                continue;
            }

            if (token == "MB")
            {
                continue;
            }

            if (token is not ("Avail" or "Used" or "Peak"))
            {
                throw IncompleteLine(line);
            }

            metricFound = true;

            if (index >= tokens.Length)
            {
                throw IncompleteLine(line);
            }

            var firstValueText = tokens[index++];

            if (!int.TryParse(
                    firstValueText,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value))
            {
                throw InvalidValue(line, firstValueText);
            }

            // For Delta VM lines, the value after the arrow is authoritative.
            if (isDelta &&
                index < tokens.Length &&
                tokens[index] == "->")
            {
                index++;

                if (index >= tokens.Length)
                {
                    throw IncompleteLine(line);
                }

                var finalValueText = tokens[index++];

                if (!int.TryParse(
                        finalValueText,
                        System.Globalization.NumberStyles.Integer,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value))
                {
                    throw InvalidValue(line, finalValueText);
                }
            }

            switch (metricFound, isRam, token)
            {
                case (true, false, "Avail"):
                    line.VmAvailable = value;
                    break;

                case (true, false, "Used"):
                    line.VmUsed = value;
                    break;

                case (true, false, "Peak"):
                    line.VmPeak = value;
                    break;

                case (true, true, "Avail"):
                    line.RamAvailable = value;
                    break;

                case (true, true, "Used"):
                    line.RamUsed = value;
                    break;

                case (true, true, "Peak"):
                    line.RamPeak = value;
                    break;
            }
        }

        if (!metricFound)
        {
            throw IncompleteLine(line);
        }
    }

    /// <summary>
    /// Extracts mouse-event type and integer payload values.
    /// </summary>
    /// <param name="line">Mouse-event record.</param>
    /// <exception cref="FormatException">One or more payload values cannot be parsed as integers.</exception>
    private static void ParseMouseEvent(JournalMouseEventLine line)
    {
        var parts = line.RawText
            .TrimStart()
            .Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0 ||
            !parts[0].StartsWith("Jrn.", StringComparison.Ordinal))
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse mouse event at physical line {line.Number}: " +
                    "the mouse event type is missing."));
        }

        var eventToken = parts[0][4..];
        line.MouseEventType = ParseMouseEventType(eventToken);

        if (parts.Length < 2 ||
            parts[1].Trim().Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse mouse event at physical line {line.Number}: " +
                    "no values were found."));
        }

        foreach (var part in parts[1].Split(',', StringSplitOptions.None))
        {
            var candidate = part
                .Trim()
                .TrimEnd('\0')
                .Trim();

            if (candidate.Length == 0)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse mouse event at physical line {line.Number}: " +
                        "a value is missing."));
            }

            if (!int.TryParse(
                    candidate,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse mouse event at physical line {line.Number}: " +
                        $"'{candidate}' is not a valid integer."));
            }

            line.Data.Add(value);
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
    /// Extracts system-information key/value fields, item numbering, and first OS caption.
    /// </summary>
    /// <param name="line">System-information record.</param>
    /// <param name="context">Shared parse context.</param>
    private static void ParseSystemInformation(
    JournalSystemInformationLine line,
    JournalParseContext context)
    {
        const string marker = ":<    ";

        var markerIndex = line.RawText.LastIndexOf(
            marker,
            StringComparison.Ordinal);

        var payload = markerIndex >= 0
            ? line.RawText[(markerIndex + marker.Length)..]
            : line.RawText;

        var separatorIndex = payload.IndexOf(
            " : ",
            StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            line.Key = payload.Trim();
            line.Value = null;

            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse system information at physical line {line.Number}: " +
                    "the key/value separator is missing."));
        }

        line.Key = payload[..separatorIndex].Trim();

        var value = payload[(separatorIndex + 3)..].Trim();
        line.Value = value.Length == 0 ? null : value;

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

        if (!context.OsVersionFound &&
            line.SystemInformationType == JournalSystemInformationType.OperatingSystem &&
            line.Key == "Caption" &&
            line.Value is not null)
        {
            context.OsVersion = line.Value;
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
        var rawText = line.RawText;

        if (rawText.Length < 2)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse timestamp at physical line {line.Number}: " +
                    "the timestamp prefix is incomplete."));
        }

        var markerIndex = 1;

        // Support both "'C" and "' C" timestamp prefixes.
        if (char.IsWhiteSpace(rawText[markerIndex]))
        {
            markerIndex++;
        }

        if (markerIndex >= rawText.Length)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse timestamp at physical line {line.Number}: " +
                    "the timestamp marker is missing."));
        }

        line.TimeStampType = ParseTimeStampType(rawText[markerIndex]);

        var semicolonIndex = rawText.IndexOf(
            ';',
            markerIndex + 1);

        // Preserve the timestamp type when the record is truncated before
        // the datetime/description separator.
        if (semicolonIndex < 0)
        {
            return;
        }

        var dateTimeText = rawText[
            (markerIndex + 1)..semicolonIndex].Trim();

        if (dateTimeText.Length == 0)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse timestamp at physical line {line.Number}: " +
                    "the datetime value is missing."));
        }

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

        var description = rawText[(semicolonIndex + 1)..].Trim();

        var descriptionMarkerIndex = description.IndexOf(
            ":<",
            StringComparison.Ordinal);

        if (descriptionMarkerIndex >= 0)
        {
            description = description[
                (descriptionMarkerIndex + 2)..].Trim();
        }

        var debugInfoIndex = description.IndexOf(
            "DBG_ERROR:",
            StringComparison.Ordinal);

        if (debugInfoIndex >= 0)
        {
            line.DebugInfoType = JournalDebugInfoType.Error;

            description = (
                description[..debugInfoIndex] +
                description[(debugInfoIndex + "DBG_ERROR:".Length)..])
                .Trim();
        }
        else
        {
            debugInfoIndex = description.IndexOf(
                "DBG_WARN:",
                StringComparison.Ordinal);

            if (debugInfoIndex >= 0)
            {
                line.DebugInfoType = JournalDebugInfoType.Warning;

                description = (
                    description[..debugInfoIndex] +
                    description[(debugInfoIndex + "DBG_WARN:".Length)..])
                    .Trim();
            }
            else
            {
                debugInfoIndex = description.IndexOf(
                    "DBG_INFO:",
                    StringComparison.Ordinal);

                if (debugInfoIndex >= 0)
                {
                    line.DebugInfoType = JournalDebugInfoType.Info;

                    description = (
                        description[..debugInfoIndex] +
                        description[(debugInfoIndex + "DBG_INFO:".Length)..])
                        .Trim();
                }
            }
        }

        line.Description = description.Length == 0
            ? null
            : description;
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

        if (line.UiEventType == JournalUiEventType.RibbonEvent)
        {
            ParseRibbonEvent(line, d1[1]);
            return;
        }

        foreach (var d2 in d1[1].Split(',', StringSplitOptions.None))
        {
            // Cleanup order: trim, remove quotes, trim again.
            var d3 = d2.Trim().Replace("\"", string.Empty, StringComparison.Ordinal).Trim();

            if (line.UiEventType is JournalUiEventType.SBTrayAction)
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

    private static void ParseRibbonEvent(
    JournalUiEventLine line,
    string rawPayload)
    {
        var payload = rawPayload.Trim();

        if (payload.StartsWith('"'))
        {
            var valueBuilder = new StringBuilder();
            var closingQuoteFound = false;

            for (var index = 1; index < payload.Length; index++)
            {
                var character = payload[index];

                if (character == '"')
                {
                    // In journal text, doubled quotes represent one literal quote.
                    if (index + 1 < payload.Length &&
                        payload[index + 1] == '"')
                    {
                        valueBuilder.Append('"');
                        index++;
                        continue;
                    }

                    closingQuoteFound = true;
                    break;
                }

                valueBuilder.Append(character);
            }

            // Preserve the available value even when the line is truncated.
            payload = valueBuilder.ToString();

            _ = closingQuoteFound;
        }

        // The first two separators identify the Ribbon event and action.
        // Everything after them belongs to the optional payload, such as JSON.
        foreach (var token in payload.Split(
                     ':',
                     3,
                     StringSplitOptions.None))
        {
            var value = token.Trim();

            if (value.Length > 0)
            {
                line.Data.Add(value);
            }
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
    private static void ParseMultiValues(
    JournalLine line,
    string text,
    List<string?> values)
    {
        var index = 0;

        while (index < text.Length &&
               char.IsWhiteSpace(text[index]))
        {
            index++;
        }

        if (index >= text.Length)
        {
            throw new FormatException(
                BuildParseErrorMessage(
                    line,
                    $"Cannot parse values at physical line {line.Number}: " +
                    "the value list is empty."));
        }

        while (true)
        {
            while (index < text.Length &&
                   char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse values at physical line {line.Number}: " +
                        "a value is missing after the separator."));
            }

            if (text[index] == '"')
            {
                var valueStart = ++index;
                var valueEnd = text.IndexOf('"', valueStart);

                if (valueEnd < 0)
                {
                    throw new FormatException(
                        BuildParseErrorMessage(
                            line,
                            $"Cannot parse values at physical line {line.Number}: " +
                            "a quoted value is incomplete."));
                }

                var value = text[valueStart..valueEnd];

                // An explicitly empty quoted value represents null.
                values.Add(value.Length == 0 ? null : value);
                index = valueEnd + 1;
            }
            else
            {
                var valueStart = index;
                var commaIndex = text.IndexOf(',', valueStart);
                var valueEnd = commaIndex >= 0
                    ? commaIndex
                    : text.Length;

                var value = text[valueStart..valueEnd].Trim();

                if (value.Length == 0)
                {
                    values.Add(null);

                    throw new FormatException(
                        BuildParseErrorMessage(
                            line,
                            $"Cannot parse values at physical line {line.Number}: " +
                            "an unquoted value is empty."));
                }

                if (!IsNumericDataValue(value))
                {
                    throw new FormatException(
                        BuildParseErrorMessage(
                            line,
                            $"Cannot parse values at physical line {line.Number}: " +
                            $"the unquoted value '{value}' is not numeric."));
                }

                values.Add(value);
                index = valueEnd;
            }

            while (index < text.Length &&
                   char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index >= text.Length)
            {
                return;
            }

            if (text[index] != ',')
            {
                throw new FormatException(
                    BuildParseErrorMessage(
                        line,
                        $"Cannot parse values at physical line {line.Number}: " +
                        "a value separator is missing."));
            }

            index++;
        }
    }

    private static bool IsNumericDataValue(string value)
    {
        return double.TryParse(
            value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out _);
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