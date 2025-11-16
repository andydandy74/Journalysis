import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineDateTime(jline):
	if hasattr(jline, 'GetDateTimeOfBlock'): 
		return jline.GetDateTimeOfBlock()
	else: return None

OUT = process_input(journalLineDateTime, IN[0])