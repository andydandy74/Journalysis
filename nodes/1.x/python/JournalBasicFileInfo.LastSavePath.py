import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBFILastSavePath(jbfi):
	if jbfi.__repr__() == 'JournalBasicFileInfo': return jbfi.LastSavePath
	else: return None

OUT = process_input(journalBFILastSavePath,IN[0])