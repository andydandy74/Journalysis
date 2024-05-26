import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandName(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.Name
	else: return None

OUT = process_input(ksCommandName,IN[0])