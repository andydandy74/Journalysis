import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSEventText(wsevent):
	if wsevent.__repr__() == 'WorksharingEvent': return wsevent.Text
	else: return None

OUT = process_input(WSEventText,IN[0])