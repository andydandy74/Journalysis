import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSLogVersion(wslog):
	if wslog.__repr__() == 'WorksharingLog': return wslog.Version
	else: return None

OUT = process_input(WSLogVersion,IN[0])