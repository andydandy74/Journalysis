import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalWSEventID(jwsevent):
	if jwsevent.__repr__() == 'JournalWorksharingEvent': return jwsevent.SessionID
	else: return None

OUT = process_input(journalWSEventID,IN[0])