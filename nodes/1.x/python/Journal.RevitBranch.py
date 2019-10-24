import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBranch(journal):
	if journal.__repr__() == 'Journal': return journal.Branch
	else: return None

OUT = process_input(journalBranch,IN[0])