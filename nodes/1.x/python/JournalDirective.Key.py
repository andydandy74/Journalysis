import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalDirectiveKey(jdirective):
	if jdirective.__repr__() == 'JournalDirective': return jdirective.Key
	else: return None

OUT = process_input(journalDirectiveKey,IN[0])