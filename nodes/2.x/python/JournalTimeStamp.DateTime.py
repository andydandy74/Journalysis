import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalTimeStampDateTime(input):
	if input.__repr__() == 'JournalTimeStamp': return input.DateTime
	else: return None

OUT = process_input(journalTimeStampDateTime,IN[0])