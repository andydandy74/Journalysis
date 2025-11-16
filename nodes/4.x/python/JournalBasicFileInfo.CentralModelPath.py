import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBFICentralModelPath(jbfi):
	if jbfi.__repr__() == 'JournalBasicFileInfo': return jbfi.CentralModelPath
	else: return None

OUT = process_input(journalBFICentralModelPath,IN[0])