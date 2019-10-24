import clr
import sys
pyt_path = r'C:\Program Files (x86)\IronPython 2.7\Lib'
sys.path.append(pyt_path)
import xml.etree.ElementTree as ET

hardcodedshortcuts = {
	"ID_APP_EXIT": ["Alt+Fn4"],
	"ID_BUTTON_REDO": ["Ctrl+Y","Ctrl+Shift+Z"],
	"ID_BUTTON_UNDO": ["Ctrl+Z","Alt+Backspace"],
	"ID_CHECK_SPELLING": ["Fn7"],
	"ID_EDIT_COPY": ["Ctrl+C","Ctrl+Insert"],
	"ID_EDIT_CUT": ["Ctrl+X","Ctrl+Delete"],
	"ID_EDIT_PASTE": ["Ctrl+V"],
	"ID_FILE_NEW_CHOOSE_TEMPLATE": ["Ctrl+N"],
	"ID_REVIT_FILE_CLOSE": ["Ctrl+W"],
	"ID_REVIT_FILE_OPEN": ["Ctrl+O"],
	"ID_REVIT_FILE_PRINT": ["Ctrl+P"],
	"ID_REVIT_FILE_SAVE": ["Ctrl+S"],
	"ID_SCHEDULE_VIEW_ZOOM_IN": ["Ctrl++"],
	"ID_SCHEDULE_VIEW_ZOOM_OUT": ["Ctrl+-"],
	"ID_SCHEDULE_VIEW_ZOOM_RESTORE": ["Ctrl+0"]
	}

class KeyboardShortcuts:
	def __init__(self, commands, commandcount, commandcountwithshortcuts):
		self.Commands = commands
		self.CommandCount = commandcount
		self.CommandCountWithShortcuts = commandcountwithshortcuts
	def __repr__(self):
		return 'KeyboardShortcuts'
	def GetCommandById(self, id):
		found = [x for x in self.Commands if x.ID == id]
		if len(found) > 0: return found[0]
		else: return None
	def GetCommandsWithShortcuts(self):
		return [x for x in self.Commands if x.HasShortcuts]

class KeyboardShortcutCommand:
	def __init__(self, name, id, shortcuts, paths):
		self.Name = name.decode("utf-8")
		self.ID = id
		self.Shortcuts = shortcuts
		self.Paths = paths
		self.HasShortcuts = len(shortcuts) > 0
		self.HasPaths = len(paths) > 0
	def __repr__(self):
		return 'KeyboardShortcutCommand'

def KSFromPath(path):
	try:
		Commands = []
		CommandCount = 0
		CommandCountWithShortcuts = 0
		root = e = ET.parse(path).getroot()
		for child in root:
			if child.tag == "ShortcutItem":	
				CommandCount += 1
				CommandId = child.get("CommandId")
				shortcuts = child.get("Shortcuts") 
				if shortcuts == None: 
					if CommandId in hardcodedshortcuts: 
						CommandShortcuts = hardcodedshortcuts[CommandId]
						CommandCountWithShortcuts += 1
					else: CommandShortcuts = []
				else: 
					CommandShortcuts = shortcuts.split("#")
					if CommandId in hardcodedshortcuts: CommandShortcuts = CommandShortcuts + hardcodedshortcuts[CommandId]
					CommandCountWithShortcuts += 1
				paths = child.get("Paths") 
				if paths == None: CommandPaths = []
				else: CommandPaths = paths.split("; ")
				Commands.append(KeyboardShortcutCommand(child.get("CommandName"), CommandId, CommandShortcuts, CommandPaths))
		return KeyboardShortcuts(Commands, CommandCount, CommandCountWithShortcuts)
	except:
		import traceback
		return traceback.format_exc()

if isinstance(IN[0], list): OUT = [KSFromPath(x) for x in IN[0]]
else: OUT = KSFromPath(IN[0])