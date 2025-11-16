import clr

def process_input(func, inputs):
	if isinstance(inputs[0], list): return [func(x, inputs[1], inputs[2], inputs[3]) for x in inputs[0]]
	else: return func(inputs[0], inputs[1], inputs[2], inputs[3])
	
def journalLinesByTypeAndProperty(journal, type, prop, val):
	if journal.__repr__() == 'Journal': 
		return journal.GetLinesByTypeAndProperty(type, prop, val)
	else: return None

OUT = process_input(journalLinesByTypeAndProperty, IN)