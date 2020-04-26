import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def SyncEventWasAborted(syncevent):
	if syncevent.__repr__() == 'SyncEvent': return syncevent.WasAborted
	else: return None

OUT = process_input(SyncEventWasAborted,IN[0])