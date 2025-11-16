import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalSysInfoItem(jsysinfo):
	if hasattr(jsysinfo, 'SystemInformationType'): return jsysinfo.ItemNumber
	else: return None

OUT = process_input(journalSysInfoItem,IN[0])