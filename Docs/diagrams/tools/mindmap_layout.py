#!/usr/bin/env python3
"""Mind-map layout for the Sol systems diagram.

Reads node and edge definitions from systems-source.dot (the same source the
Graphviz variants use), then places them by hand:

  - One band per organizer. The organizer (hub) sits in a left column.
  - Its components fan out to the right in a grid, COLS per row.
  - Input nodes sit in their own left band.
  - Edges are drawn as curves between the hub and component boxes.

Output: master.svg (overwrites the Docs/diagrams/master.svg file).
Run:   python3 mindmap_layout.py
"""
import html
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'systems-source.dot')
OUT = os.path.join(HERE, '..', 'master-bands.svg')

BOX_W = 300
BOX_H = 92
COL_GAP = 40
ROW_GAP = 34
COLS = 3
HUB_W = 250
BAND_PAD = 26
BAND_GAP = 46
LEFT = 40
TOP = 120

LABEL_RE = re.compile(r'label=<(.*)>\s*(?:,|\])', re.S)


def parse(path):
    text = open(path).read()
    nodes = {}
    clusters = []
    cur = None
    for line in text.split('\n'):
        s = line.strip()
        m = re.match(r'subgraph (cluster_\w+) \{', s)
        if m:
            cur = {'id': m.group(1), 'nodes': [], 'label': ''}
            clusters.append(cur)
            continue
        if s == '}' and cur is not None:
            cur = None
            continue
        if cur is not None and s.startswith('label=<'):
            cur['label'] = s
        nm = re.match(r'(\w+)\s+\[(.*)\];?$', s)
        if nm and not s.startswith(('node ', 'edge ', 'graph ', 'label')):
            node_id, attrs = nm.group(1), nm.group(2)
            nodes[node_id] = {'id': node_id, 'attrs': attrs}
            if cur is not None:
                cur['nodes'].append(node_id)
    edges = []
    for line in text.split('\n'):
        em = re.match(r'\s*(\w+)\s*->\s*(\w+)\s*\[?(.*?)\]?;?\s*$', line)
        if em and em.group(1) in nodes and em.group(2) in nodes:
            attrs = em.group(3)
            lab = re.search(r'label="([^"]*)"', attrs)
            edges.append({
                'src': em.group(1),
                'dst': em.group(2),
                'label': lab.group(1) if lab else '',
                'dashed': 'dashed' in attrs,
                'dotted': 'dotted' in attrs,
                'back': 'dir=back' in attrs,
            })
    return nodes, clusters, edges


def title_and_sub(attrs):
    """Pull the first bold line and the small subtitle out of an HTML label."""
    lab = re.search(r'label=<(.*)>', attrs, re.S)
    body = lab.group(1) if lab else attrs
    bold = re.findall(r'<B>(.*?)</B>', body, re.S)
    small = re.findall(r'<FONT POINT-SIZE="9">(.*?)</FONT>', body, re.S)
    title = re.sub(r'<[^>]+>', '', bold[0]) if bold else ''
    sub = re.sub(r'<[^>]+>', '', small[0]) if small else ''
    return html.unescape(title.strip()), html.unescape(sub.strip())


def shape_of(attrs):
    if 'shape=cylinder' in attrs:
        return 'cylinder'
    if 'shape=note' in attrs:
        return 'note'
    return 'box'


def fill_of(attrs):
    m = re.search(r'fillcolor="([^"]+)"', attrs)
    return m.group(1) if m else '#ffffff'


def stroke_of(attrs):
    m = re.search(r'color="([^"]+)"', attrs)
    return m.group(1) if m else '#555555'


def dashed_box(attrs):
    return 'dashed' in attrs


def esc(s):
    return html.escape(s, quote=True)


def wrap(text, width=44):
    words, lines, cur = text.split(), [], ''
    for w in words:
        if len(cur) + len(w) + 1 > width and cur:
            lines.append(cur)
            cur = w
        else:
            cur = (cur + ' ' + w).strip()
    if cur:
        lines.append(cur)
    return lines


def box_svg(x, y, w, h, title, sub, fill, stroke, shape, dashed):
    dash = ' stroke-dasharray="5 4"' if dashed else ''
    out = []
    if shape == 'cylinder':
        out.append(f'<path d="M{x},{y+12} a{w/2},10 0 0 1 {w},0 v{h-24} a{w/2},10 0 0 1 {-w},0 z" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
        out.append(f'<ellipse cx="{x+w/2}" cy="{y+12}" rx="{w/2}" ry="10" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
    elif shape == 'note':
        out.append(f'<path d="M{x},{y} h{w-14} l14,14 v{h} h{-w} z" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
    else:
        out.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="8" fill="{fill}" stroke="{stroke}" stroke-width="1.6"{dash}/>')
    out.append(f'<text x="{x+12}" y="{y+22}" font-size="14" font-weight="bold" fill="#1f2933">{esc(title)}</text>')
    yy = y + 42
    for line in wrap(sub, 46)[:3]:
        out.append(f'<text x="{x+12}" y="{yy}" font-size="11" fill="#3e4c59">{esc(line)}</text>')
        yy += 15
    return '\n'.join(out)


def grid_positions(n, x0, y0):
    """Place n component boxes in COLS-wide rows starting at (x0, y0)."""
    pos = []
    for i in range(n):
        r, c = divmod(i, COLS)
        pos.append((x0 + c * (BOX_W + COL_GAP), y0 + r * (BOX_H + ROW_GAP)))
    return pos


def band_height(n_items):
    rows = max(1, (n_items + COLS - 1) // COLS)
    return rows * BOX_H + (rows - 1) * ROW_GAP


def build():
    nodes, clusters, edges = parse(SRC)
    placed = {}
    bands = []
    y = TOP

    # Input band first (its own band, no hub).
    input_cluster = next((c for c in clusters if c['id'] == 'cluster_input'), None)
    if input_cluster:
        ids = input_cluster['nodes']
        h = band_height(len(ids))
        title, sub = title_and_sub(input_cluster['label'])
        bands.append({'title': 'INPUT', 'sub': 'PlayerInput.inputactions', 'y': y, 'h': h,
                      'fill': '#e3f2fd', 'stroke': '#1565c0', 'hub': None})
        for (nid, (px, py)) in zip(ids, grid_positions(len(ids), LEFT + HUB_W + 80, y + 14)):
            placed[nid] = (px, py)
        y += h + BAND_PAD + BAND_GAP

    # One band per organizer. Hub = first node in the cluster that matches
    # the organizer name; the rest are components.
    for c in clusters:
        if c['id'] == 'cluster_input':
            continue
        ids = c['nodes']
        if not ids:
            continue
        title, sub = title_and_sub(c['label'])
        # The organizer hub is the first node listed in the cluster.
        hub_id, comps = ids[0], ids[1:]
        rows = max(1, (len(comps) + COLS - 1) // COLS)
        h = max(BOX_H + 30, rows * BOX_H + (rows - 1) * ROW_GAP + 30)
        bands.append({'title': title, 'sub': sub, 'y': y, 'h': h,
                      'fill': _cluster_fill(c), 'stroke': _cluster_stroke(c),
                      'hub': hub_id})
        placed[hub_id] = (LEFT + 14, y + (h - BOX_H) / 2)
        for (nid, (px, py)) in zip(comps, grid_positions(len(comps), LEFT + HUB_W + 80, y + 15)):
            placed[nid] = (px, py)
        y += h + BAND_PAD + BAND_GAP

    return nodes, edges, placed, bands, y


def _cluster_fill(c):
    m = re.search(r'fillcolor="([^"]+)"', c['label'])
    if m:
        return m.group(1)
    return '#f5f5f5'


def _cluster_stroke(c):
    m = re.search(r'color="([^"]+)"', c['label'])
    if m:
        return m.group(1)
    return '#888888'


def render(nodes, edges, placed, bands, bottom):
    width = LEFT + HUB_W + 80 + 3 * BOX_W + 2 * COL_GAP + 60
    height = bottom + 60
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" font-family="Helvetica, Arial, sans-serif">',
           '<rect width="100%" height="100%" fill="#ffffff"/>',
           '<text x="' + str(width / 2) + '" y="36" text-anchor="middle" font-size="22" font-weight="bold" fill="#1f2933">Sol: Systems Mind Map</text>',
           '<text x="' + str(width / 2) + '" y="60" text-anchor="middle" font-size="12" fill="#616e7c">Each band is one organizer. Its hub is on the left; its components fan out to the right. Arrows show data and direction.</text>',
           '<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#666"/></marker></defs>']

    # Bands
    for b in bands:
        out.append(f'<rect x="{LEFT - 14}" y="{b["y"]}" width="{width - LEFT - 26 + 14}" height="{b["h"]}" rx="14" fill="{b["fill"]}" fill-opacity="0.45" stroke="{b["stroke"]}" stroke-width="1.6"/>')
        out.append(f'<text x="{LEFT + 2}" y="{b["y"] - 6}" font-size="13" font-weight="bold" fill="#1f2933">{esc(b["title"])}</text>')
        if b['sub']:
            out.append(f'<text x="{LEFT + 2 + 8 * len(b["title"]) + 8}" y="{b["y"] - 6}" font-size="11" fill="#52606d">{esc(b["sub"])}</text>')

    # Edges first (so boxes sit on top)
    for e in edges:
        if e['src'] not in placed or e['dst'] not in placed:
            continue
        x1, y1 = _center(placed[e['src']])
        x2, y2 = _center(placed[e['dst']])
        if e['back']:
            x1, y1, x2, y2 = x2, y2, x1, y1
        dash = ' stroke-dasharray="5 4"' if e['dashed'] else (' stroke-dasharray="1 3"' if e['dotted'] else '')
        mid_x = (x1 + x2) / 2
        d = f'M{x1},{y1} C{mid_x},{y1} {mid_x},{y2} {x2},{y2}'
        out.append(f'<path d="{d}" fill="none" stroke="#666" stroke-width="1.1"{dash} marker-end="url(#arr)"/>')
        if e['label']:
            out.append(f'<text x="{mid_x}" y="{(y1 + y2) / 2 - 3}" font-size="9" text-anchor="middle" fill="#52606d">{esc(e["label"])}</text>')

    # Boxes
    for nid, (px, py) in placed.items():
        n = nodes[nid]
        title, sub = title_and_sub(n['attrs'])
        out.append(box_svg(px, py, BOX_W, BOX_H, title, sub, fill_of(n['attrs']), stroke_of(n['attrs']), shape_of(n['attrs']), dashed_box(n['attrs'])))

    out.append('</svg>')
    return '\n'.join(out) + '\n'


def _center(pos):
    return (pos[0] + BOX_W / 2, pos[1] + BOX_H / 2)


if __name__ == '__main__':
    nodes, edges, placed, bands, bottom = build()
    missing = [nid for nid in nodes if nid not in placed]
    svg = render(nodes, edges, placed, bands, bottom)
    with open(OUT, 'w') as f:
        f.write(svg)
    print(f'nodes placed: {len(placed)} of {len(nodes)}; edges: {len(edges)}; bands: {len(bands)}')
    if missing:
        print('not placed:', ', '.join(missing))
    sys.exit(0)
