import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLineCount(journal):
	if journal.__repr__() == 'Journal': return journal.LineCount
	else: return 0

OUT = process_input(journalLineCount,IN[0])