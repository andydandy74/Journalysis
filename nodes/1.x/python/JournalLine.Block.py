import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineBlock(jline):
	if hasattr(jline, 'Block'): return jline.Block
	else: return None

OUT = process_input(journalLineBlock,IN[0])