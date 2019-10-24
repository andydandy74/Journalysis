import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalAPIMessageText(japimsg):
	if japimsg.__repr__() == 'JournalAPIMessage': return japimsg.MessageText
	else: return None

OUT = process_input(journalAPIMessageText,IN[0])