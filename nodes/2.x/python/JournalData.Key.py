import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalDataKey(jdata):
	if jdata.__repr__() == 'JournalData': return jdata.Key
	else: return None

OUT = process_input(journalDataKey,IN[0])