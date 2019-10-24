import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LoadedLinkDuration(link):
	if link.__repr__() == 'LoadedLink': return link.LoadDuration
	else: return None

OUT = process_input(LoadedLinkDuration,IN[0])