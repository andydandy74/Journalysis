import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBFIFileName(jbfi):
	if jbfi.__repr__() == 'JournalBasicFileInfo': return jbfi.FileName
	else: return None

OUT = process_input(journalBFIFileName,IN[0])