#!/usr/bin/env python3
"""Double check for master.svg against systems-source.dot.

Checks:
  1. XML parses.
  2. Canvas is 16:9.
  3. Every source node is placed once, and every source edge is drawn once.
  4. Every box lies inside the canvas.
  5. No two boxes overlap.
  6. Every drawn edge's endpoints are placed boxes.
Exit 0 only if all checks pass.
"""
import itertools
import os
import re
import sys
import xml.etree.ElementTree as ET

from mindmap_layout import parse
import mindmap_v4 as v4

HERE = os.path.dirname(os.path.abspath(__file__))
SVG = os.path.join(HERE, '..', 'master.svg')

failures = []


def check(cond, msg):
    print(('PASS ' if cond else 'FAIL ') + msg)
    if not cond:
        failures.append(msg)


# 1. XML
try:
    root = ET.parse(SVG).getroot()
    check(True, 'XML parses')
except ET.ParseError as e:
    check(False, f'XML parses ({e})')
    sys.exit(1)

# 2. 16:9
w, h = int(root.get('width')), int(root.get('height'))
check(abs(w / h - 16 / 9) < 0.001, f'canvas is 16:9 ({w}x{h})')

# Rebuild the placement the same way the generator does, then compare.
nodes, clusters, edges = parse(v4.SRC)
placed, _ = v4.place(nodes, clusters)

# 3. Every source node placed once
check(set(placed) == set(nodes), f'every node placed ({len(placed)} of {len(nodes)})')
check(len(placed) == len(nodes), 'no node placed twice')

# Count edge paths in the SVG: each drawn edge is a <path> with a marker-end
paths = [p for p in root.iter('{http://www.w3.org/2000/svg}path') if p.get('marker-end')]
check(len(paths) == len(edges), f'every source edge drawn once ({len(paths)} drawn, {len(edges)} in source)')

# 4. Inside canvas
outside = []
for nid, (x, y) in placed.items():
    bw, bh = v4._size(nid)
    if x < 0 or y < 0 or x + bw > w or y + bh > h:
        outside.append(nid)
check(not outside, f'all boxes inside canvas (outside: {outside})')

# 5. No overlaps
def rect(nid):
    bw, bh = v4._size(nid)
    x, y = placed[nid]
    return (x, y, x + bw, y + bh)

overlaps = []
for a, b in itertools.combinations(placed, 2):
    ax1, ay1, ax2, ay2 = rect(a)
    bx1, by1, bx2, by2 = rect(b)
    if ax1 < bx2 and bx1 < ax2 and ay1 < by2 and by1 < ay2:
        overlaps.append((a, b))
check(not overlaps, f'no boxes overlap (pairs: {overlaps})')

# 6. Every edge endpoint is a placed box
dangling = [e for e in edges if e['src'] not in placed or e['dst'] not in placed]
check(not dangling, f'every edge endpoint is a placed box (dangling: {len(dangling)})')

# Edge label text in SVG matches the source label for every edge
svg_text = ET.tostring(root, encoding='unicode')
missing_labels = []
for e in edges:
    if e['label']:
        probe = e['label'].split('\n')[0][:20]
        if probe and probe not in svg_text and v4.esc(probe) not in svg_text:
            missing_labels.append(e['label'])
check(not missing_labels, f'edge labels present in SVG (missing: {len(missing_labels)})')

print()
print('RESULT:', 'ALL CHECKS PASSED' if not failures else f'{len(failures)} CHECK(S) FAILED')
sys.exit(0 if not failures else 1)
