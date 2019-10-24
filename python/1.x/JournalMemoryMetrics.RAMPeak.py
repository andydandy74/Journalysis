import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsRAMPeak(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.RAMPeak
	else: return None

OUT = process_input(journalMemoryMetricsRAMPeak,IN[0])