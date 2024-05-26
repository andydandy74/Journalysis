import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBFIWorksharing(jbfi):
	if jbfi.__repr__() == 'JournalBasicFileInfo': return jbfi.Worksharing
	else: return None

OUT = process_input(journalBFIWorksharing,IN[0])