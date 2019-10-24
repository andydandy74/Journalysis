import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalUIEventType(input):
	if input.__repr__() == 'JournalUIEvent': return input.UIEventType
	else: return None

OUT = process_input(journalUIEventType,IN[0])