import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionHostAddress(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.HostAddress
	else: return None

OUT = process_input(WSSessionHostAddress,IN[0])