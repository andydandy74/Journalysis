import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionHasLoadedLinks(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.HasLoadedLinks()
	else: return None

OUT = process_input(WSSessionHasLoadedLinks,IN[0])