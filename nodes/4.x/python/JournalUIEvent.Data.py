import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalUIEventData(input):
	if input.__repr__() == 'JournalUIEvent': return input.Data
	else: return None

OUT = process_input(journalUIEventData,IN[0])