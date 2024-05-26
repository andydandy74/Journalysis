import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSysInfoKey(jsysinfo):
	if hasattr(jsysinfo, 'SystemInformationType'): return jsysinfo.Key
	else: return None

OUT = process_input(journalSysInfoKey,IN[0])