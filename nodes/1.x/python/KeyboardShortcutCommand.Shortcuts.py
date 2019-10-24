import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandShortcuts(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.Shortcuts
	else: return []

OUT = process_input(ksCommandShortcuts,IN[0])