import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def ksCommandHasShortcuts(ksc):
	if ksc.__repr__() == 'KeyboardShortcutCommand': return ksc.HasShortcuts
	else: return False

OUT = process_input(ksCommandHasShortcuts,IN[0])