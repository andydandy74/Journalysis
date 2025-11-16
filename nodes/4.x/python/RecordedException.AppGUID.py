import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionAppGUID(input):
	if input.__repr__() == 'RecordedException': return input.AppGUID
	else: return None

OUT = process_input(RecordedExceptionAppGUID,IN[0])