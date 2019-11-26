import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalGUIResourceUsageAvailable(jguiru):
	if jguiru.__repr__() == 'JournalGUIResourceUsage': return jguiru.Available
	else: return None

OUT = process_input(journalGUIResourceUsageAvailable,IN[0])