import clr

def process_input(func, input1, input2):
	if isinstance(input, list): return [func(x, input2) for x in input1]
	else: return func(input1, input2)
	
def journalStripComments(journal, preserveTimestamps):
	if journal.__repr__() == 'Journal': return journal.StripComments(preserveTimestamps)
	else: return None

OUT = process_input(journalStripComments,IN[0],IN[1])