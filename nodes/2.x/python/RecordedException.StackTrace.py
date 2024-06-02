import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionStackTrace(input):
	if input.__repr__() == 'RecordedException': return input.StackTrace
	else: return None

OUT = process_input(RecordedExceptionStackTrace,IN[0])