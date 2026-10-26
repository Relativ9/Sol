#!/usr/bin/env python3
"""Checks each drawn edge in master.svg connects the right boxes.

For every source edge (in order), the generator draws one <path>. Its start and
end points must sit on the source and destination boxes (or on the reverse, for
edges marked dir=back, where the arrow is drawn from the receiver).
"""
import os
import re
import sys
import xml.etree.ElementTree as ET

from mindmap_layout import parse
import mindmap_v4 as v4

HERE = os.path.dirname(os.path.abspath(__file__))
SVG = os.path.join(HERE, '..', 'master.svg')
NS = '{http://www.w3.org/2000/svg}'

nodes, clusters, edges = parse(v4.SRC)
placed, _ = v4.place(nodes, clusters)
root = ET.parse(SVG).getroot()
paths = [p for p in root.iter(NS + 'path') if p.get('marker-end')]

def on_box(nid, x, y, tol=2.0):
    bw, bh = v4._size(nid)
    px, py = placed[nid]
    return (px - tol <= x <= px + bw + tol) and (py - tol <= y <= py + bh + tol)

def endpoints(d):
    m = re.match(r'M([-\d.]+),([-\d.]+) Q[-\d.]+,[-\d.]+ ([-\d.]+),([-\d.]+)', d)
    x1, y1, x2, y2 = map(float, m.groups())
    return (x1, y1), (x2, y2)

bad = []
for e, p in zip(edges, paths):
    (x1, y1), (x2, y2) = endpoints(p.get('d'))
    src, dst = (e['dst'], e['src']) if e['back'] else (e['src'], e['dst'])
    ok = on_box(src, x1, y1, tol=1e9) and on_box(dst, x2, y2, tol=1e9)
    # Use the centre test: the path starts at the centre of its source box.
    sx, sy = v4._centre(src, placed)
    dx, dy = v4._centre(dst, placed)
    ok = abs(x1 - sx) < 1 and abs(y1 - sy) < 1 and abs(x2 - dx) < 1 and abs(y2 - dy) < 1
    if not ok:
        bad.append(f"{e['src']}->{e['dst']} ({'back' if e['back'] else 'fwd'})")

print(f'edges checked: {min(len(edges), len(paths))}')
print('mismatched endpoints:', bad if bad else 'none')
print('RESULT:', 'PASS' if not bad and len(edges) == len(paths) else 'FAIL')
sys.exit(0 if not bad and len(edges) == len(paths) else 1)
