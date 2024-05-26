import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LoadedLinkEnd(link):
	if link.__repr__() == 'LoadedLink': return link.LoadEnd
	else: return None

OUT = process_input(LoadedLinkEnd,IN[0])