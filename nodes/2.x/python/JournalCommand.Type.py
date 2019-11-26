import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalCommandType(jcommand):
	if jcommand.__repr__() == 'JournalCommand': return jcommand.CommandType
	else: return None

OUT = process_input(journalCommandType,IN[0])