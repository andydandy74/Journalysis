import clr
import sys
pyt_path = r'C:\Program Files (x86)\IronPython 2.7\Lib'
sys.path.append(pyt_path)
import xml.etree.ElementTree as ET

class KeyboardShortcuts:
	def __init__(self, commands, commandcount, commandcountwithshortcuts):
		self.Commands = commands
		self.CommandCount = commandcount
		self.CommandCountWithShortcuts = commandcountwithshortcuts
	def __repr__(self):
		return 'KeyboardShortcuts'
	def GetCommandsWithShortcuts(self):
		return [x for x in self.Commands if x.HasShortcuts]

class KeyboardShortcutCommand:
	def __init__(self, name, id, shortcuts, paths):
		self.Name = name
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
		#root = ET.fromstring(ksfile)
		root = e = ET.parse(path).getroot()
		for child in root:
			if child.tag == "ShortcutItem":	
				CommandCount += 1
				shortcuts = child.get("Shortcuts") 
				if shortcuts == None: CommandShortcuts = []
				else: 
					CommandShortcuts = shortcuts.split("#")
					CommandCountWithShortcuts += 1
				paths = child.get("Paths") 
				if paths == None: CommandPaths = []
				else: CommandPaths = paths.split("; ")
				Commands.append(KeyboardShortcutCommand(child.get("CommandName"), child.get("CommandId"), CommandShortcuts, CommandPaths))
		return KeyboardShortcuts(Commands, CommandCount, CommandCountWithShortcuts)
	except:
		import traceback
		return traceback.format_exc()

if isinstance(IN[0], list): OUT = [KSFromPath(x) for x in IN[0]]
else: OUT = KSFromPath(IN[0])