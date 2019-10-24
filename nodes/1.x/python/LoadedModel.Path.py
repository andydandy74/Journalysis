import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMPath(model):
	if model.__repr__() == 'LoadedModel': return model.Path
	else: return None

OUT = process_input(LMPath,IN[0])