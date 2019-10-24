import clr

def process_input(func, inputs):
	if isinstance(inputs[0], list): return [func(x, inputs[1]) for x in inputs[0]]
	else: return func(inputs[0], inputs[1])
	
def journalLinesByType(journal, type):
	if journal.__repr__() == 'Journal': 
		if isinstance(type, list): return journal.GetLinesByTypes(type)
		else: return journal.GetLinesByType(type)
	else: return None

OUT = process_input(journalLinesByType, IN)