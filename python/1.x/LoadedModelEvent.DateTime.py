import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMEDateTime(modelevent):
	if modelevent.__repr__() == 'LoadedModelEvent': return modelevent.DateTime
	else: return None

OUT = process_input(LMEDateTime,IN[0])