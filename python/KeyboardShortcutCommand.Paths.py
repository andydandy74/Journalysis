import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandPaths(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.Paths
	else: return []

OUT = process_input(ksCommandPaths,IN[0])