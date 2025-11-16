import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalDataValues(jdata):
	if jdata.__repr__() == 'JournalData': return jdata.Values
	else: return []

OUT = process_input(journalDataValues,IN[0])