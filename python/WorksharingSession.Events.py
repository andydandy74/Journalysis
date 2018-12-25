import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionEvents(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Events
	else: return []

OUT = process_input(WSSessionEvents,IN[0])