import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandID(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.ID
	else: return None

OUT = process_input(ksCommandID,IN[0])