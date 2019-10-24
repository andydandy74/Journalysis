import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionSyncEvents(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.GetSyncEvents()
	else: return None

OUT = process_input(WSSessionSyncEvents,IN[0])