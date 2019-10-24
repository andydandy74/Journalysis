import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalOSVersion(journal):
	if journal.__repr__() == 'Journal': return journal.OSVersion
	else: return None

OUT = process_input(journalOSVersion,IN[0])