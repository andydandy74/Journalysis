import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionMessage(input):
	if input.__repr__() == 'RecordedException': return input.Message
	else: return None

OUT = process_input(RecordedExceptionMessage,IN[0])