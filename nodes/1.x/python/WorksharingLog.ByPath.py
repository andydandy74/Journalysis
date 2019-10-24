import clr
import time

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
		sessionlookup = [x for x in sessions if x.ID == ID]
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
	def CreatesDetached(self):
		return self.Events[1].Text == ">Open"
	def CreatesNewCentral(self):
		return len([x for x in self.Events if x.Text == ">Open"]) == 0
	def GetLoadDuration(self):
		if self.CreatesNewCentral(): return None
		else:
			openStart = [x.DateTime for x in self.Events if x.Text == ">Open"][0]
			openEnd = [x.DateTime for x in self.Events if x.Text == "<Open"]
			if len(openEnd) == 0: return None
			else: return openEnd[0] - openStart
	def GetLoadedLinks(self):
		links = []
		for event in self.Events:
			if event.Text.startswith(">OpenLink"): links.append(LoadedLink(event.DateTime, event.Text.split("\"")[1]))
			elif event.Text == "<OpenLink" and len(links) > 0:
				links[-1].LoadEnd = event.DateTime
				links[-1].LoadDuration = event.DateTime - links[-1].LoadStart				
		return links
	def GetSyncEvents(self):
		events = []
		for event in self.Events:
			if event.Text == ">STC": events.append(SyncEvent(event.DateTime))
			elif event.Text == "<STC" and len(events) > 0:
				events[-1].End = event.DateTime
				events[-1].Duration = event.DateTime - events[-1].Start
		return events
	def HasLoadedLinks(self):
		return len([x for x in self.Events if x.Text == "<OpenLink"]) > 0
	def UsesCentralModel(self):
		if self.CreatesNewCentral(): return True
		else: return len([x for x in self.Events if x.Text.startswith(">Open:Central")]) > 0
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
	try:
		processing_started = time.time()
		sessions = []
		version = None
		with open(path, 'r') as slog:
			for line in slog:
				line = line.lstrip().rstrip('\n')
				if line.startswith("$"):
					contents = line.split()
					session_lookup = [x for x in sessions if x.ID == contents[0]]
					if len(session_lookup) == 0:
						sessions.append(WorksharingSession(contents[0]))
						current_session = sessions[-1]
					else: current_session = session_lookup[0]
					timestamp = time.strptime(contents[1] + " " + contents[2])
					text = ' '.join(contents[3:])
					event = WorksharingEvent(timestamp, text)
					current_session.Events.append(event)
					if text.startswith(">Session"): 
						sessions[-1].Start = timestamp
						sessions[-1].Date = timestamp.Date
					else: current_session.End = timestamp
				elif line.startswith("user"): sessions[-1].User = line.split('user="')[-1][:-1]
				elif line.startswith("build"):
					versioninfo = line.split('build="')[-1].split()
					sessions[-1].RevitVersion = int(versioninfo[0])
					sessions[-1].RevitBuild = versioninfo[-1][:-1]
				elif line.startswith("journal"): sessions[-1].Journal = line.split('journal="')[-1][:-1]
				elif line.startswith("host"):
					hostinfo = line.split('host=')[-1].split()
					sessions[-1].HostAddress = hostinfo[0]
					sessions[-1].HostName = hostinfo[-1][1:-1]
				elif line.startswith("server"):
					serverinfo = line.split('server=')[-1].split()
					sessions[-1].ServerAddress = serverinfo[0]
					sessions[-1].ServerName = serverinfo[-1][1:-1]
				elif line.startswith("central"): sessions[-1].Central = line.split('central="')[-1][:-1]
				elif line.startswith("Worksharing"): version = line.split("Version ")[-1].split(",")[0]
		for session in sessions: session.Duration = session.End - session.Start
		WSLog = WorksharingLog(version, sessions)
		WSLog.ProcessingTime = time.time() - processing_started
		return WSLog
	except:
		import sys
		pyt_path = r'C:\Program Files (x86)\IronPython 2.7\Lib'
		sys.path.append(pyt_path)
		import traceback
		return traceback.format_exc()

if isinstance(IN[0], list): OUT = [WSLogFromPath(x) for x in IN[0]]
else: OUT = WSLogFromPath(IN[0])