import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineNumber(jline):
	if hasattr(jline, 'Number'): return jline.Number
	else: return None

OUT = process_input(journalLineNumber,IN[0])