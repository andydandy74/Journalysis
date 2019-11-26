import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionStart(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Start
	else: return None

OUT = process_input(WSSessionStart,IN[0])