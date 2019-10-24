import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionUsesCentralModel(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.UsesCentralModel()
	else: return None

OUT = process_input(WSSessionUsesCentralModel,IN[0])