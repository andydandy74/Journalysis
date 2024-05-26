import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalAPIMessageIsError(japimsg):
	if japimsg.__repr__() == 'JournalAPIMessage': return japimsg.IsError
	else: return None

OUT = process_input(journalAPIMessageIsError,IN[0])