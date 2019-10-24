import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMachine(journal):
	if journal.__repr__() == 'Journal': return journal.Machine
	else: return None

OUT = process_input(journalMachine,IN[0])