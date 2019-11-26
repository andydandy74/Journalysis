import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAPath(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Path
	else: return None

OUT = process_input(LAPath,IN[0])