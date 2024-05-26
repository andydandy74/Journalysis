import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSLogAllSessionsUseSameBuild(wslog):
	if wslog.__repr__() == 'WorksharingLog': return wslog.AllSessionsUseSameBuild()
	else: return None

OUT = process_input(WSLogAllSessionsUseSameBuild,IN[0])