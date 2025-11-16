import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def SyncEventEnd(syncevent):
	if syncevent.__repr__() == 'SyncEvent': return syncevent.End
	else: return None

OUT = process_input(SyncEventEnd,IN[0])