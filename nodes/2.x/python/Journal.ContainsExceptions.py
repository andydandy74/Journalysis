import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalContainsExceptions(journal):
	if journal.__repr__() == 'Journal': return journal.ContainsExceptions()
	else: return False

OUT = process_input(journalContainsExceptions,IN[0])