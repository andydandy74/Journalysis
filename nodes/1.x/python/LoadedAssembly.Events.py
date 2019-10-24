import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAEvents(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Events
	else: return []

OUT = process_input(LAEvents,IN[0])