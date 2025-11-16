import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LoadedLinkFileName(link):
	if link.__repr__() == 'LoadedLink': return link.FileName
	else: return None

OUT = process_input(LoadedLinkFileName,IN[0])