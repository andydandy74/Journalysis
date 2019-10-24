import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBuild(journal):
	if journal.__repr__() == 'Journal': return journal.Build
	else: return None

OUT = process_input(journalBuild,IN[0])