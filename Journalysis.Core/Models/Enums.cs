namespace Journalysis.Core.Models;

/// <summary>
/// Identifies an API message category recognized by the journal parser.
/// </summary>
/// <remarks>
/// This classification is independent of whether the message represents an error.
/// </remarks>
public enum JournalApiMessageType
{
    Unknown = 0,
    AddedPushbutton,
    AssemblyVersionConflict,
    Exception,
    RegisteredExternalServer,
    RegisteredExternalService,
    RegisteringCommandEvent,
    RegisteringEvent,
    ReplacingCommandId,
    RestoringCommandId,
    StartingExternalApplication,
    StartingExternalDbApplication,
    UnregisteringCommandEvent,
    UnregisteringEvent
}

/// <summary>
/// Identifies the context from which a journal command was started.
/// </summary>
/// <remarks>
/// These values are based on command type strings observed in Revit journals.
/// Use a nullable property to represent commands whose type has not yet been parsed.
/// Unknown represents an unrecognized parsed value.
/// </remarks>
public enum JournalCommandType
{
    Unknown = 0,
    AccelKey,
    Internal,
    KeyboardShortcut,
    PrintPreviewUi,
    Ribbon,
    StartupPage,
    StatusBar,
    SystemMenu
}

/// <summary>
/// Identifies a debug-message category attached to a timestamp.
/// </summary>
/// <remarks>
/// Use a nullable property to represent timestamps without a debug marker.
/// Unknown represents an unrecognized category, not the absence of a marker.
/// </remarks>
public enum JournalDebugInfoType
{
    Unknown = 0,
    Error,
    Info,
    Warning
}

/// <summary>
/// Identifies the category of a parsed journal record.
/// </summary>
public enum JournalLineKind
{
    Unknown = 0,
    AddinEvent,
    ApiMessage,
    BasicFileInfo,
    Command,
    Comment,
    Data,
    Directive,
    GuiResourceUsage,
    KeyboardEvent,
    MemoryMetrics,
    MiscCommand,
    MouseEvent,
    SystemInformation,
    TimeStamp,
    UiEvent,
    WorksharingEvent
}

/// <summary>
/// Identifies a mouse-related journal command.
/// </summary>
/// <remarks>
/// Member names retain the journal tokens to make parsing and troubleshooting
/// straightforward.
/// </remarks>
public enum JournalMouseEventType
{
    Unknown = 0,
    LButtonDblClk,
    LButtonDown,
    LButtonUp,
    MButtonDblClk,
    MButtonDown,
    MButtonUp,
    MouseMove,
    RButtonDblClk,
    RButtonDown,
    RButtonUp,
    Scroll,
    Wheel
}

/// <summary>
/// Identifies the system-information section containing a record.
/// </summary>
public enum JournalSystemInformationType
{
    Unknown = 0,
    OperatingSystem,
    Printer,
    PrinterConfiguration,
    Processor,
    VideoController
}

/// <summary>
/// Identifies the literal timestamp marker found in the journal.
/// </summary>
/// <remarks>
/// The source parser recognizes C, H, and E but does not define their meanings.
/// Keep the literal names rather than assigning unverified interpretations.
/// </remarks>
public enum JournalTimeStampType
{
    Unknown = 0,
    C,
    E,
    H
}



/// <summary>
/// Identifies a user-interface journal command.
/// </summary>
/// <remarks>
/// Member names retain the journal tokens.
/// </remarks>
public enum JournalUiEventType
{
    Unknown = 0,
    Activate,
    AppButtonEvent,
    Browser,
    CheckBox,
    Close,
    ComboBox,
    DropFiles,
    Edit,
    Grid,
    ListBox,
    Maximize,
    Minimize,
    PropertiesPalette,
    PushButton,
    RadioButton,

    // Referenced during property extraction, but not recognized by the 
    // first-pass UI classification. Declaring it here does not change classification.
    Restore,

    RibbonEvent,
    SBTrayAction,
    Size,
    SliderCtrl,
    TabCtrl,
    TreeCtrl,
    WidgetEvent
}

/// <summary>
/// Identifies how a model was opened or whether loading completed.
/// </summary>
public enum LoadedModelEventType
{
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