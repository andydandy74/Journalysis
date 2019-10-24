import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMouseEventType(input):
	if input.__repr__() == 'JournalMouseEvent': return input.MouseEventType
	else: return None

OUT = process_input(journalMouseEventType,IN[0])