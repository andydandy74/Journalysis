import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalAPIMessageType(japimsg):
	if japimsg.__repr__() == 'JournalAPIMessage': return japimsg.MessageType
	else: return None

OUT = process_input(journalAPIMessageType,IN[0])