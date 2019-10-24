import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMVersionHistory(model):
	if model.__repr__() == 'LoadedModel': return model.GetVersionHistory()
	else: return None

OUT = process_input(LMVersionHistory,IN[0])