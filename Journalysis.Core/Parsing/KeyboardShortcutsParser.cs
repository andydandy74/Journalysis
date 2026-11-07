using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Parses Revit keyboard shortcuts XML files exported through Revit's
/// Keyboard Shortcuts dialog.
/// </summary>
/// <remarks>
/// The parser reads direct <c>ShortcutItem</c> children of the document root.
/// Each item must provide the <c>CommandName</c> and <c>CommandId</c> attributes.
/// The <c>Shortcuts</c> and <c>Paths</c> attributes are optional.
///
/// <para>
/// Shortcut values in the <c>Shortcuts</c> attribute are separated by
/// <c>#</c>. Path values in the <c>Paths</c> attribute are separated by
/// <c>"; "</c>.
/// </para>
///
/// <para>
/// Some shortcuts are hardcoded in Revit and are not included in exported files.
/// These shortcuts are appended to the shortcuts read from the file.
/// </para>
///
/// <para>
/// If the document has no root element, an empty
/// <see cref="KeyboardShortcuts"/> collection is returned.
/// </para>
/// </remarks>
public sealed class KeyboardShortcutsParser
{
    /// <summary>
    /// Gets the shortcuts that Revit assigns internally and does not include
    /// in exported keyboard shortcuts files.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> HardcodedShortcuts =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            ["ID_APP_EXIT"] = ["Alt+Fn4"],
            ["ID_BUTTON_DELETE"] = ["Delete"],
            ["ID_BUTTON_REDO"] = ["Ctrl+Y", "Ctrl+Shift+Z"],
            ["ID_BUTTON_UNDO"] = ["Ctrl+Z", "Alt+Backspace"],
            ["ID_CHECK_SPELLING"] = ["Fn7"],
            ["ID_EDIT_COPY"] = ["Ctrl+C", "Ctrl+Insert"],
            ["ID_EDIT_CUT"] = ["Ctrl+X", "Ctrl+Delete"],
            ["ID_EDIT_PASTE"] = ["Ctrl+V"],
            ["ID_FILE_NEW_CHOOSE_TEMPLATE"] = ["Ctrl+N"],
            ["ID_REVIT_FILE_CLOSE"] = ["Ctrl+W"],
            ["ID_REVIT_FILE_OPEN"] = ["Ctrl+O"],
            ["ID_REVIT_FILE_PRINT"] = ["Ctrl+P"],
            ["ID_REVIT_FILE_SAVE"] = ["Ctrl+S"],
            ["ID_SCHEDULE_VIEW_ZOOM_IN"] = ["Ctrl++"],
            ["ID_SCHEDULE_VIEW_ZOOM_OUT"] = ["Ctrl+-"],
            ["ID_SCHEDULE_VIEW_ZOOM_RESTORE"] = ["Ctrl+0"],
        };

    private const string ShortcutItemElement = "ShortcutItem";
    private const string ShortcutSeparator = "#";
    private const string PathSeparator = "; ";

    /// <summary>
    /// Parses a Revit keyboard shortcuts XML file from a file path.
    /// </summary>
    /// <param name="path">Path to the keyboard shortcuts XML file.</param>
    /// <returns>
    /// A <see cref="KeyboardShortcuts"/> instance containing the parsed commands
    /// in their original file order.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is <see langword="null"/>, empty, or consists only
    /// of whitespace.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// A <c>ShortcutItem</c> is missing the required <c>CommandName</c> or
    /// <c>CommandId</c> attribute.
    /// </exception>
    /// <exception cref="XmlException">
    /// The file does not contain well-formed XML.
    /// </exception>
    public KeyboardShortcuts Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path);
        return Parse(reader);
    }

    /// <summary>
    /// Parses Revit keyboard shortcuts XML from a text reader.
    /// </summary>
    /// <param name="reader">
    /// Reader containing the keyboard shortcuts XML. The caller retains ownership
    /// of the reader, which is not disposed by this method.
    /// </param>
    /// <returns>
    /// A <see cref="KeyboardShortcuts"/> instance containing the parsed commands
    /// in their original file order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="reader"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// A <c>ShortcutItem</c> is missing the required <c>CommandName</c> or
    /// <c>CommandId</c> attribute.
    /// </exception>
    /// <exception cref="XmlException">
    /// The input does not contain well-formed XML.
    /// </exception>
    public KeyboardShortcuts Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var document = XDocument.Load(reader, LoadOptions.SetLineInfo);

        // Only direct children of the root element are commands, matching the original parser.
        var commands = document.Root?
            .Elements(ShortcutItemElement)
            .Select(ParseCommand)
            ?? [];

        return new KeyboardShortcuts(commands);
    }

    /// <summary>
    /// Formats the source line information for an XML element.
    /// </summary>
    /// <param name="item">XML element for which line information is requested.</param>
    /// <returns>
    /// A formatted line suffix when line information is available; otherwise,
    /// <see cref="string.Empty"/>.
    /// </returns>
    private static string FormatLineInfo(XElement item) =>
        item is IXmlLineInfo info && info.HasLineInfo() ? $" (line {info.LineNumber})" : string.Empty;

    /// <summary>
    /// Gets a required attribute from a <c>ShortcutItem</c> element.
    /// </summary>
    /// <param name="item">XML element containing the attribute.</param>
    /// <param name="attributeName">Name of the required attribute.</param>
    /// <returns>The value of the specified attribute.</returns>
    /// <exception cref="InvalidDataException">
    /// The specified attribute is missing.
    /// </exception>
    private static string GetRequiredAttribute(XElement item, string attributeName) =>
        (string?)item.Attribute(attributeName)
        ?? throw new InvalidDataException(
            $"{ShortcutItemElement} is missing the required '{attributeName}' attribute" +
            $"{FormatLineInfo(item)}.");

    /// <summary>
    /// Parses a single <c>ShortcutItem</c> XML element into a command.
    /// </summary>
    /// <param name="item">XML element representing the command.</param>
    /// <returns>
    /// The parsed command, including any hardcoded shortcuts associated with its ID.
    /// </returns>
    /// <exception cref="InvalidDataException">
    /// The element is missing the required <c>CommandName</c> or <c>CommandId</c>
    /// attribute.
    /// </exception>
    private static KeyboardShortcutCommand ParseCommand(XElement item)
    {
        var name = GetRequiredAttribute(item, "CommandName");
        var id = GetRequiredAttribute(item, "CommandId");

        var shortcuts = Split((string?)item.Attribute("Shortcuts"), ShortcutSeparator);

        // Exported shortcuts come first, followed by any hardcoded ones.
        if (HardcodedShortcuts.TryGetValue(id, out var hardcoded))
        {
            shortcuts = [.. shortcuts, .. hardcoded];
        }

        var paths = Split((string?)item.Attribute("Paths"), PathSeparator);

        return new KeyboardShortcutCommand(name, id, shortcuts, paths);
    }

    /// <summary>
    /// Splits an optional attribute value into non-empty values.
    /// </summary>
    /// <param name="value">Value to split, or <see langword="null"/>.</param>
    /// <param name="separator">Separator used to split the value.</param>
    /// <returns>
    /// A list containing the non-empty parts, or an empty list when
    /// <paramref name="value"/> is <see langword="null"/> or empty.
    /// </returns>
    private static IReadOnlyList<string> Split(string? value, string separator) =>
        string.IsNullOrEmpty(value)
            ? []
            : value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
}