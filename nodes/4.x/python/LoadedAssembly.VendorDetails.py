import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LAVendorDetails(assembly):
	if assembly.__repr__() == 'LoadedAssembly': return assembly.VendorDetails
	else: return None

OUT = process_input(LAVendorDetails,IN[0])