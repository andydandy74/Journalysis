import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSysInfoValue(jsysinfo):
	if hasattr(jsysinfo, 'SystemInformationType'): return jsysinfo.Value
	else: return None

OUT = process_input(journalSysInfoValue,IN[0])