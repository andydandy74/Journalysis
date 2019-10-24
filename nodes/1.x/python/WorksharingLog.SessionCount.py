import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSLogSessionCount(wslog):
	if wslog.__repr__() == 'WorksharingLog': return wslog.SessionCount
	else: return None

OUT = process_input(WSLogSessionCount,IN[0])