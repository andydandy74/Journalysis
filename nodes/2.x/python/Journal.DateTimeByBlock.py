import clr

OUT = []
if IN[0].__repr__() == 'Journal':
	if isinstance(IN[1], list): OUT = IN[0].GetDateTimeByBlocks(IN[1])
	else: OUT = IN[0].GetDateTimeByBlock(IN[1])