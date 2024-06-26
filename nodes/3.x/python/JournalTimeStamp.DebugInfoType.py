import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalTimeStampDebugInfoType(input):
	if input.__repr__() == 'JournalTimeStamp': return input.DebugInfoType
	else: return None

OUT = process_input(journalTimeStampDebugInfoType,IN[0])