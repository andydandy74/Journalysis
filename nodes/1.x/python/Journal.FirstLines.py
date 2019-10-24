import clr

def process_input(func, input):
	if isinstance(input[0], list): 
		if isinstance(input[1], list): return [func(x, y) for x, y in zip(input[0], input[1])]
		else: return [func(x, input[1]) for x in input[0]]
	else: 
		if isinstance(input[1], list): return [func(input[0], x) for x in input[1]]
		else: return func(input[0], input[1])
	
def journalFirstLines(journal, num):
	if journal.__repr__() == 'Journal': return journal.GetFirstLines(num)
	else: return None

OUT = process_input(journalFirstLines,IN)