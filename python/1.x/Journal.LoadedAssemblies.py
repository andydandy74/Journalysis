import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLoadedAssemblies(journal):
	if journal.__repr__() == 'Journal': return journal.GetLoadedAssemblies()
	else: return None

OUT = process_input(journalLoadedAssemblies,IN[0])