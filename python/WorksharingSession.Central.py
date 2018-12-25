import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionCentral(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Central
	else: return None

OUT = process_input(WSSessionCentral,IN[0])