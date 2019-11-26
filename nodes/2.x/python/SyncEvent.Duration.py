import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def SyncEventDuration(syncevent):
	if syncevent.__repr__() == 'SyncEvent': return syncevent.Duration
	else: return None

OUT = process_input(SyncEventDuration,IN[0])