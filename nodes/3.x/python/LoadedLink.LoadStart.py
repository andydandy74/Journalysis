import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LoadedLinkStart(link):
	if link.__repr__() == 'LoadedLink': return link.LoadStart
	else: return None

OUT = process_input(LoadedLinkStart,IN[0])