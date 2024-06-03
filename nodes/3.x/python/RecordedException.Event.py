import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionEvent(input):
	if input.__repr__() == 'RecordedException': return input.Event
	else: return None

OUT = process_input(RecordedExceptionEvent,IN[0])