import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionCreatesNewCentral(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.CreatesNewCentral()
	else: return None

OUT = process_input(WSSessionCreatesNewCentral,IN[0])