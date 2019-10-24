import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalAddinEventMessageText(jaddinevent):
	if jaddinevent.__repr__() == 'JournalAddinEvent': return jaddinevent.MessageText
	else: return None

OUT = process_input(journalAddinEventMessageText,IN[0])