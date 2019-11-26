import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionLoadedLinks(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.GetLoadedLinks()
	else: return None

OUT = process_input(WSSessionLoadedLinks,IN[0])