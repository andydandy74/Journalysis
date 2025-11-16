import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalUser(journal):
	if journal.__repr__() == 'Journal': return journal.User
	else: return None

OUT = process_input(journalUser,IN[0])