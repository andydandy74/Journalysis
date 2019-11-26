import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalVersion(journal):
	if journal.__repr__() == 'Journal': return journal.Version
	else: return None

OUT = process_input(journalVersion,IN[0])