import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAVendor(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.Vendor
	else: return None

OUT = process_input(LAVendor,IN[0])