import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalWSEventDateTime(jwsevent):
	if jwsevent.__repr__() == 'JournalWorksharingEvent': return jwsevent.DateTime
	else: return None

OUT = process_input(journalWSEventDateTime,IN[0])