import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsVMAvailable(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.VMAvailable
	else: return None

OUT = process_input(journalMemoryMetricsVMAvailable,IN[0])