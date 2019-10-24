import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSLogSessions(wslog):
	if wslog.__repr__() == 'WorksharingLog': return wslog.Sessions
	else: return []

OUT = process_input(WSLogSessions,IN[0])