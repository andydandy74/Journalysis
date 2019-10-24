import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def SyncEventStart(syncevent):
	if syncevent.__repr__() == 'SyncEvent': return syncevent.Start
	else: return None

OUT = process_input(SyncEventStart,IN[0])