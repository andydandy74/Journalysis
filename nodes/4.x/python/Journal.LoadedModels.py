import clr
import sys
import os

class LoadedModel:
	def __init__(self, journal, path, initialEvent):
		self.Journal = journal
		if path != None: self.Path = self.NormalizePath(path)
		else: self.Path = None
		self.Events = []
		self.Events.append(initialEvent)
	def __repr__(self):
		return "LoadedModel"
	def GetVersion(self):
		return [x for x in self.Journal.GetLinesByBlocks(range(self.Events[0].JournalLine.Block, self.Events[1].JournalLine.Block)) if x.Type == "JournalComment" and ":< File was saved in" in x.RawText][0].RawText.split("File was saved in")[-1].strip()
	def GetVersionHistory(self):
		allComments = [x for x in self.Journal.GetLinesByBlocks(range(self.Events[0].JournalLine.Block, self.Events[1].JournalLine.Block))] 
		saveHistoryMarkers = [x.Number for x in allComments if x.Type == "JournalComment" and ":< Document save history" in x.RawText]
		return [x.RawText.split(":<")[-1].strip() for x in allComments if x.Number in range(saveHistoryMarkers[0] + 1, saveHistoryMarkers[1])]
	def NormalizePath(self, path):
		return os.path.normpath(os.path.join(os.path.dirname(self.Journal.Path),path))

class LoadedModelEvent:
	def __init__(self, journalLine, type, dateTime):
		self.JournalLine = journalLine
		self.Type = type
		self.DateTime = dateTime
	def __repr__(self):
		return "LoadedModelEvent"

journal = IN[0]

# All variations of opening a document via dialog
events = [LoadedModelEvent(x, 'Open_Dialog', x.GetDateTimeOfBlock()) for x in journal.GetLinesByType('JournalCommand') if x.CommandID in ('ID_REVIT_FILE_OPEN', 'ID_APPMENU_PROJECT_OPEN')]
# All variations of opening a document via recent files
events.extend([LoadedModelEvent(x, 'Open_Recent', x.GetDateTimeOfBlock()) for x in journal.GetLinesByType('JournalCommand') if x.CommandID == 'ID_FILE_MRU_FIRST' or x.CommandID.startswith('ID_FILE_MRU_FILE')])
# Models opened via drag & drop
events.extend([LoadedModelEvent(x, 'Open_DragDrop', x.GetDateTimeOfBlock()) for x in journal.GetLinesByType('JournalUIEvent') if x.UIEventType == "DropFiles"])

# Get next JournalData line to extract file path
nextDataLines = [x.JournalLine.NextOfType('JournalData') for x in events]
paths = []
ignoreEvents = []
i = 0
for line in nextDataLines:
	# For workshared models we'll need the second JournalData line
	if line.Key not in ("File Name", "Dropped File Name", "MRUFileName"):
		line = line.NextOfType('JournalData')
	nextDataLine = line.NextOfType('JournalData')
	# Flag models that do not have a JournalData line that carries the filename
	if line.Key not in ("File Name", "Dropped File Name", "MRUFileName"):
		ignoreEvents.append(i)
	# Flag models that were canceled in file open dialog
	elif line.Key == "File Name" and line.Values[0] == "IDCANCEL":
		ignoreEvents.append(i)
	# Flag models that were canceled in status bar, otherwise add path to list
	elif nextDataLine != None:
		if nextDataLine.Key == "ProgressCancelled":
			nextDataLine2 = nextDataLine.NextOfType('JournalData')
			if nextDataLine2.Key == "TaskDialogResult" and nextDataLine2.Values[-1] == "IDYES":
				ignoreEvents.append(i)
			else: paths.append(line.Values[-1])
		else: paths.append(line.Values[-1])
	else: paths.append(line.Values[-1])
	i += 1

# Remove flagged events
for index in sorted(ignoreEvents, reverse=True): del events[index]

# Build LoadedModel objects
loadedModels = [LoadedModel(journal, paths[x], events[x]) for x in range(0, len(events))]

# Get lines of load completed
loadedLines = [x.JournalLine.NextOfTypeAndProperty('JournalComment', 'RawText', "'***********************************************************").NextOfType('JournalTimeStamp') for x in events]
# Create event objects for load completed
loadedEvents = [LoadedModelEvent(x, 'Loaded', x.DateTime) for x in loadedLines]
# Add events to loaded models
[loadedModels[x].Events.append(loadedEvents[x]) for x in range(0, len(loadedModels))]

OUT = loadedModels