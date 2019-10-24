import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSEventDateTime(wsevent):
	if wsevent.__repr__() == 'WorksharingEvent': return wsevent.DateTime
	else: return None

OUT = process_input(WSEventDateTime,IN[0])