import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalPlaybackInterrupted(journal):
	if journal.__repr__() == 'Journal': return journal.WasPlaybackInterrupted()
	else: return False

OUT = process_input(journalPlaybackInterrupted,IN[0])