import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalDate(journal):
	if journal.__repr__() == 'Journal': return journal.GetDate()
	else: return None

OUT = process_input(journalDate,IN[0])