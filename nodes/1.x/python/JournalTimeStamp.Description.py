import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalTimeStampDescription(input):
	if input.__repr__() == 'JournalTimeStamp': return input.Description
	else: return None

OUT = process_input(journalTimeStampDescription,IN[0])