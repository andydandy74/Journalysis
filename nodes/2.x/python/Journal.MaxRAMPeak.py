import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMaxRAMPeak(journal):
	if journal.__repr__() == 'Journal': return journal.GetMaxRAMPeak()
	else: return None

OUT = process_input(journalMaxRAMPeak,IN[0])