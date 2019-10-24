import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAClass(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Class
	else: return None

OUT = process_input(LAClass,IN[0])