import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMouseEventData(input):
	if input.__repr__() == 'JournalMouseEvent': return input.Data
	else: return []

OUT = process_input(journalMouseEventData,IN[0])