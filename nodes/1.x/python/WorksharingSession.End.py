import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionEnd(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.End
	else: return None

OUT = process_input(WSSessionEnd,IN[0])