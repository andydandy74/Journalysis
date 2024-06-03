import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionAppName(input):
	if input.__repr__() == 'RecordedException': return input.AppName
	else: return None

OUT = process_input(RecordedExceptionAppName,IN[0])