import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionType(input):
	if input.__repr__() == 'RecordedException': return input.Type
	else: return None

OUT = process_input(RecordedExceptionType,IN[0])