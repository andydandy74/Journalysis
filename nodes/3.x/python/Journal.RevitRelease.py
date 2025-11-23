import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalRelease(journal):
	if journal.__repr__() == 'Journal': return journal.Release
	else: return None

OUT = process_input(journalRelease,IN[0])