namespace Journalysis.Core.Models;

/// <summary>
/// Identifies an API message category recognized by the journal parser.
/// </summary>
/// <remarks>
/// API message lines are journal comments that record API successes and API errors.
/// Classification is independent of whether the message is marked as an error.
/// </remarks>
public enum JournalApiMessageType
{
    /// <summary>
    /// Unrecognized API message pattern.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Message about adding a push button, typically to the Revit ribbon.
    /// </summary>
    AddedPushbutton,

    /// <summary>
    /// Message indicating an assembly version conflict.
    /// </summary>
    AssemblyVersionConflict,

    /// <summary>
    /// Message describing an exception condition during API or add-in execution.
    /// </summary>
    Exception,

    /// <summary>
    /// Message indicating external server registration.
    /// </summary>
    RegisteredExternalServer,

    /// <summary>
    /// Message indicating external service registration.
    /// </summary>
    RegisteredExternalService,

    /// <summary>
    /// Message indicating API command-event registration.
    /// </summary>
    RegisteringCommandEvent,

    /// <summary>
    /// Message indicating event registration.
    /// </summary>
    RegisteringEvent,

    /// <summary>
    /// Message indicating command ID replacement.
    /// </summary>
    ReplacingCommandId,

    /// <summary>
    /// Message indicating command ID restoration.
    /// </summary>
    RestoringCommandId,

    /// <summary>
    /// Message indicating startup of an external application.
    /// </summary>
    StartingExternalApplication,

    /// <summary>
    /// Message indicating startup of an external DB application.
    /// </summary>
    StartingExternalDbApplication,

    /// <summary>
    /// Message indicating API command-event unregistration.
    /// </summary>
    UnregisteringCommandEvent,

    /// <summary>
    /// Message indicating event unregistration.
    /// </summary>
    UnregisteringEvent
}

/// <summary>
/// Identifies the context from which a journal command was started.
/// </summary>
/// <remarks>
/// These values are based on command-type strings observed in Revit journals.
/// Use a nullable property to represent commands whose type has not yet been parsed.
/// Unknown represents an unrecognized parsed value.
/// </remarks>
public enum JournalCommandType
{
    /// <summary>
    /// Unrecognized command-context token parsed from the journal text.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Command started from an accelerator-key action.
    /// Typically includes keyboard-driven actions such as Cancel, Delete, or Undo.
    /// </summary>
    AccelKey,

    /// <summary>
    /// Command started from an internal Revit context.
    /// Commonly seen for startup-page display workflows and occasionally for other commands.
    /// </summary>
    Internal,

    /// <summary>
    /// Command started by a keyboard shortcut.
    /// </summary>
    KeyboardShortcut,

    /// <summary>
    /// Command started from the print preview UI.
    /// </summary>
    PrintPreviewUi,

    /// <summary>
    /// Command started by clicking a control in the Revit ribbon.
    /// </summary>
    Ribbon,

    /// <summary>
    /// Command started by interacting with the Revit startup page.
    /// </summary>
    StartupPage,

    /// <summary>
    /// Command started by clicking a control in the status bar.
    /// </summary>
    StatusBar,

    /// <summary>
    /// Command started from the system menu.
    /// In observed journals, this has primarily been used for exiting Revit.
    /// </summary>
    SystemMenu
}

/// <summary>
/// Identifies a debug-message category attached to a timestamp.
/// </summary>
/// <remarks>
/// Parsed from known timestamp-description prefixes: DBG_ERROR, DBG_WARN, and DBG_INFO.
/// Use a nullable property to represent timestamps without a debug marker.
/// Unknown represents an unrecognized parsed value, not the absence of a marker.
/// </remarks>
public enum JournalDebugInfoType
{
    /// <summary>
    /// Unrecognized debug-info token.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Error diagnostic category.
    /// </summary>
    Error,

    /// <summary>
    /// Informational diagnostic category.
    /// </summary>
    Info,

    /// <summary>
    /// Warning diagnostic category.
    /// </summary>
    Warning
}

/// <summary>
/// Identifies the category of a parsed journal record.
/// </summary>
/// <remarks>
/// Categories are semantic classifications used by parsing and analysis layers, not strict
/// guarantees from Autodesk.
/// </remarks>
public enum JournalLineKind
{
    /// <summary>
    /// Unrecognized record category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Add-in journaling record (Jrn.AddInEvent), typically used by add-ins
    /// to persist playback-relevant actions.
    /// </summary>
    AddinEvent,

    /// <summary>
    /// API success/error message comment (API_SUCCESS / API_ERROR),
    /// including add-in registration, startup, and exception-related messages.
    /// </summary>
    ApiMessage,

    /// <summary>
    /// Basic file metadata comment ([Jrn.BasicFileInfo]), such as worksharing mode,
    /// central path, last save path, and locale.
    /// </summary>
    BasicFileInfo,

    /// <summary>
    /// Built-in Revit command playback entry (Jrn.Command), including command context,
    /// description, and command ID.
    /// </summary>
    Command,

    /// <summary>
    /// General comment record that is not classified as API message, basic file info,
    /// GUI resource usage, memory metrics, timestamp, or worksharing event.
    /// </summary>
    Comment,

    /// <summary>
    /// Playback data record (Jrn.Data) containing a key and associated values
    /// used by Revit and add-ins.
    /// </summary>
    Data,

    /// <summary>
    /// Directive record (Jrn.Directive) that captures settings and startup/session
    /// metadata such as version and username.
    /// </summary>
    Directive,

    /// <summary>
    /// GUI resource-usage comment (GDI availability/usage counters).
    /// </summary>
    GuiResourceUsage,

    /// <summary>
    /// Keyboard input playback record (Jrn.Key), typically special keys
    /// such as arrows, Enter, function keys, Ctrl, Shift, or Tab.
    /// </summary>
    KeyboardEvent,

    /// <summary>
    /// Memory-usage comment containing VM/RAM availability, used, and peak metrics.
    /// </summary>
    MemoryMetrics,

    /// <summary>
    /// Playback-relevant command record that does not fit AddinEvent, Command, Data,
    /// Directive, KeyboardEvent, MouseEvent, or UiEvent classifications.
    /// </summary>
    MiscCommand,

    /// <summary>
    /// Mouse input playback record (for example, move, button up/down/double-click,
    /// wheel, and scroll) with numeric payload data.
    /// </summary>
    MouseEvent,

    /// <summary>
    /// Structured system-information entry (hardware/OS/printer/video/processor data)
    /// extracted from system-information sections.
    /// </summary>
    SystemInformation,

    /// <summary>
    /// Timestamp comment that starts a new logical block and can carry a marker
    /// (C/E/H), datetime, debug tag, and optional description.
    /// </summary>
    TimeStamp,

    /// <summary>
    /// UI interaction playback record (Jrn.* UI events), such as ribbon, dialogs,
    /// project browser, view-state changes, and drag-drop actions.
    /// </summary>
    UiEvent,

    /// <summary>
    /// Worksharing event comment (SLOG) mirroring events also written to central-model
    /// worksharing logs, including session ID, timestamp, and text.
    /// </summary>
    WorksharingEvent
}

/// <summary>
/// Identifies a mouse-related journal command.
/// </summary>
/// <remarks>
/// These are playback-relevant mouse-action tokens recorded in journal lines.
/// </remarks>
public enum JournalMouseEventType
{
    /// <summary>
    /// Unrecognized mouse-event token.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Left mouse button double-click.
    /// </summary>
    LButtonDblClk,

    /// <summary>
    /// Left mouse button pressed.
    /// </summary>
    LButtonDown,

    /// <summary>
    /// Left mouse button released.
    /// </summary>
    LButtonUp,

    /// <summary>
    /// Middle mouse button double-click.
    /// </summary>
    MButtonDblClk,

    /// <summary>
    /// Middle mouse button pressed.
    /// </summary>
    MButtonDown,

    /// <summary>
    /// Middle mouse button released.
    /// </summary>
    MButtonUp,

    /// <summary>
    /// Mouse movement event.
    /// </summary>
    MouseMove,

    /// <summary>
    /// Right mouse button double-click.
    /// </summary>
    RButtonDblClk,

    /// <summary>
    /// Right mouse button pressed.
    /// </summary>
    RButtonDown,

    /// <summary>
    /// Right mouse button released.
    /// </summary>
    RButtonUp,

    /// <summary>
    /// Scroll action event.
    /// </summary>
    Scroll,

    /// <summary>
    /// Mouse wheel action event.
    /// </summary>
    Wheel
}

/// <summary>
/// Identifies the system-information section containing a record.
/// </summary>
/// <remarks>
/// System-information sections are introduced by comments such as
/// "OPERATING SYSTEM INFORMATION:" and contain key/value hardware or environment data.
/// </remarks>
public enum JournalSystemInformationType
{
    /// <summary>
    /// Unrecognized system-information section.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Operating-system information section.
    /// </summary>
    OperatingSystem,

    /// <summary>
    /// Printer information section.
    /// </summary>
    Printer,

    /// <summary>
    /// Printer-configuration information section.
    /// </summary>
    PrinterConfiguration,

    /// <summary>
    /// Processor information section.
    /// </summary>
    Processor,

    /// <summary>
    /// Video-controller information section.
    /// </summary>
    VideoController
}

/// <summary>
/// Identifies the literal timestamp marker found in the journal.
/// </summary>
/// <remarks>
/// Keep the literal enum member names to preserve parser fidelity with source text.
/// </remarks>
public enum JournalTimeStampType
{
    /// <summary>
    /// Unrecognized timestamp marker.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Comment/code-block information marker. Often used for internal comments,
    /// diagnostic notes, or background tracking entries.
    /// </summary>
    C,

    /// <summary>
    /// Event/execution marker. Often appears on interactive user actions such as
    /// UI commands, button clicks, and external add-in operations not logged
    /// under the core H category.
    /// </summary>
    E,

    /// <summary>
    /// Header/handle/hook marker. Often appears with higher-level directives,
    /// core calls, API entry points, or major system-handling events.
    /// </summary>
    H
}

/// <summary>
/// Identifies a UI journal command.
/// </summary>
/// <remarks>
/// Member names retain the journal tokens.
/// Descriptions reflect observed behavior rather than an exhaustive specification.
/// </remarks>
public enum JournalUiEventType
{
    /// <summary>
    /// Unrecognized UI event token parsed from the journal text.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Appears to indicate making a view the active view.
    /// </summary>
    Activate,

    /// <summary>
    /// Opening or closing the Revit application menu.
    /// </summary>
    AppButtonEvent,

    /// <summary>
    /// Interaction with the project browser.
    /// </summary>
    Browser,

    /// <summary>
    /// Interaction with a check box in a dialog.
    /// </summary>
    CheckBox,

    /// <summary>
    /// Closing a view.
    /// </summary>
    Close,

    /// <summary>
    /// Interaction with a combo box in a dialog.
    /// </summary>
    ComboBox,

    /// <summary>
    /// Loading files via drag and drop.
    /// </summary>
    DropFiles,

    /// <summary>
    /// Interaction with a text field in a dialog.
    /// </summary>
    Edit,

    /// <summary>
    /// Interaction with a table-like control in a dialog,
    /// such as the compound structure editor.
    /// </summary>
    Grid,

    /// <summary>
    /// Interaction with a list box or drop-down list in a dialog.
    /// </summary>
    ListBox,

    /// <summary>
    /// Maximizing a view.
    /// </summary>
    Maximize,

    /// <summary>
    /// Minimizing a view.
    /// </summary>
    Minimize,

    /// <summary>
    /// Activating or leaving the properties palette.
    /// </summary>
    PropertiesPalette,

    /// <summary>
    /// Interaction with a push button in a dialog.
    /// </summary>
    PushButton,

    /// <summary>
    /// Interaction with a radio button in a dialog.
    /// </summary>
    RadioButton,

    /// <summary>
    /// Restoring a minimized view.
    /// </summary>
    Restore,

    /// <summary>
    /// Interaction with an element in the Revit ribbon.
    /// </summary>
    RibbonEvent,

    /// <summary>
    /// Status-bar tray interaction, observed when starting the selection filter.
    /// </summary>
    SBTrayAction,

    /// <summary>
    /// Reports the size of the active window.
    /// </summary>
    Size,

    /// <summary>
    /// Interaction with a slider control.
    /// </summary>
    SliderCtrl,

    /// <summary>
    /// Interaction with a tab in a dialog.
    /// </summary>
    TabCtrl,

    /// <summary>
    /// Interaction with a tree-view element in a dialog.
    /// </summary>
    TreeCtrl,

    /// <summary>
    /// Interaction with an element in the material editor.
    /// </summary>
    WidgetEvent
}

/// <summary>
/// Identifies how a model was opened or whether loading completed.
/// </summary>
/// <remarks>
/// Values mirror the loaded-model analysis workflow: open-attempt detection
/// (dialog/recent/drag-drop) followed by optional load-completion detection.
/// </remarks>
public enum LoadedModelEventType
{
    /// <summary>
    /// Unrecognized loaded-model event.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The model was opened through a file-open dialog.
    /// </summary>
    OpenDialog,

    /// <summary>
    /// The model was opened through the recent-files interface.
    /// </summary>
    OpenRecent,

    /// <summary>
    /// The model was opened by dragging and dropping a file.
    /// </summary>
    OpenDragDrop,

    /// <summary>
    /// A load-completion marker was found for the model.
    /// </summary>
    Loaded
}