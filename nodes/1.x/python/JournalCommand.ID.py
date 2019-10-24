import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalCommandID(jcommand):
	if jcommand.__repr__() == 'JournalCommand': return jcommand.CommandID
	else: return None

OUT = process_input(journalCommandID,IN[0])