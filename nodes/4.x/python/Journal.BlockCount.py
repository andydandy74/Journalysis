import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalBlockCount(journal):
	if journal.__repr__() == 'Journal': return journal.BlockCount
	else: return 0

OUT = process_input(journalBlockCount,IN[0])