import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsVMUsed(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.VMUsed
	else: return None

OUT = process_input(journalMemoryMetricsVMUsed,IN[0])