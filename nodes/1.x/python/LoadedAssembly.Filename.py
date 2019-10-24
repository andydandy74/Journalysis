import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAFilename(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Filename
	else: return None

OUT = process_input(LAFilename,IN[0])