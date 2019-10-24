import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionDate(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Date
	else: return None

OUT = process_input(WSSessionDate,IN[0])