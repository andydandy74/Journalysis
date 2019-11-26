import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMinVMAvailable(journal):
	if journal.__repr__() == 'Journal': return journal.GetMinVMAvailable()
	else: return None

OUT = process_input(journalMinVMAvailable,IN[0])