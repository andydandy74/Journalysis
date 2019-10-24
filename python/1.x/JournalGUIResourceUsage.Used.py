import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalGUIResourceUsageUsed(jguiru):
	if jguiru.__repr__() == 'JournalGUIResourceUsage': return jguiru.Used
	else: return None

OUT = process_input(journalGUIResourceUsageUsed,IN[0])