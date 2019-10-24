import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalIsInPlaybackMode(journal):
	if journal.__repr__() == 'Journal': return journal.IsInPlaybackMode()
	else: return False

OUT = process_input(journalIsInPlaybackMode,IN[0])