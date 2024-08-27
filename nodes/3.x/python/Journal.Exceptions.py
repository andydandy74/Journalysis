import clr

class RecordedException:
	def __init__(self, journalLine):
		self.JournalLine = journalLine
		self.Type = "Unknown"
		self.Message = None
		self.Event = None
		self.AppName = None
		self.AppGUID = None
		self.StackTrace = None
	def __repr__(self):
		return "RecordedException"

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalGetExceptions(journal):
	if journal.__repr__() == 'Journal': 
		exclines = journal.GetExceptions()
		excobjs = []
		for line in exclines:
			newexc = RecordedException(line)
			addnewexc = True
			if line.Type == "JournalTimeStamp": 
				lp1 = line.Description.split("ApplicationException is being thrown on behalf of the function <",1)
				if len(lp1) > 1:
					newexc.Type = "ApplicationException"
					newexc.Message = lp1[0]
					lp2 = lp1[1].split(">",1)
					newexc.StackTrace = lp2[0]
				else:
					if "ArchiveException" in line.Description: newexc.Type = "ArchiveException"
					elif line.Description.startswith("ExceptionCode"): newexc.Type = "Fatal Error"
					newexc.Message = line.Description
			elif line.Type == "JournalAPIMessage": 
				if line.MessageText.startswith("An external service execution throws"):
					addnewexc = False
					lp1 = line.MessageText.split(":")
					if len(excobjs[-1].Message) > 0: excobjs[-1].Message + " "
					excobjs[-1].Message = excobjs[-1].Message + lp1[0]
					if len(lp1) > 1:
						lp2 = lp1[1].split("; ")
						excobjs[-1].AppName = lp2[0].replace(" Name(","").replace(")","")
						if len(lp2) > 3: excobjs[-1].AppGUID = lp2[2] + ", " + lp2[3]
				else:
					lp1 = line.MessageText.split(" exception(",1)
					newexc.Type = lp1[0]
					if len(lp1) > 1: 
						lp2 = lp1[1].split(") was thrown from a handler of ",1)
						newexc.Message = lp2[0]
						if len(lp2) > 1: 
							lp3 = lp2[1].split("event. The API event handler was registered by application ",1)
							newexc.Event = lp3[0]
							if len(lp3) > 1: 
								lp4 = lp3[1].split(" (",1)
								newexc.AppName = lp4[0]
								if len(lp4) > 1:
									lp5 = lp4[1].split(")",1)
									newexc.AppGUID = lp5[0]
			elif line.Type == "JournalComment":
				lp1 = line.RawText.split("Exception caught from managed method ",1)
				if len(lp1) > 1:
					lp2 = lp1[1].split(" <",1)
					newexc.StackTrace = lp2[0]
					if len(lp2) > 1:
						lp3 = lp2[1].split("> <",1)
						newexc.Type = lp3[0]
						if len(lp3) > 1: newexc.Message = lp3[1].strip()[:-1]
				else: 
					lp1 = line.RawText.split("External Command Registration Exception: ",1)
					lp2 = line.RawText.split(":< ")
					if len(lp1) > 1:
						newexc.Type = "External Command Registration Exception"
						newexc.Message = lp1[1]
					elif "OpenStream FileException" in line.RawText: 
						newexc.Type = "OpenStream FileException"
						newexc.Message = line.RawText.split("OpenStream FileException",1)[1].strip()
					elif "exceptionsInOnOpenDocument" in line.RawText:
						addnewexc = False
						lp3 = line.RawText.split("<<")
						if len(lp3) > 1: excobjs[-1].Message = excobjs[-1].Message + " " + lp3[1]
					elif len(lp2) > 1: 
						if "ExceptionProxy:" in lp2[1]:
							lp3 = lp2[1].split(": ",1)
							newexc.Type = lp3[0]
							if len(lp3) > 1:
								lp4 = lp3[1].split(" at",1)
								newexc.Message = lp4[0]
								if len(lp4) > 1: newexc.StackTrace = "at" + lp4[1]
						else: newexc.Message = lp2[1]
					else: newexc.Message = line.RawText
			else: newexc.Message = line.RawText
			if addnewexc: excobjs.append(newexc)
		return excobjs
	else: return []

OUT = process_input(journalGetExceptions,IN[0])