import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionHostName(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.HostName
	else: return None

OUT = process_input(WSSessionHostName,IN[0])