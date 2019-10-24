import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMEvents(model):
	if model.__repr__() == 'LoadedModel': return model.Events
	else: return []

OUT = process_input(LMEvents,IN[0])