import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineAllPrevious(jline):
	if hasattr(jline, 'AllPrevious'): 
		return jline.AllPrevious()
	else: return None

OUT = process_input(journalLineAllPrevious, IN[0])