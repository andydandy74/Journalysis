import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def WSSessionJournal(wssess):
	if wssess.__repr__() == 'WorksharingSession': return wssess.Journal
	else: return None

OUT = process_input(WSSessionJournal,IN[0])