import clr

def process_input(func, input1, input2):
	if isinstance(input2, list): return [func(input1, x) for x in input2]
	else: return func(input1, input2)
	
def WSLogSessionByID(wslog, id):
	if wslog.__repr__() == 'WorksharingLog': return wslog.GetSessionByID(id)
	else: return None

OUT = process_input(WSLogSessionByID, IN[0], IN[1])