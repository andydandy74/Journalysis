import clr

OUT = []
if IN[0].__repr__() == 'Journal':
	if isinstance(IN[1], list): OUT = IN[0].GetLinesByBlocks(IN[1])
	else: OUT = IN[0].GetLinesByBlock(IN[1])