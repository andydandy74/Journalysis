import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionServerName(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.ServerName
	else: return None

OUT = process_input(WSSessionServerName,IN[0])