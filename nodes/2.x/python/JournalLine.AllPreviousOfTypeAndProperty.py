import clr

def process_input(func, inputs):
	if isinstance(inputs[1], list) or isinstance(inputs[2], list) or isinstance(inputs[3], list): return None
	elif isinstance(inputs[0], list): return [func(x, inputs[1], inputs[2], inputs[3]) for x in inputs[0]]
	else: return func(inputs[0], inputs[1], inputs[2], inputs[3])
	
def journalLineAllPreviousOfTypeAndProperty(jline, type, prop, val):
	if hasattr(jline, 'AllPreviousOfTypeAndProperty'): 
		return jline.AllPreviousOfTypeAndProperty(type, prop, val)
	else: return None

OUT = process_input(journalLineAllPreviousOfTypeAndProperty, IN)