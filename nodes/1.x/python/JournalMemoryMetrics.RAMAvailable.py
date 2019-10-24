import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsRAMAvailable(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.RAMAvailable
	else: return None

OUT = process_input(journalMemoryMetricsRAMAvailable,IN[0])