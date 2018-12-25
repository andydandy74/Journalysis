import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsRAMUsed(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.RAMUsed
	else: return None

OUT = process_input(journalMemoryMetricsRAMUsed,IN[0])