data = [(x.Start, x.End, x) for x in IN[0]]
# sort by end first, then by start
data.sort(key=lambda x: x[1])
data.sort(key=lambda x: x[0])
l = len(data)
overlaps = []
for i in range(l):
	for j in range(i+1, l):
		x = data[i]
		y = data[j]
		# identify overlaps
		if (y[0]<=x[1]): overlaps.append([x[2], y[2]])

OUT = overlaps