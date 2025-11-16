import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def SyncEventRLCount(syncevent):
	if syncevent.__repr__() == 'SyncEvent': return syncevent.ReloadLatestCount
	else: return None

OUT = process_input(SyncEventRLCount,IN[0])