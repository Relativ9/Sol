#!/usr/bin/env python3
"""Sol systems mind map, version 4.

Multi-core layout. Each organizer is a core; its components orbit it. Cores
sit in a 3 x 3 grid on a 16:9 canvas. Data arrows run between boxes, including
across cores. Colour codes the layer (input, player, physics, stats, progression,
persistence, time). Node text is loaded from descriptions.json.

Reads:  systems-source.dot (nodes, edges), descriptions.json (detail text)
Writes: ../master.svg (16:9)

Run:    python3 mindmap_v4.py
"""
import json
import math
import os
import re
import sys

from mindmap_layout import parse, esc, wrap

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'systems-source.dot')
DESC = os.path.join(HERE, 'descriptions.json')
OUT = os.path.join(HERE, '..', 'master.svg')

# 16:9 canvas
CANVAS_W = 3200
CANVAS_H = 1800

# Hub and component boxes
HUB_W, HUB_H = 300, 150
BOX_W, BOX_H = 250, 132

# Grid: 3 columns x 3 rows of cores
COLS, ROWS = 3, 3
TOP = 150
BOTTOM = 40
SIDE = 60
CELL_W = (CANVAS_W - 2 * SIDE) / COLS
CELL_H = (CANVAS_H - TOP - BOTTOM) / ROWS
RING_RX = CELL_W / 2 - BOX_W / 2 - 40
RING_RY = CELL_H / 2 - BOX_H / 2 - 30

# Hub for each organizer cluster (named explicitly, not "first node")
HUBS = {
    'cluster_pc': 'PC',
    'cluster_cam': 'CAM',
    'cluster_combat': 'CSC',
    'cluster_phys': 'GRAV',
    'cluster_stats': 'SS',
    'cluster_ui': 'UIM',
    'cluster_talent': 'TTC',
    'cluster_save': 'SM',
    'cluster_time': 'TM',
}

# Cores that get a fixed two-column arrangement instead of a ring
ROW_LAYOUT = {'cluster_stats'}

# Colour by layer. Fill is the light tint, stroke is the border.


def load_descriptions():
    d = {}
    for name in ('descriptions.json', 'descriptions_b.json'):
        with open(os.path.join(HERE, name)) as f:
            d.update(json.load(f))
    return d


def node_layer(nid, cluster_of):
    cid = cluster_of.get(nid)
    if nid.startswith('IN_'):
        return 'input'
    return CLUSTER_LAYER.get(cid, 'input')


def core_positions(cluster_list):
    """Place each hub in its grid cell. Returns {hub_id: (cx, cy)}."""
    cells = {}
    order = [
        'cluster_pc', 'cluster_stats', 'cluster_talent',
        'cluster_cam', 'cluster_combat', 'cluster_phys',
        'cluster_ui', 'cluster_save', 'cluster_time',
    ]
    for idx, cid in enumerate(order):
        r, c = divmod(idx, COLS)
        cx = SIDE + c * CELL_W + CELL_W / 2
        cy = TOP + r * CELL_H + CELL_H / 2
        cells[cid] = (cx, cy)
    return cells, order


def place(nodes, clusters):
    cluster_of = {}
    members = {}
    for c in clusters:
        members[c['id']] = [i for i in c['nodes'] if i in nodes]
        for i in members[c['id']]:
            cluster_of[i] = c['id']
    cells, order = core_positions(clusters)
    placed = {}
    for cid in order:
        hub = HUBS[cid]
        cx, cy = cells[cid]
        placed[hub] = (cx - HUB_W / 2, cy - HUB_H / 2)
        comps = [i for i in members[cid] if i != hub]
        if cid in ROW_LAYOUT:
            # Fixed two-column arrangement for crowded cores (no ring overlap)
            # Left column ends before the hub's left edge; right column starts after its right edge.
            left_x = cx - HUB_W / 2 - 40 - BOX_W
            right_x = cx + HUB_W / 2 + 40
            half = (len(comps) + 1) // 2
            row_gap = BOX_H + 26
            top_y = cy - (half - 1) * row_gap / 2 - BOX_H / 2
            for j, comp in enumerate(comps):
                if j < half:
                    px, py = left_x, top_y + j * row_gap
                else:
                    px, py = right_x, top_y + (j - half) * row_gap
                placed[comp] = (px, py)
            continue
        k = len(comps)
        for j, comp in enumerate(comps):
            ang = -math.pi / 2 + 2 * math.pi * j / max(k, 1)
            px = cx + RING_RX * math.cos(ang) - BOX_W / 2
            py = cy + RING_RY * math.sin(ang) - BOX_H / 2
            placed[comp] = (px, py)
    # Input nodes sit in a strip above the grid
    inputs = [i for i in nodes if i.startswith('IN_')]
    step = (CANVAS_W - 2 * SIDE) / max(len(inputs), 1)
    for k, nid in enumerate(inputs):
        placed[nid] = (SIDE + k * step + step / 2 - BOX_W / 2, 24)
    return placed, cluster_of


def _size(nid):
    return (HUB_W, HUB_H) if nid in HUBS.values() else (BOX_W, BOX_H)


def _centre(nid, placed):
    w, h = _size(nid)
    x, y = placed[nid]
    return (x + w / 2, y + h / 2)

LAYER = {
    'input':   ('#e3f2fd', '#1565c0'),
    'player':  ('#fdecea', '#c62828'),
    'camera':  ('#f3e8ff', '#7e3ff2'),
    'combat':  ('#fff1f0', '#b5483c'),
    'physics': ('#eceff1', '#455a64'),
    'stats':   ('#fff3e0', '#e65100'),
    'talent':  ('#e0f7fa', '#00838f'),
    'save':    ('#ede7f6', '#4527a0'),
    'time':    ('#e8eaf6', '#283593'),
}
CLUSTER_LAYER = {
    'cluster_pc': 'player', 'cluster_cam': 'camera', 'cluster_combat': 'combat',
    'cluster_phys': 'physics', 'cluster_stats': 'stats', 'cluster_ui': 'talent',
    'cluster_talent': 'talent', 'cluster_save': 'save', 'cluster_time': 'time',
}


def wrap_lines(text, width):
    return wrap(text, width)


def box(nid, x, y, w, h, desc, layer, hub):
    fill, stroke = LAYER[layer]
    shape = 'box'
    if nid in ('PCSTATE', 'PCSVC', 'RB'):
        shape = 'cylinder'
    if nid in ('TEV',):
        shape = 'note'
    dashed = nid in ('AEH', 'GL', 'CINE')
    dash = ' stroke-dasharray="6 4"' if dashed else ''
    sw = 2.2 if hub else 1.4
    parts = []
    if shape == 'cylinder':
        parts.append(f'<rect x="{x}" y="{y+10}" width="{w}" height="{h-20}" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
        parts.append(f'<ellipse cx="{x+w/2}" cy="{y+10}" rx="{w/2}" ry="10" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
        parts.append(f'<ellipse cx="{x+w/2}" cy="{y+h-10}" rx="{w/2}" ry="10" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
    elif shape == 'note':
        parts.append(f'<path d="M{x},{y} H{x+w-16} L{x+w},{y+16} V{y+h} H{x} Z" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"/>')
    else:
        parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="10" fill="{fill}" stroke="{stroke}" stroke-width="{sw}"{dash}/>')
    title = desc['what']
    parts.append(f'<text x="{x+12}" y="{y+24}" font-size="{16 if hub else 14}" font-weight="bold" fill="#1f2933">{esc(title)}</text>')
    yy = y + 44
    width_chars = 52 if hub else 38
    max_lines = 6 if hub else 5
    for line in wrap_lines(desc['detail'], width_chars)[:max_lines]:
        parts.append(f'<text x="{x+12}" y="{yy}" font-size="11" fill="#2d3a45">{esc(line)}</text>')
        yy += 14
    if not desc.get('verified', True):
        parts.append(f'<text x="{x+w-10}" y="{y+h-8}" font-size="10" text-anchor="end" fill="#b28704">unverified</text>')
    return '\n'.join(parts)


def curve(x1, y1, x2, y2, dashed, dotted, back, label):
    if back:
        x1, y1, x2, y2 = x2, y2, x1, y1
    dash = ' stroke-dasharray="6 4"' if dashed else (' stroke-dasharray="1 4"' if dotted else '')
    mx, my = (x1 + x2) / 2, (y1 + y2) / 2
    dist = math.hypot(x2 - x1, y2 - y1) or 1
    nx, ny = -(y2 - y1) / dist, (x2 - x1) / dist
    bend = 0.16 * dist
    cx, cy = mx + nx * bend, my + ny * bend
    path = f'<path d="M{x1:.1f},{y1:.1f} Q{cx:.1f},{cy:.1f} {x2:.1f},{y2:.1f}" fill="none" stroke="#5a6470" stroke-width="1.2"{dash} marker-end="url(#arr)"/>'
    lab = ''
    if label:
        lab = f'<text x="{cx:.1f}" y="{cy:.1f}" font-size="10" text-anchor="middle" fill="#2d3a45" stroke="#ffffff" stroke-width="3" paint-order="stroke">{esc(label)}</text>'
    return path, lab


def render(nodes, edges, clusters, desc):
    placed, cluster_of = place(nodes, clusters)
    out = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{CANVAS_W}" height="{CANVAS_H}" viewBox="0 0 {CANVAS_W} {CANVAS_H}" font-family="Helvetica, Arial, sans-serif">',
        '<rect width="100%" height="100%" fill="#ffffff"/>',
        f'<text x="{CANVAS_W/2}" y="{CANVAS_H-18}" text-anchor="middle" font-size="12" fill="#616e7c">Sol systems. Each core is an organizer; its components orbit it. Arrows show data and direction. Dashed = read or poll. Dotted = collider or scene. Unverified items are marked.</text>',
        '<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#5a6470"/></marker></defs>',
    ]
    # Halos behind each core
    for cid in HUBS:
        hub = HUBS[cid]
        cx, cy = _centre(hub, placed)
        layer = CLUSTER_LAYER[cid]
        fill, _ = LAYER[layer]
        out.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{RING_RX + BOX_W/2 + 24}" ry="{RING_RY + BOX_H/2 + 24}" fill="{fill}" fill-opacity="0.55" stroke="none"/>')

    # Edges
    labels = []
    for e in edges:
        if e['src'] not in placed or e['dst'] not in placed:
            continue
        x1, y1 = _centre(e['src'], placed)
        x2, y2 = _centre(e['dst'], placed)
        p, l = curve(x1, y1, x2, y2, e['dashed'], e['dotted'], e['back'], e['label'])
        out.append(p)
        labels.append(l)
    out += labels

    # Boxes
    hubs = set(HUBS.values())
    for nid, (x, y) in placed.items():
        d = desc.get(nid, {'what': nid, 'detail': '', 'verified': False})
        w, h = _size(nid)
        layer = node_layer(nid, cluster_of)
        out.append(box(nid, x, y, w, h, d, layer, nid in hubs))

    out.append('</svg>')
    return '\n'.join(out) + '\n'


if __name__ == '__main__':
    nodes, clusters, edges = parse(SRC)
    desc = load_descriptions()
    svg = render(nodes, edges, clusters, desc)
    with open(OUT, 'w') as f:
        f.write(svg)
    print(f'nodes: {len(nodes)}, edges: {len(edges)}, described: {len(desc)}, canvas: {CANVAS_W}x{CANVAS_H}')
    sys.exit(0)
