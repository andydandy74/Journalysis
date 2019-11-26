import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalProcessingTime(journal):
	if journal.__repr__() == 'Journal': return journal.ProcessingTime
	else: return None

OUT = process_input(journalProcessingTime,IN[0])