import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionBuild(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.RevitBuild
	else: return None

OUT = process_input(WSSessionBuild,IN[0])