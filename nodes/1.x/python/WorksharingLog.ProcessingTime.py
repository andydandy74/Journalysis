import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSLogProcessingTime(wslog):
	if wslog.__repr__() == 'WorksharingLog': return wslog.ProcessingTime
	else: return None

OUT = process_input(WSLogProcessingTime,IN[0])