import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineNext(jline):
	if hasattr(jline, 'Next'): 
		return jline.Next()
	else: return None

OUT = process_input(journalLineNext, IN[0])