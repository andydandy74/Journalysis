import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBFILocale(jbfi):
	if jbfi.__repr__() == 'JournalBasicFileInfo': return jbfi.Locale
	else: return None

OUT = process_input(journalBFILocale,IN[0])