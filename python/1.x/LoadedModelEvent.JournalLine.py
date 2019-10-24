import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def LMEJournalLine(modelevent):
	if modelevent.__repr__() == 'LoadedModelEvent': return modelevent.JournalLine
	else: return None

OUT = process_input(LMEJournalLine,IN[0])