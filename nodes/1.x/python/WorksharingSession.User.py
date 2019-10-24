import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionUser(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.User
	else: return None

OUT = process_input(WSSessionUser,IN[0])