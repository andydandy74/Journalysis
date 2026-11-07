using System;
using System.Collections.Generic;
using System.Text;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents a single command entry (<c>ShortcutItem</c>) in a Revit keyboard shortcuts file.
/// </summary>
/// <remarks>
/// Instances are created by <see cref="KeyboardShortcutsParser"/> while parsing a shortcuts file.
/// The supplied shortcut and path sequences are materialized during construction.
/// </remarks>
public sealed class KeyboardShortcutCommand
{
    /// <summary>
    /// Initializes a keyboard shortcut command.
    /// </summary>
    /// <param name="name">Display name of the command.</param>
    /// <param name="id">Case-sensitive command ID, for example <c>ID_EDIT_COPY</c>.</param>
    /// <param name="shortcuts">Key combinations assigned to the command.</param>
    /// <param name="paths">UI paths (ribbon locations) where the command can be found.</param>
    /// <exception cref="ArgumentNullException">
    /// Any argument is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// This constructor is intended for use by the keyboard shortcuts parser.
    /// </remarks>
    internal KeyboardShortcutCommand(
        string name,
        string id,
        IEnumerable<string> shortcuts,
        IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(shortcuts);
        ArgumentNullException.ThrowIfNull(paths);

        Name = name;
        Id = id;
        Shortcuts = shortcuts.ToArray();
        Paths = paths.ToArray();
    }

    /// <summary>
    /// Gets a value indicating whether the command has at least one UI path.
    /// </summary>
    public bool HasPaths => Paths.Count > 0;

    /// <summary>
    /// Gets a value indicating whether the command has at least one shortcut.
    /// </summary>
    public bool HasShortcuts => Shortcuts.Count > 0;

    /// <summary>
    /// Gets the command ID.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name of the command.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the UI paths where the command can be found.
    /// </summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>
    /// Gets the key combinations assigned to the command.
    /// </summary>
    public IReadOnlyList<string> Shortcuts { get; }

    /// <summary>
    /// Returns the type name of this instance.
    /// </summary>
    public override string ToString() => nameof(KeyboardShortcutCommand);
}