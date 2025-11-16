import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def RecordedExceptionJournalLine(input):
	if input.__repr__() == 'RecordedException': return input.JournalLine
	else: return None

OUT = process_input(RecordedExceptionJournalLine,IN[0])