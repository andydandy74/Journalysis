namespace Journalysis.Core.Models;

/// <summary>
/// Identifies an API message category recognized by the journal parser.
/// </summary>
/// <remarks>
/// API message lines are journal comments that record API successes and API errors.
/// Classification is independent of whether the message is marked as an error.
/// These messages can be used to identify journals with API errors, extract loaded
/// add-in assemblies from pushbutton and startup messages, and find which add-ins
/// subscribed to Revit events.
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
/// Only commands with a built-in Revit command ID are recorded here; add-in commands
/// typically appear as UI events instead.
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
    /// Command started from Revit's project browser.
    /// </summary>
    ProjectBrowser,

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
/// Each category corresponds to a JournalLine subtype. Command records only built-in
/// command IDs, while add-in interactions appear as UiEvent. Comment is the catch-all for
/// comment lines that do not match a more specific category. TimeStamp records start the
/// logical blocks used to group records, and WorksharingEvent lines mirror SLOG entries
/// also written to the central model's worksharing log.
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
    /// Use it to identify when and how often an add-in performed a given action.
    /// </summary>
    AddinEvent,

    /// <summary>
    /// API success/error message comment (API_SUCCESS / API_ERROR),
    /// including add-in registration, startup, and exception-related messages.
    /// Message categories are documented by <see cref="JournalApiMessageType"/>.
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
    /// Only built-in command IDs are recorded here; add-in commands appear as UiEvent.
    /// </summary>
    Command,

    /// <summary>
    /// General comment record that is not classified as API message, basic file info,
    /// GUI resource usage, memory metrics, timestamp, or worksharing event. It is the
    /// catch-all category for comment lines and still contains data worth mining.
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
    /// Known key tokens are documented by <see cref="JournalKeyboardKey"/>.
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
    /// Action types are documented by <see cref="JournalMouseEventType"/>.
    /// </summary>
    MouseEvent,

    /// <summary>
    /// Structured system-information entry (hardware/OS/printer/video/processor data)
    /// extracted from system-information sections.
    /// Sections are documented by <see cref="JournalSystemInformationType"/>.
    /// </summary>
    SystemInformation,

    /// <summary>
    /// Timestamp comment that starts a new logical block and can carry a marker
    /// (C/E/H), datetime, debug tag, and optional description.
    /// Every timestamp begins a new block, which is the unit used to group records.
    /// </summary>
    TimeStamp,

    /// <summary>
    /// UI interaction playback record (Jrn.* UI events), such as ribbon, dialogs,
    /// project browser, view-state changes, and drag-drop actions.
    /// This is the main record of add-in invocations and dialog interactions.
    /// Event types are documented by <see cref="JournalUiEventType"/>.
    /// </summary>
    UiEvent,

    /// <summary>
    /// Worksharing event comment (SLOG) mirroring events also written to central-model
    /// worksharing logs, including session ID, timestamp, and text.
    /// Event terms are documented by <see cref="WorksharingLogEventType"/>; the session ID
    /// can be used to cross-reference the journal with its worksharing log.
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
    /// Scroll action event - possibly deprecated.
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
/// Printer, PrinterConfiguration, and VideoController sections may describe multiple
/// devices; those records are numbered consecutively within the section.
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
/// UI events are the main record of dialog interactions and add-in invocations,
/// for example an external command started from the ribbon.
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
    /// Maximizing a view. Possibly deprecated.
    /// </summary>
    Maximize,

    /// <summary>
    /// Minimizing a view.  Possibly deprecated.
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
    /// Restoring a minimized view. Possibly deprecated.
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
/// Classifies SLOG lines in the journal as event lines or session metadata.
/// </summary>
/// <remarks>
/// SLOG comments mirror the worksharing events written to the central model's
/// worksharing log. Event lines carry a session ID, timestamp, and text such as
/// <c>&gt;Session</c>; metadata lines carry key/value pairs such as <c>build=</c>.
/// The session ID can be used to cross-reference a journal with its worksharing log.
/// </remarks>
public enum JournalWorksharingEntryType
{
    /// <summary>
    /// Not recognized.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// SLOG event with session ID, timestamp, and event text.
    /// </summary>
    Event,

    /// <summary>
    /// SLOG metadata line (e.g., build=..., user=..., host=...).
    /// </summary>
    Metadata
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

/// <summary>
/// Identifies the kind of worksharing session derived from its recorded events.
/// </summary>
/// <remarks>
/// Classification is heuristic and based on the presence of event markers such as
/// <c>&gt;Open</c>, <c>&gt;Open:Central</c>, <c>&gt;STC</c>, <c>&gt;WSD</c>, and
/// <c>.ReconnectInMiddle</c>.
/// </remarks>
public enum WorksharingSessionType
{
    /// <summary>
    /// The session could not be classified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The session opened a central model and the second event is an open marker,
    /// indicating a detached copy was created.
    /// </summary>
    CreateDetached,

    /// <summary>
    /// The session opened a model without opening the central model directly,
    /// indicating a local copy was created.
    /// </summary>
    CreateLocalCopy,

    /// <summary>
    /// The session synchronized with central without opening a model,
    /// indicating a new central model was created.
    /// </summary>
    CreateNewCentral,

    /// <summary>
    /// The session changed worksets without opening a model.
    /// </summary>
    ChooseWorksets,

    /// <summary>
    /// The session reconnected in the middle of an existing session.
    /// </summary>
    Reconnected,

    /// <summary>
    /// The session worked directly in the central model.
    /// </summary>
    WorkInCentral
}

/// <summary>
/// Identifies keyboard tokens observed in <c>Jrn.Key</c> playback records.
/// </summary>
/// <remarks>
/// Values mirror the raw tokens written by Revit so member names can be matched
/// directly against <see cref="JournalKeyboardEventLine.Key"/>. This is an observed,
/// non-exhaustive list; unlisted tokens are still preserved as strings. The enum
/// documents the vocabulary and is not used as the property type.
/// </remarks>
public enum JournalKeyboardKey
{
    /// <summary>Unrecognized keyboard token.</summary>
    Unknown = 0,

    /// <summary>Right arrow key.</summary>
    VK_RIGHT,

    /// <summary>Down arrow key.</summary>
    VK_DOWN,

    /// <summary>Left arrow key.</summary>
    VK_LEFT,

    /// <summary>Up arrow key.</summary>
    VK_UP,

    /// <summary>Carriage return (Enter).</summary>
    CR,

    /// <summary>Function key 1.</summary>
    VK_F1,

    /// <summary>Function key 2.</summary>
    VK_F2,

    /// <summary>Function key 3.</summary>
    VK_F3,

    /// <summary>Function key 4.</summary>
    VK_F4,

    /// <summary>Function key 5.</summary>
    VK_F5,

    /// <summary>Function key 6.</summary>
    VK_F6,

    /// <summary>Function key 7.</summary>
    VK_F7,

    /// <summary>Function key 8.</summary>
    VK_F8,

    /// <summary>Function key 9.</summary>
    VK_F9,

    /// <summary>Function key 10.</summary>
    VK_F10,

    /// <summary>Function key 11.</summary>
    VK_F11,

    /// <summary>Function key 12.</summary>
    VK_F12,

    /// <summary>Control modifier key.</summary>
    VK_CONTROL,

    /// <summary>Shift modifier key.</summary>
    VK_SHIFT,

    /// <summary>Tab key.</summary>
    Tab
}

/// <summary>
/// Identifies terms observed in worksharing log (<c>.slog</c>) event lines.
/// </summary>
/// <remarks>
/// Member names mirror the raw tokens where possible; <c>OpenCentral</c> represents
/// the token <c>Open:Central</c>. A leading <c>&gt;</c> marks the start of an action,
/// <c>&lt;</c> its conclusion, and <c>$</c> prefixes session IDs. Entries noted as not
/// processed by Journalysis are documented for recognition but are not used by analysis.
/// This enum documents the vocabulary; event text remains a string in the model.
/// </remarks>
public enum WorksharingLogEventType
{
    /// <summary>Unrecognized event token.</summary>
    Unknown = 0,

    /// <summary>Start of an action, for example <c>&gt;Open</c>.</summary>
    StartOfAction,

    /// <summary>Conclusion of an action, for example <c>&lt;Open</c>.</summary>
    ConclusionOfAction,

    /// <summary>Prefix for session IDs, for example <c>$78gre237</c>.</summary>
    SessionIdPrefix,

    /// <summary>Attempt acquire or relinquish partitions or elements. Not processed by Journalysis.</summary>
    AAoRPoE,

    /// <summary>Transparent borrowing for autojoin. Not processed by Journalysis.</summary>
    AJ,

    /// <summary>Meaning not documented in the source dictionary. Not processed by Journalysis.</summary>
    Auto,

    /// <summary>Revit crashing. Not processed by Journalysis.</summary>
    Crash,

    /// <summary>Show button of extended error dialog. Not processed by Journalysis.</summary>
    EED,

    /// <summary>End of transactions checks. Not processed by Journalysis.</summary>
    EOTC,

    /// <summary>Granting or denying editing requests. Not processed by Journalysis.</summary>
    ERN,

    /// <summary>Making an element editable. Not processed by Journalysis.</summary>
    ME,

    /// <summary>Opening the model.</summary>
    Open,

    /// <summary>Opening the central model. Raw token: <c>Open:Central</c>.</summary>
    OpenCentral,

    /// <summary>Opening a linked model.</summary>
    OpenLink,

    /// <summary>Starting a new session after connectivity was lost.</summary>
    ReconnectInMiddle,

    /// <summary>Using the Relinquish All command. Not processed by Journalysis.</summary>
    RelinquishAllMine,

    /// <summary>Reloading latest; may appear on its own or as a subaction of <c>STC</c>. Not processed by Journalysis.</summary>
    RL,

    /// <summary>Relinquish borrowed elements for autojoin. Not processed by Journalysis.</summary>
    ROB,

    /// <summary>Worksharing session.</summary>
    Session,

    /// <summary>Syncing with the central model.</summary>
    STC,

    /// <summary>Saving to the local copy.</summary>
    STL,

    /// <summary>Revit suggesting to sync with central. Not processed by Journalysis.</summary>
    SuggestSTC,

    /// <summary>Applying the worksharing configuration after opening the model.</summary>
    WSConfig,

    /// <summary>
    /// Worksharing dialog specifying which worksets to open or close. When the central
    /// model is set to ask on open, this appears in a separate session.
    /// </summary>
    WSD
}

/// <summary>
/// Identifies metadata keys recorded in worksharing log (<c>.slog</c>) session lines.
/// </summary>
/// <remarks>
/// These keys appear as <c>key=value</c> lines following a <c>&gt;Session</c> event.
/// Member names use PascalCase while the recorded keys are lower case. This enum
/// documents the vocabulary; metadata remains string-keyed in the model.
/// </remarks>
public enum WorksharingLogMetadataKey
{
    /// <summary>Unrecognized metadata key.</summary>
    Unknown = 0,

    /// <summary>Revit build of the worksharing session.</summary>
    Build,

    /// <summary>Path to the central model.</summary>
    Central,

    /// <summary>Computer name of the Revit session.</summary>
    Host,

    /// <summary>Journal path of the Revit session.</summary>
    Journal,

    /// <summary>Path to the local copy of the model. Not processed by Journalysis.</summary>
    Local,

    /// <summary>Server recorded for the session; exact meaning not documented in the source dictionary.</summary>
    Server,

    /// <summary>Username of the Revit session.</summary>
    User
}