import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAVersion(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Version
	else: return None

OUT = process_input(LAVersion,IN[0])