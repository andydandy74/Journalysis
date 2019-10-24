import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSessionTime(journal):
	if journal.__repr__() == 'Journal': return journal.GetSessionTime()
	else: return None

OUT = process_input(journalSessionTime,IN[0])