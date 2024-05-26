import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSysInfoType(jsysinfo):
	if hasattr(jsysinfo, 'SystemInformationType'): return jsysinfo.SystemInformationType
	else: return None

OUT = process_input(journalSysInfoType,IN[0])