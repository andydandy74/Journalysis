import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionLoadDuration(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.GetLoadDuration()
	else: return None

OUT = process_input(WSSessionLoadDuration,IN[0])