import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineType(jline):
	if hasattr(jline, 'Type'): return jline.Type
	else: return None

OUT = process_input(journalLineType,IN[0])