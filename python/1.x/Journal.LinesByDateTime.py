import clr

def process_input(func, inputs):
	if isinstance(inputs[0], list): return [func(x, inputs[1], inputs[2]) for x in inputs[0]]
	else: return func(inputs[0], inputs[1], inputs[2])
	
def journalLinesByDateTime(journal, fromDateTime, toDateTime):
	if journal.__repr__() == 'Journal': return journal.GetLinesByDateTime(fromDateTime, toDateTime)
	else: return None

OUT = process_input(journalLinesByDateTime, IN)