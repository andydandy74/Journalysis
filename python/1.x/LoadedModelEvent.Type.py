import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMEType(modelevent):
	if modelevent.__repr__() == 'LoadedModelEvent': return modelevent.Type
	else: return None

OUT = process_input(LMEType,IN[0])