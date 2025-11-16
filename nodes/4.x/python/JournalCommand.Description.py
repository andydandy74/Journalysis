import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalCommandDescription(jcommand):
	if jcommand.__repr__() == 'JournalCommand': return jcommand.CommandDescription
	else: return None

OUT = process_input(journalCommandDescription,IN[0])