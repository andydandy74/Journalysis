import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalGUIResourceUsageUser(jguiru):
	if jguiru.__repr__() == 'JournalGUIResourceUsage': return jguiru.User
	else: return None

OUT = process_input(journalGUIResourceUsageUser,IN[0])