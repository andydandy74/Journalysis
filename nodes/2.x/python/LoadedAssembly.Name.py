import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAName(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Name
	else: return None

OUT = process_input(LAName,IN[0])