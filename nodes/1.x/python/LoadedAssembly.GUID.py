import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAGuid(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.GUID
	else: return None

OUT = process_input(LAGuid,IN[0])