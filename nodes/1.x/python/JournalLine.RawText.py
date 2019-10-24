import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineRawText(jline):
	if hasattr(jline, 'RawText'): return jline.RawText
	else: return None

OUT = process_input(journalLineRawText,IN[0])