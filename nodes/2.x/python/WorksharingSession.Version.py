import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionVersion(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.RevitVersion
	else: return None

OUT = process_input(WSSessionVersion,IN[0])