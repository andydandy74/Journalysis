import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineAllNext(jline):
	if hasattr(jline, 'AllNext'): 
		return jline.AllNext()
	else: return None

OUT = process_input(journalLineAllNext, IN[0])