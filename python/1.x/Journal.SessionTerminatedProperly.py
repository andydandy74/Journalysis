import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSessionTerminatedProperly(journal):
	if journal.__repr__() == 'Journal': return journal.WasSessionTerminatedProperly()
	else: return False

OUT = process_input(journalSessionTerminatedProperly,IN[0])