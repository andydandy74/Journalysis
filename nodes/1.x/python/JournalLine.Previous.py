import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLinePrevious(jline):
	if hasattr(jline, 'Previous'): 
		return jline.Previous()
	else: return None

OUT = process_input(journalLinePrevious, IN[0])