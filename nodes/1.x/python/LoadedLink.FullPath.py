import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LoadedLinkFullPath(link):
	if link.__repr__() == 'LoadedLink': return link.FullPath
	else: return None

OUT = process_input(LoadedLinkFullPath,IN[0])