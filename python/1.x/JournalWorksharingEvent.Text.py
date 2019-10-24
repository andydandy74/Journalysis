import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalWSEventText(jwsevent):
	if jwsevent.__repr__() == 'JournalWorksharingEvent': return jwsevent.Text
	else: return None

OUT = process_input(journalWSEventText,IN[0])