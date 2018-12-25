import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalPath(journal):
	if journal.__repr__() == 'Journal': return journal.Path
	else: return None

OUT = process_input(journalPath,IN[0])