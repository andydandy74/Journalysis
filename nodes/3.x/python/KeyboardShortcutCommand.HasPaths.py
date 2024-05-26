import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandHasPaths(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.HasPaths
	else: return False

OUT = process_input(ksCommandHasPaths,IN[0])