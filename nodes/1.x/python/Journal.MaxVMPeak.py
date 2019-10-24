import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMaxVMPeak(journal):
	if journal.__repr__() == 'Journal': return journal.GetMaxVMPeak()
	else: return None

OUT = process_input(journalMaxVMPeak,IN[0])