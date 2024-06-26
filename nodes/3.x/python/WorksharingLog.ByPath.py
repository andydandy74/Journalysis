import clr
import time
from System import DateTime

class WorksharingLog:
	def __init__(self, version, sessions):
		self.Version = version
		self.Sessions = sessions
		self.SessionCount = len(sessions)
		self.ProcessingTime = None
	def __repr__(self):
		return "WorksharingLog"
	def AllSessionsUseSameBuild(self):
		return len(set([x.RevitBuild for x in self.Sessions])) == 1
	def GetSessionByID(self, ID):
		sessionlookup = [x for x in self.Sessions if x.ID == ID]
		if len(sessionlookup) > 0: return sessionlookup[0]
		else: return None

class WorksharingSession:
	def __init__(self, id):
		self.ID = id
		self.Start = None
		self.End = None
		self.Date = None
		self.Duration = None
		self.User = None
		self.RevitVersion = None
		self.RevitBuild = None
		self.Journal = None
		self.HostAddress = None
		self.HostName = None
		self.ServerAddress = None
		self.ServerName = None
		self.Central = None
		self.Events = []
	def __repr__(self):
		return "WorksharingSession"
	def GetLoadDuration(self):
		if len([x for x in self.Events if x.Text == ">Open"]) == 0: return None
		else:
			openStart = [x.DateTime for x in self.Events if x.Text == ">Open"][0]
			openEnd = [x.DateTime for x in self.Events if x.Text == "<Open"]
			wsconfigStart = [x.DateTime for x in self.Events if x.Text == ">WSConfig"]
			wsconfigEnd = [x.DateTime for x in self.Events if x.Text == "<WSConfig"]
			if len(openEnd) == 0: return None
			elif len(wsconfigStart) > 0 and len(wsconfigEnd) > 0: return openEnd[0].Subtract(openStart).Add(wsconfigEnd[0].Subtract(wsconfigStart[0]))
			else: return openEnd[0].Subtract(openStart)
	def GetLoadedLinks(self):
		links = []
		for event in self.Events:
			if event.Text.startswith(">OpenLink"): links.append(LoadedLink(event.DateTime, event.Text.split("\"")[1]))
			elif event.Text == "<OpenLink" and len(links) > 0:
				links[-1].LoadEnd = event.DateTime
				links[-1].LoadDuration = event.DateTime.Subtract(links[-1].LoadStart)
		return links
	def GetSessionType(self):
		containsOpen = len([x for x in self.Events if x.Text == ">Open"]) > 0
		containsOpenCentral = len([x for x in self.Events if x.Text == ">Open:Central"]) > 0
		containsSTC = len([x for x in self.Events if x.Text == ">STC"]) > 0
		containsWSD = len([x for x in self.Events if x.Text == ">WSD"]) > 0
		containsReconnect = len([x for x in self.Events if x.Text.startswith(".ReconnectInMiddle")]) > 0
		if containsReconnect: return "Reconnected"
		if not containsOpen and containsSTC: return "CreateNewCentral"
		elif containsOpenCentral:
			if self.Events[1].Text == ">Open": return "CreateDetached"
			else: return "WorkInCentral"
		elif not containsOpen and containsWSD: return "ChooseWorksets"
		elif containsOpen and not containsOpenCentral: return "CreateLocalCopy"
		else: return "Unknown"
	def GetSyncEvents(self):
		events = []
		for event in self.Events:
			if event.Text == ">STC": events.append(SyncEvent(event.DateTime))
			elif event.Text == ">STC:RL:Read": events[-1].ReloadLatestCount += 1
			elif event.Text == ".STC:RL:LockRoot RW gaveUp": events[-1].WasAborted = True
			elif event.Text == "<STC" and len(events) > 0:
				events[-1].End = event.DateTime
				events[-1].Duration = event.DateTime.Subtract(events[-1].Start)
		return events
	def HasLoadedLinks(self):
		return len([x for x in self.Events if x.Text == "<OpenLink"]) > 0
	def WasTerminatedProperly(self):
		return self.End != None

class WorksharingEvent:
	def __init__(self, timestamp, text):
		self.DateTime = timestamp
		self.Text = text
	def __repr__(self):
		return "WorksharingEvent"

class SyncEvent:
	def __init__(self, start):
		self.Start = start
		self.End = None
		self.Duration = None
		self.ReloadLatestCount = 0
		self.WasAborted = False
	def __repr__(self):
		return "SyncEvent"
		
class LoadedLink:
	def __init__(self, start, linkpath):
		self.LoadStart = start
		self.LoadEnd = None
		self.LoadDuration = None
		self.FileName = linkpath.split("\\")[-1]
		self.FullPath = linkpath
	def __repr__(self):
		return "LoadedLink"

def WSLogFromPath(path):
	line = None
	try:
		processing_started = time.time()
		sessions = []
		version = None
		with open(path, 'r', encoding='utf-16-le') as slog:
			for line in slog.readlines():
				line = line.lstrip(' ').rstrip('\n')
				if line.startswith("$"):
					contents = line.split()
					session_lookup = [x for x in sessions if x.ID == contents[0]]
					if len(session_lookup) == 0:
						sessions.append(WorksharingSession(contents[0]))
						current_session = sessions[-1]
					else: current_session = session_lookup[0]
					timestamp = DateTime.Parse(contents[1] + " " + contents[2])
					text = ' '.join(contents[3:])
					event = WorksharingEvent(timestamp, text)
					current_session.Events.append(event)
					if text.startswith(">Session"): 
						sessions[-1].Start = timestamp
						sessions[-1].Date = timestamp.Date
					elif text.startswith(".ReconnectInMiddle"):
						current_session.Start = current_session.Events[0].DateTime
						current_session.Date = current_session.Events[0].DateTime.Date
					else: current_session.End = timestamp
				elif line.startswith("user"): sessions[-1].User = line.split('user="')[-1].replace('"','')
				elif line.startswith("build"):
					versioninfo = line.split('build="')[-1].split()
					sessions[-1].RevitVersion = int(versioninfo[0])
					sessions[-1].RevitBuild = versioninfo[-1].replace('"','')
				elif line.startswith("journal"): sessions[-1].Journal = line.split('journal="')[-1].replace('"','')
				elif line.startswith("host"):
					hostinfo = line.split('host=')[-1].split()
					sessions[-1].HostAddress = hostinfo[0]
					sessions[-1].HostName = hostinfo[-1].replace('"','')
				elif line.startswith("server"):
					serverinfo = line.split('server=')[-1].split()
					sessions[-1].ServerAddress = serverinfo[0]
					sessions[-1].ServerName = serverinfo[-1].replace('"','')
				elif line.startswith("central"): sessions[-1].Central = line.split('central="')[-1].replace('"','')
				elif line[1:].startswith("Worksharing"): version = line.split("Version ")[-1].split(",")[0]
		for session in sessions: 
			if session.Start and session.End: session.Duration = session.End.Subtract(session.Start)
		WSLog = WorksharingLog(version, sessions)
		WSLog.ProcessingTime = time.time() - processing_started
		return WSLog
	except:
		import sys
		import traceback
		if line: return traceback.format_exc() + '\nCould not parse line:\n' + line
		else: return traceback.format_exc()

if isinstance(IN[0], list): OUT = [WSLogFromPath(x) for x in IN[0]]
else: OUT = WSLogFromPath(IN[0])