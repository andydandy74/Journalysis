if IN[0].__repr__() == 'KeyboardShortcuts':
	if isinstance(IN[1], list): OUT = [IN[0].GetCommandById(x) for x in IN[1]]
	else: OUT = IN[0].GetCommandById(IN[1])
else: OUT = None