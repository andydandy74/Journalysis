import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionDuration(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Duration
	else: return None

OUT = process_input(WSSessionDuration,IN[0])