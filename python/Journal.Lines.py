import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalLines(journal):
	if journal.__repr__() == 'Journal': return journal.Lines
	else: return []

OUT = process_input(journalLines,IN[0])