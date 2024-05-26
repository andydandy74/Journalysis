import clr

def process_input(func, input1, input2):
	if isinstance(input1, list): return [func(x, input2) for x in input1]
	else: return func(input1, input2)
	
def journalLineAllPreviousOfType(jline, type):
	if hasattr(jline, 'AllPreviousOfType'): 
		return jline.AllPreviousOfType(type)
	else: return None

OUT = process_input(journalLineAllPreviousOfType, IN[0], IN[1])