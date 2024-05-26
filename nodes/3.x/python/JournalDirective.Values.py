import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalDirectiveValues(jdirective):
	if jdirective.__repr__() == 'JournalDirective': return jdirective.Values
	else: return []

OUT = process_input(journalDirectiveValues,IN[0])