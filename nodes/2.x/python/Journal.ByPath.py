import clr
import sys
sys.path.append("C:\Program Files (x86)\IronPython 2.7\Lib")
import os
import time

class Journal:
	def __init__(self, lines, version, username, blockcount, path, build, branch, machinename, osversion):
		self.Lines = lines
		self.LineCount = len(lines)
		self.Version = version
		self.User = username	
		self.BlockCount = blockcount
		self.Path = path
		self.Build = build
		self.Branch = branch
		self.Machine = machinename
		self.OSVersion = osversion
		self.ProcessingTime = None
	def __repr__(self):
		return 'Journal'
	def ContainsAPIErrors(self):
		if len([x for x in self.GetLinesByTypeAndProperty('JournalAPIMessage', 'IsError', True)]) > 0: return True
		else: return False
	def ContainsExceptions(self):
		if len(self.GetExceptions()) > 0: return True
		else: return False
	def GetDate(self):
		return self.GetDateTimeByBlock(1).Date
	def GetDateTimeByBlock(self, block):
		if block == 0: return None
		else: return [x for x in self.GetLinesByType('JournalTimeStamp') if x.Block == block][0].DateTime
	def GetDateTimeByBlocks(self, blocks):
		if 0 in blocks: return None
		else: return [x.DateTime for x in self.GetLinesByType('JournalTimeStamp') if x.Block in blocks]
	def GetExceptions(self):
		exlist = []
		exlist.extend([x for x in self.GetLinesByType('JournalTimeStamp') if "Exception" in x.Description])
		exlist.extend([x for x in self.GetLinesByType('JournalAPIMessage') if x.IsError and "Exception" in x.MessageText])
		exlist.extend([x for x in self.GetLinesByType('JournalComment') if "Exception" in x.RawText and "ExceptionPolicy" not in x.RawText and "Exception occurred" not in x.RawText])
		exlist.sort(key=lambda x: x.Number)
		return exlist
	def GetFirstLines(self, number):
		return self.Lines[:number]
	def GetLastLines(self, number):
		return self.Lines[-number:]
	def GetLinesByBlock(self, block):
		return [x for x in self.Lines if x.Block == block]
	def GetLinesByBlocks(self, blocks):
		return [x for x in self.Lines if x.Block in blocks]
	def GetLinesByDateTime(self, fromDateTime, toDateTime):
		tslist = []
		if fromDateTime and toDateTime: tslist = [x for x in self.GetLinesByType("JournalTimeStamp") if x.DateTime > fromDateTime and x.DateTime < toDateTime]
		elif fromDateTime: tslist = [x for x in self.GetLinesByType("JournalTimeStamp") if x.DateTime > fromDateTime]
		elif toDateTime: tslist = [x for x in self.GetLinesByType("JournalTimeStamp") if x.DateTime < toDateTime]
		return self.GetLinesByBlocks([x.Block for x in tslist])
	def GetLinesByType(self, type):
		return [x for x in self.Lines if x.Type == type]
	def GetLinesByTypeAndProperty(self, type, prop, val):
		return [x for x in self.Lines if x.Type == type and getattr(x, prop) == val]
	def GetLinesByTypes(self, types):
		return [x for x in self.Lines if x.Type in types]
	def GetLoadedAssemblies(self):
		apimsgs = self.GetLinesByType("JournalAPIMessage")
		loadedAssemblies = []
		replacedCommands = {}
		for apimsg in apimsgs:
			if apimsg.MessageText.startswith("Starting"):
				startdata1 = apimsg.MessageText.split("Application: ")[-1].split(", Class: ")
				startdata2 = startdata1[-1].split(", Vendor : ")
				startdata3 = startdata2[-1].split(", Assembly: ") 
				appname = startdata1[0]
				classname = startdata2[0]
				path = startdata3[-1]
				vendor = startdata3[0]
				loadedAssemblies.append(LoadedAssembly(appname, classname, path, vendor, apimsg))
		for apimsg in apimsgs:
			if apimsg.MessageText.startswith("Registering") or apimsg.MessageText.startswith("Unregistering") or apimsg.MessageText.startswith("API registering") or apimsg.MessageText.startswith("API unregistering"):
				regevt = apimsg.MessageText.split("by application ")[-1].rsplit(" (",1)
				appname = regevt[0]
				appguid = regevt[1][:-2]
				foundByGUID = [x for x in loadedAssemblies if x.GUID == appguid]
				if len(foundByGUID) > 0: foundByGUID[0].Events.append(apimsg)
				else:
					foundByName = [x for x in loadedAssemblies if x.Name == appname and x.GUID == None]
					if len(foundByName) > 0:
						foundByName[0].GUID = appguid
						foundByName[0].Events.append(apimsg)
			elif apimsg.MessageText.startswith("Replacing"):
				replname = apimsg.MessageText.split("from application '",1)[-1].split("'",1)[0]
				replcommand = apimsg.MessageText.split("Replacing command id '",1)[-1].split("'",1)[0]
				foundByName = [x for x in loadedAssemblies if x.Name == replname]
				if len(foundByName) > 0: 
					foundByName[0].Events.append(apimsg)
					replacedCommands[replcommand] = foundByName[0]
			elif apimsg.MessageText.startswith("Restoring"):
				restcommand = apimsg.MessageText.split("Restoring command id '",1)[-1].split("'",1)[0]
				if restcommand in replacedCommands: replacedCommands[restcommand].Events.append(apimsg)
			elif apimsg.MessageText.startswith("Added pushbutton"):
				pbpath = apimsg.MessageText.split(", ")[-1].replace("assembly: ","").replace("assembly ","")
				foundByPath = [x for x in loadedAssemblies if x.Path == pbpath]
				if len(foundByPath) > 0: foundByPath[0].Events.append(apimsg)
				else:
					foundByFilename = [x for x in loadedAssemblies if x.Filename == pbpath.rsplit("\\",1)[-1]]
					if len(foundByFilename) > 0: foundByFilename[0].Events.append(apimsg)
			elif apimsg.MessageText.startswith("System."):
				excguid = apimsg.MessageText.split("). Changes made by this handler are going to be discarded.",1)[0].rsplit(" (",1)[-1]
				foundByGUID = [x for x in loadedAssemblies if x.GUID == excguid]
				if len(foundByGUID) > 0: foundByGUID[0].Events.append(apimsg)
		return loadedAssemblies
	def GetMaxRAMPeak(self):
		return max([x.RAMPeak for x in self.GetLinesByType('JournalMemoryMetrics')])
	def GetMaxVMPeak(self):
		return max([x.VMPeak for x in self.GetLinesByType('JournalMemoryMetrics')])
	def GetMinRAMAvailable(self):
		return min([x.RAMAvailable for x in self.GetLinesByType('JournalMemoryMetrics')])
	def GetMinVMAvailable(self):
		return min([x.VMAvailable for x in self.GetLinesByType('JournalMemoryMetrics')])
	def GetSessionTime(self):
		ts = self.GetLinesByType('JournalTimeStamp')
		return ts[-1].DateTime - ts[0].DateTime
	def GetStartupTime(self):
		first_ts = self.GetDateTimeByBlock(1)
		# Revit 2019 and later
		startup1 = [x.Block for x in self.GetLinesByTypeAndProperty('JournalCommand', 'CommandID', 'ID_REVIT_MODEL_BROWSER_OPEN')]
		if len(startup1) > 0: return self.GetDateTimeByBlock(startup1[0]) - first_ts
		else:
			# Revit 2018 and earlier
			startup2 = [x.Block for x in self.GetLinesByTypeAndProperty('JournalCommand', 'CommandID', 'ID_STARTUP_PAGE')]
			if len(startup2) > 0: return self.GetDateTimeByBlock(startup2[0]) - first_ts
			else:
				# DynamoAutomation journal playback
				startup3 = [x.Block for x in self.GetLinesByTypeAndProperty('JournalCommand', 'CommandID', 'ID_FILE_MRU_FIRST')]
				if len(startup3) > 0: return self.GetDateTimeByBlock(startup3[0]) - first_ts
				else: 
					# Session started by double-clicking project or family file
					startup4 = [x.Block for x in self.GetLinesByTypeAndProperty('JournalCommand', 'CommandID', 'ID_REVIT_FILE_OPEN')]
					if len(startup4) > 0: return self.GetDateTimeByBlock(startup4[0]) - first_ts
					else: return None
	def IsInPlaybackMode(self):
		if len([x for x in self.GetLinesByType('JournalTimeStamp') if x.Description.startswith("started journal file playback")]) > 0: return True
		else: return False
	def StripComments(self, preserveTimeStamps):
		desiredTypes = ['JournalAddinEvent', 'JournalCommand', 'JournalData', 'JournalDirective', 'JournalKeyboardEvent', 'JournalMiscCommand', 'JournalMouseEvent', 'JournalUIEvent']
		if preserveTimeStamps: desiredTypes.append("JournalTimeStamp")
		return "\n".join([x.RawText for x in self.Lines if x.Type in desiredTypes])
	def WasPlaybackInterrupted(self):
		if len([x for x in self.GetLinesByType('JournalTimeStamp') if x.Description.startswith("stopped at line") and x.Description.endswith("journal file playback")]) > 0: return True
		else: return False
	def WasSessionTerminatedProperly(self):
		if len([x for x in self.GetLinesByTypeAndProperty('JournalTimeStamp', 'Description', 'finished recording journal file')]) > 0: return True
		else: return False

class JournalLine:
	def __init__(self, number, raw, block):
		self.Number = number
		self.RawText = raw
		self.Block = block
		self.Journal = None
		self.Type = 'JournalLine'
	def __repr__(self):
		return self.Type
	def AllNext(self):
		if self.Journal.Lines.index(self) + 1 < self.Journal.LineCount: return self.Journal.Lines[self.Journal.Lines.index(self) + 1:]
		else: return []
	def AllNextOfType(self, type):
		return [x for x in self.AllNext() if x.Type == type]
	def AllNextOfTypeAndProperty(self, type, prop, val):
		return [x for x in self.AllNext() if x.Type == type and getattr(x, prop) == val]
	def AllPrevious(self):
		if self.Journal.Lines.index(self) - 1 > -1: return self.Journal.Lines[:self.Journal.Lines.index(self)]
		else: return []
	def AllPreviousOfType(self, type):
		return [x for x in self.AllPrevious() if x.Type == type]
	def AllPreviousOfTypeAndProperty(self, type, prop, val):
		return [x for x in self.AllPrevious() if x.Type == type and getattr(x, prop) == val]
	def GetDateTimeOfBlock(self):
		return self.Journal.GetDateTimeByBlock(self.Block)
	def Next(self):
		if self.Journal.Lines.index(self) + 1 < self.Journal.LineCount: return self.Journal.Lines[self.Journal.Lines.index(self) + 1]
		else: return None
	def NextOfType(self, type):
		allnext = self.AllNextOfType(type)
		if len(allnext) > 0: return allnext[0]
		else: return None
	def NextOfTypeAndProperty(self, type, prop, val):
		allnext = self.AllNextOfTypeAndProperty(type, prop, val)
		if len(allnext) > 0: return allnext[0]
		else: return None
	def Previous(self):
		if self.Journal.Lines.index(self) - 1 > -1: return self.Journal.Lines[self.Journal.Lines.index(self) - 1]
		else: return None
	def PreviousOfType(self, type):
		allprev = self.AllPreviousOfType(type)
		if len(allprev) > 0: return allprev[-1]
		else: return None
	def PreviousOfTypeAndProperty(self, type, prop, val):
		allprev = self.AllPreviousOfTypeAndProperty(type, prop, val)
		if len(allprev) > 0: return allprev[-1]
		else: return None

class JournalAddinEvent(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.MessageText = None
		self.Type = 'JournalAddinEvent'
	def __repr__(self):
		return self.Type

class JournalAPIMessage(JournalLine):
	def __init__(self, number, raw, block, is_error):
		JournalLine.__init__(self, number, raw, block)
		self.IsError = is_error
		self.MessageText = None
		self.MessageType = None
		self.Type = 'JournalAPIMessage'
	def __repr__(self):
		return self.Type

class JournalBasicFileInfo(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Worksharing = None
		self.CentralModelPath = None
		self.LastSavePath = None
		self.Locale = None
		self.FileName = None
		self.Type = 'JournalBasicFileInfo'
	def __repr__(self):
		return self.Type

class JournalCommand(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.CommandType = None
		self.CommandDescription = None
		self.CommandID = None
		self.Type = 'JournalCommand'
	def __repr__(self):
		return self.Type

class JournalComment(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Type = 'JournalComment'
	def __repr__(self):
		return self.Type

class JournalData(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Key = None
		self.Values = []
		self.Type = 'JournalData'
	def __repr__(self):
		return self.Type

class JournalDirective(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Key = None
		self.Values = []
		self.Type = 'JournalDirective'
	def __repr__(self):
		return self.Type

class JournalGUIResourceUsage(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Available = None
		self.Used = None
		self.User = None
		self.Type = 'JournalGUIResourceUsage'
	def __repr__(self):
		return self.Type

class JournalKeyboardEvent(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Key = None
		self.Type = 'JournalKeyboardEvent'
	def __repr__(self):
		return self.Type
		
class JournalMemoryMetrics(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.VMAvailable = None
		self.VMUsed = None
		self.VMPeak = None
		self.RAMAvailable = None
		self.RAMUsed = None
		self.RAMPeak = None
		self.Type = 'JournalMemoryMetrics'
	def __repr__(self):
		return self.Type

class JournalMiscCommand(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.Type = 'JournalMiscCommand'
	def __repr__(self):
		return self.Type

class JournalMouseEvent(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.MouseEventType = None
		self.Data = []
		self.Type = 'JournalMouseEvent'
	def __repr__(self):
		return self.Type
		
class JournalSystemInformation(JournalLine):
	def __init__(self, number, raw, block, sysinfotype):
		JournalLine.__init__(self, number, raw, block)
		self.SystemInformationType = sysinfotype
		self.ItemNumber = None
		self.Key = None
		self.Value = None
		self.Type = 'JournalSystemInformation'
	def __repr__(self):
		return self.Type
		
class JournalTimeStamp(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.TimeStampType = None
		self.DateTime = None
		self.Description = None
		self.DebugInfoType = None
		self.Type = 'JournalTimeStamp'
	def __repr__(self):
		return self.Type

class JournalUIEvent(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.UIEventType = None
		self.Data = []
		self.Type = 'JournalUIEvent'
	def __repr__(self):
		return self.Type

class JournalWorksharingEvent(JournalLine):
	def __init__(self, number, raw, block):
		JournalLine.__init__(self, number, raw, block)
		self.SessionID = None
		self.DateTime = None
		self.Text = None
		self.Type = 'JournalWorksharingEvent'
	def __repr__(self):
		return self.Type

class LoadedAssembly:
	def __init__(self, name, classname, path, vendor, initialEvent):
		self.Name = name
		self.Class = classname
		self.Path = path
		self.Filename = path.split("\\")[-1]
		self.Vendor = vendor
		self.GUID = None
		self.Events = [initialEvent]
	def __repr__(self):
		return "LoadedAssembly"

def JournalFromPath(path):
	line = None
	try:
		processing_started = time.time()
		lineObjs = []
		jVersion = None
		jUsername = None
		jMachineName = None
		jOSVersion = None
		jPath = None
		jBuild = None
		jBranch = None
		sysinfoStarted = False
		commandCount = 0
		i = 1
		b = 0
		# Round 1: Create line objects
		with open(path, 'r') as lines:
			for line in lines:
				line = line.lstrip().rstrip('\n').rstrip('\x00')
				# ignore empty lines
				if len(line) < 2: pass
				elif line.startswith("'C ") or line.startswith("'H ") or line.startswith("'E "):
					b += 1
					lineObjs.append(JournalTimeStamp(i,line,b))
				elif ":< API_SUCCESS { " in line: lineObjs.append(JournalAPIMessage(i,line,b,False))
				elif ":< API_ERROR { " in line: lineObjs.append(JournalAPIMessage(i,line,b,True))
				elif ":: Delta VM: " in line or line.startswith("' 0:< Initial VM: "): lineObjs.append(JournalMemoryMetrics(i,line,b))
				elif ":< GUI Resource Usage GDI: " in line: lineObjs.append(JournalGUIResourceUsage(i,line,b))
				elif line.startswith("' [Jrn.BasicFileInfo]"): lineObjs.append(JournalBasicFileInfo(i,line,b))
				elif line.startswith("Jrn.Data "): lineObjs.append(JournalData(i,line,b))
				elif line.startswith("Jrn.Directive "): lineObjs.append(JournalDirective(i,line,b))
				elif line.startswith("Jrn.Command "):
					lineObjs.append(JournalCommand(i,line,b))
					# We need to count commands so we can grab JournalSystemInformation lines
					if commandCount < 2: commandCount += 1
				elif line.startswith("Jrn.Key "): lineObjs.append(JournalKeyboardEvent(i,line,b))
				elif line.startswith("Jrn.AddInEvent "): lineObjs.append(JournalAddinEvent(i,line,b))
				elif line.startswith('Jrn.Wheel') or line.startswith('Jrn.MouseMove') or line.startswith('Jrn.LButtonUp') or line.startswith('Jrn.LButtonDown') or line.startswith('Jrn.LButtonDblClk') or line.startswith('Jrn.MButtonUp') or line.startswith('Jrn.MButtonDown') or line.startswith('Jrn.MButtonDblClk') or line.startswith('Jrn.RButtonUp') or line.startswith('Jrn.RButtonDown') or line.startswith('Jrn.RButtonDblClk') or line.startswith('Jrn.Scroll'):
					lineObjs.append(JournalMouseEvent(i,line,b))
				elif line.startswith('Jrn.Activate') or line.startswith('Jrn.AppButtonEvent') or line.startswith('Jrn.Browser') or line.startswith('Jrn.CheckBox') or line.startswith('Jrn.Close') or line.startswith('Jrn.ComboBox') or line.startswith('Jrn.DropFiles') or line.startswith('Jrn.Edit') or line.startswith('Jrn.Grid') or line.startswith('Jrn.ListBox') or line.startswith('Jrn.Maximize') or line.startswith('Jrn.Minimize') or line.startswith('Jrn.PropertiesPalette') or line.startswith('Jrn.PushButton') or line.startswith('Jrn.RadioButton') or line.startswith('Jrn.RibbonEvent') or line.startswith('Jrn.SBTrayAction') or line.startswith('Jrn.Size') or line.startswith('Jrn.SliderCtrl') or line.startswith('Jrn.TabCtrl') or line.startswith('Jrn.TreeCtrl') or line.startswith('Jrn.WidgetEvent'):
					lineObjs.append(JournalUIEvent(i,line,b))
				elif ":< SLOG $" in line: lineObjs.append(JournalWorksharingEvent(i,line,b))
				# append linebreaks to previous line
				elif lineObjs[-1].RawText[-1] == "_": lineObjs[-1].RawText = lineObjs[-1].RawText[:-1] + line
				# append linebreaks in commands
				elif line[0] == ",": lineObjs[-1].RawText = (lineObjs[-1].RawText + line).replace(" _,",",")
				elif line[0] == "'":
					# append linebreaks in API Messages
					if line[1] != " " and lineObjs[-1].Type == 'JournalAPIMessage' and not lineObjs[-1].RawText.endswith("}"): lineObjs[-1].RawText += " " + line[1:]
					elif line[1] != " " and lineObjs[-1].Type == 'JournalTimeStamp': lineObjs[-1].RawText += " " + line[1:]
					elif sysinfoStarted:
						if ":< PROCESSOR INFORMATION:" in line: 
							sysinfoType = "Processor"
							lineObjs.append(JournalComment(i,line,b))
						elif ":< VIDEO CONTROLLER INFORMATION:" in line: 
							sysinfoType = "VideoController"
							lineObjs.append(JournalComment(i,line,b))
						elif ":< PRINTER INFORMATION:" in line: 
							sysinfoType = "Printer"
							lineObjs.append(JournalComment(i,line,b))
						elif ":< PRINTER CONFIGURATION INFORMATION:" in line: 
							sysinfoType = "PrinterConfiguration"
							lineObjs.append(JournalComment(i,line,b))
						elif " INFORMATION:" in line: 
							sysinfoType = "Unknown"
							lineObjs.append(JournalComment(i,line,b))
						elif ":<    " in line: 
							if line.split(":<    ")[-1].startswith(" "): lineObjs.append(JournalComment(i,line,b))
							else: lineObjs.append(JournalSystemInformation(i,line,b,sysinfoType))
						else: lineObjs.append(JournalComment(i,line,b))
					else:
						if not sysinfoStarted:
							if ":< OPERATING SYSTEM INFORMATION:" in line: 
								sysinfoStarted = True
								sysinfoType = 'OperatingSystem'
						lineObjs.append(JournalComment(i,line,b))
				else: lineObjs.append(JournalMiscCommand(i,line,b))
				i += 1				
		jBlockCount = b	
		# Round 2: Process raw multiline text and fill type-specific attributes
		machineNameFound = False
		OSVersionFound = False
		sysinfoItem = 0
		for line in lineObjs:
			if line.Type == 'JournalAPIMessage':
				line.MessageText = line.RawText.split("{ ")[1].split(" }")[0].strip()
				if line.MessageText.startswith("Registered an external service"): line.MessageType = "RegisteredExternalService"
				elif line.MessageText.startswith("Registered an external server") or line.MessageText.startswith("An external server has been registered"): line.MessageType = "RegisteredExternalServer"
				elif line.MessageText.startswith("Starting External DB Application"): line.MessageType = "StartingExternalDBApp"
				elif line.MessageText.startswith("Starting External Application"): line.MessageType = "StartingExternalApp"
				elif line.MessageText.startswith("Registering"): line.MessageType = "RegisteringEvent"
				elif line.MessageText.startswith("Replacing command id"): line.MessageType = "ReplacingCommandID"
				elif line.MessageText.startswith("API registering command"): line.MessageType = "RegisteringCommandEvent"
				elif line.MessageText.startswith("Added pushbutton"): line.MessageType = "AddedPushbutton"
				elif line.MessageText.startswith("Unregistering"): line.MessageType = "UnregisteringEvent"
				elif line.MessageText.startswith("Restoring command id"): line.MessageType = "RestoringCommandID"
				elif line.MessageText.startswith("API unregistering command"): line.MessageType = "UnregisteringCommandEvent"
				elif line.MessageText.startswith("System."): line.MessageType = "Exception"
				else: line.MessageType = "Unknown"
			elif line.Type == 'JournalDirective':
				d1 = line.RawText.split('"  , ')
				if len(d1) > 1:
					KeyCandidate = d1[0][15:]
					# Allow for different formatting in Revit 2022
					if KeyCandidate.startswith('"'): KeyCandidate = KeyCandidate[1:]
					line.Key = KeyCandidate
					for d2 in d1[1].split(","):
						line.Values.append(d2.strip().replace('"',''))
				else:
					# Very rare case where a multiline error msg is inserted between key and values
					d1 = line.RawText.split('"')
					if len(d1) > 1:	line.Key = d1[1]
					if len(d1) > 2: line.Values.append(d1[2])
				# Add Revit version to journal metadata
				if line.Key == 'Version': jVersion = int(line.Values[0][:4])
				# Add username to journal metadata
				elif line.Key == 'Username': jUsername = line.Values[0]
			elif line.Type == 'JournalData':
				d1 = line.RawText.split('"  , ')
				KeyCandidate = d1[0][10:]
				# Allow for different formatting in Revit 2022
				if KeyCandidate.startswith('"'): KeyCandidate = KeyCandidate[1:]
				line.Key = KeyCandidate
				# if this line is cut off don't try to extract values
				if len(d1) > 1:
					for d2 in d1[1].split(","):
						line.Values.append(d2.strip().replace('"',''))
			elif line.Type == 'JournalWorksharingEvent':
				ws = line.RawText.split(':< SLOG ')[-1].split()
				line.SessionID = ws[0]
				if len(ws) > 2: line.DateTime = time.strptime(ws[1] + " " + ws[2])
				if len(ws) > 3: line.Text = ' '.join(ws[3:])
			elif line.Type == 'JournalSystemInformation':
				si = line.RawText.split(':<    ')[-1].split(' : ')
				line.Key = si[0]
				if len(si) > 1: line.Value = si[1]
				else: line.Value = None
				if line.SystemInformationType == "Processor" and line.Key == "AddressWidth": sysinfoItem += 1
				elif line.SystemInformationType == "VideoController" and line.Key == "AdapterCompatibility": sysinfoItem += 1
				elif line.SystemInformationType == "Printer" and line.Key == "Caption": sysinfoItem += 1
				elif line.SystemInformationType == "PrinterConfiguration" and line.Key == "Color": sysinfoItem += 1
				if not OSVersionFound:
					if "Caption :" in line.RawText:
						jOSVersion = line.RawText.split(":")[-1].strip()
						OSVersionFound = True
				line.ItemNumber = sysinfoItem
			elif line.Type == 'JournalCommand':
				c1 = line.RawText.replace("  "," ").split('" , "')
				line.CommandType = c1[0][13:]
				# only try to process command ID if line is complete
				if len(c1) > 1:
					c2 = c1[1].split(" , ")
					line.CommandDescription = c2[0]
					# only try to process command ID if line is complete
					if len(c2) > 1:
						CommandIDCandidate = c2[1][:-1]
						# Allow for different formatting in Revit 2022
						if CommandIDCandidate.endswith('"'): CommandIDCandidate = CommandIDCandidate = CommandIDCandidate[:-1]
						line.CommandID = CommandIDCandidate
			elif line.Type == 'JournalMouseEvent':
				m1 = line.RawText.split(" ",1)
				line.MouseEventType = m1[0][4:].strip()
				if len(m1) > 1:
					for m2 in m1[1].split(","):
						potential_int = m2.strip().rstrip('\x00')
						# don't try converting broken lines
						if len(potential_int) > 0: line.Data.append(int(potential_int))
			elif line.Type == 'JournalKeyboardEvent': line.Key = line.RawText.split('"')[1]
			elif line.Type == 'JournalBasicFileInfo':
				bfi = map(list, zip(*[x.split(":",1) for x in line.RawText[22:].split("Rvt.Attr.")[1:]]))
				# don't try converting broken lines 
				if len(bfi) > 1:
					bfidict = dict(zip(bfi[0], [x.strip() for x in bfi[1]]))
					if "Worksharing" in bfidict and bfidict["Worksharing"] != "": line.Worksharing = bfidict["Worksharing"]
					if "CentralModelPath" in bfidict and bfidict["CentralModelPath"] != "": line.CentralModelPath = bfidict["CentralModelPath"]
					if "LastSavePath" in bfidict and bfidict["LastSavePath"] != "": 
						line.LastSavePath = bfidict["LastSavePath"]
						line.FileName = bfidict["LastSavePath"].split("\\")[-1]
					if "LocaleWhenSaved" in bfidict and bfidict["LocaleWhenSaved"] != "": line.Locale = bfidict["LocaleWhenSaved"]
			elif line.Type == 'JournalGUIResourceUsage':
				g2 = []
				for g1 in line.RawText.split(","):
					g2.append(int(g1.strip().split()[-1]))
				line.Available = g2[0]
				line.Used = g2[1]
				line.User = g2[2]
			elif line.Type == 'JournalUIEvent':
				d1 = line.RawText.split(" ",1)
				line.UIEventType = d1[0][4:]
				if line.UIEventType not in ("Maximize", "Minimize", "Restore"):
					for d2 in d1[1].split(","):
						d3 = d2.strip().replace('"','').strip()
						if line.UIEventType in ("RibbonEvent", "SBTrayAction"):
							for d4 in d3.split(":"):
								d4 = d4.strip()
								if d4 != "": line.Data.append(d4)
						elif line.UIEventType == "Browser":
							for d4 in d3.split(">>"):
								d4 = d4.strip()
								if d4 != "": line.Data.append(d4)
						elif d3 != "": line.Data.append(d3)
			elif line.Type == 'JournalAddinEvent': line.MessageText = line.RawText.split('"')[3]
			elif line.Type == 'JournalTimeStamp':
				line.TimeStampType = line.RawText[1]
				ts1 = line.RawText.split(";")
				# if this line is cut off don't try to extract datetime or description
				if len(ts1) > 1: 
					try:
						line.DateTime = time.strptime(ts1[0][3:])
					# After starting RhinoInsideRevit month name abbreviations will be localized
					# If we set the locale once parsing should continue to work
					except:
						import locale
						locale.setlocale(locale.LC_ALL, '')
						line.DateTime = time.strptime(ts1[0][3:])
					# formatting until Revit 2019
					if ':<' in ts1[1]: line.Description = ts1[1][7:].strip()
					# formatting as of Revit 2020
					else: line.Description = ts1[1][2:].strip()
					if line.Description.startswith("DBG_ERROR:"):
						line.DebugInfoType = "Error"
						line.Description = line.Description.replace("DBG_ERROR: ","")
					elif line.Description.startswith("DBG_WARN:"):
						line.DebugInfoType = "Warning"
						line.Description = line.Description.replace("DBG_WARN: ","")
					elif line.Description.startswith("DBG_INFO:"):
						line.DebugInfoType = "Info"
						line.Description = line.Description.replace("DBG_INFO: ","")
			elif line.Type == 'JournalMemoryMetrics':
				if "Initial VM" in line.RawText: m1 = line.RawText.split(":",2)[2].replace(";","").split()
				else: m1 = line.RawText.split(":",6)[6].split()
				m3 = []
				m4 = []
				for m2 in m1:
					if m2.isdigit(): m3.append(int(m2))
					elif m2 in ("Avail","Used","Peak"): m4.append(m2)
				if len(m3) > 0: line.VMAvailable = m3[0]
				if len(m3) > 1: line.VMUsed = m3[1]
				if len(m3) == 6:		
					line.VMPeak = m3[2]
					line.RAMAvailable = m3[3]
					line.RAMUsed = m3[4]
					line.RAMPeak = m3[5]
				elif len(m3) == 5:
					if m4[2] == "Avail":
						line.VMPeak = None
						line.RAMAvailable = m3[2]
						line.RAMUsed = m3[3]
						line.RAMPeak = m3[4]
					elif m4[2] == "Peak":
						line.VMPeak = m3[2]
						line.RAMAvailable = m3[3]
						line.RAMUsed = m3[4]
						line.RAMPeak = None
				elif len(m3) == 4:
					line.VMPeak = None
					line.RAMAvailable = m3[2]
					line.RAMUsed = m3[3]
					line.RAMPeak = None
			elif line.Type == 'JournalComment':
				if "this journal =" in line.RawText: jPath = line.RawText.split("=")[1].strip()
				elif line.RawText.startswith("' Build:"): jBuild = line.RawText.split(":")[1].strip()
				elif line.RawText.startswith("' Branch:"): jBranch = line.RawText.split(":")[1].strip()
				elif not machineNameFound:
					if "Additional IP address/name found for host" in line.RawText:
						jMachineName = line.RawText.split(":")[1].split(" ")[-1]
						machineNameFound = True
		# Create journal object
		journal = Journal(lineObjs, jVersion, jUsername, jBlockCount, jPath, jBuild, jBranch, jMachineName, jOSVersion)
		for line in journal.Lines:
			line.Journal = journal
		# Compute total processing time of this node
		journal.ProcessingTime = time.time() - processing_started
		return journal
	except:
		import traceback
		if line:
			if hasattr(line, "RawText"):
				return traceback.format_exc() + '\nCould not parse line:\n' + line.RawText
			else: return traceback.format_exc()
		else: return traceback.format_exc()

if isinstance(IN[0], list): OUT = [JournalFromPath(x) for x in IN[0]]
else: OUT = JournalFromPath(IN[0])