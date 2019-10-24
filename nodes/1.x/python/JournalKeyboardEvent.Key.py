import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalKeyboardEventKey(jkeyboardevent):
	if jkeyboardevent.__repr__() == 'JournalKeyboardEvent': return jkeyboardevent.Key
	else: return None

OUT = process_input(journalKeyboardEventKey,IN[0])