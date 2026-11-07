using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents the commands defined in a Revit keyboard shortcuts XML file.
/// </summary>
/// <remarks>
/// The command collection and the filtered collection of commands with shortcuts are
/// materialized when this instance is created.
/// </remarks>
public sealed class KeyboardShortcuts
{
    private readonly IReadOnlyList<KeyboardShortcutCommand> _commandsWithShortcuts;

    /// <summary>
    /// Initializes a keyboard shortcuts collection.
    /// </summary>
    /// <param name="commands">
    /// Commands read from the shortcuts file, in file order.
    /// The sequence is materialized by this constructor.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="commands"/> is <see langword="null"/>.
    /// </exception>
    internal KeyboardShortcuts(IEnumerable<KeyboardShortcutCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        Commands = commands.ToArray();
        _commandsWithShortcuts = Commands.Where(x => x.HasShortcuts).ToArray();
    }

    /// <summary>
    /// Gets the total number of commands.
    /// </summary>
    public int CommandCount => Commands.Count;

    /// <summary>
    /// Gets the number of commands that have at least one shortcut.
    /// </summary>
    public int CommandCountWithShortcuts => _commandsWithShortcuts.Count;

    /// <summary>
    /// Gets all commands, in file order.
    /// </summary>
    public IReadOnlyList<KeyboardShortcutCommand> Commands { get; }

    /// <summary>
    /// Gets all commands that have at least one shortcut, in file order.
    /// The result is materialized when this instance is created.
    /// </summary>
    public IReadOnlyList<KeyboardShortcutCommand> CommandsWithShortcuts =>
        _commandsWithShortcuts;

    /// <summary>
    /// Gets the first command with the given ID.
    /// </summary>
    /// <param name="id">
    /// Command ID to search for (case-insensitive, e.g. <c>ID_EDIT_COPY</c>).
    /// </param>
    /// <returns>
    /// The matching command, or <see langword="null"/> when none is found.
    /// </returns>
    public KeyboardShortcutCommand? GetCommandById(string id) =>
        Commands.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the type name of this instance.
    /// </summary>
    public override string ToString() => nameof(KeyboardShortcuts);
}