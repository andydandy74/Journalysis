import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalTimeStampType(input):
	if input.__repr__() == 'JournalTimeStamp': return input.TimeStampType
	else: return None

OUT = process_input(journalTimeStampType,IN[0])