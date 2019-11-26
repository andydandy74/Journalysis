import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMVersion(model):
	if model.__repr__() == 'LoadedModel': return model.GetVersion()
	else: return None

OUT = process_input(LMVersion,IN[0])