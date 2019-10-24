import clr

def process_input(func, input):
	if isinstance(input, list): return [func(x) for x in input]
	else: return func(input)
	
def journalMemoryMetricsVMPeak(jmm):
	if jmm.__repr__() == 'JournalMemoryMetrics': return jmm.VMPeak
	else: return None

OUT = process_input(journalMemoryMetricsVMPeak,IN[0])