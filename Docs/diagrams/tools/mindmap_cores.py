#!/usr/bin/env python3
"""Multi-core mind map for the Sol systems diagram.

Every organizer is a core. Its components sit in a ring around it. Cores are
packed in a grid across the whole canvas, so the page is used evenly.
Data arrows run between boxes, including across cores.

Reads:  systems-source.dot (nodes, edges, organizer clusters)
Writes: Docs/diagrams/master.svg

Run:    python3 mindmap_cores.py
"""
import html
import math
import os
import re
import sys

from mindmap_layout import parse, title_and_sub, shape_of, fill_of, stroke_of, dashed_box, esc, wrap

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, 'systems-source.dot')
OUT = os.path.join(HERE, '..', 'master.svg')

BOX_W = 230
BOX_H = 104
CORE_W = 250
CORE_H = 120
RING_RX = 360
RING_RY = 230
CELL_W = 1000
CELL_H = 860
MARGIN = 90
COLS = 3



def box_svg(x, y, w, h, nid_attrs):
    title, sub = title_and_sub(nid_attrs)
    fill, stroke = fill_of(nid_attrs), stroke_of(nid_attrs)
    shape = shape_of(nid_attrs)
    dash = ' stroke-dasharray="5 4"' if dashed_box(nid_attrs) else ''
    out = []
    if shape == 'cylinder':
        out.append(f'<rect x="{x}" y="{y+10}" width="{w}" height="{h-20}" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
        out.append(f'<ellipse cx="{x+w/2}" cy="{y+10}" rx="{w/2}" ry="10" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
        out.append(f'<ellipse cx="{x+w/2}" cy="{y+h-10}" rx="{w/2}" ry="10" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
    elif shape == 'note':
        out.append(f'<path d="M{x},{y} H{x+w-16} L{x+w},{y+16} V{y+h} H{x} Z" fill="{fill}" stroke="{stroke}" stroke-width="1.6"/>')
    else:
        out.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="9" fill="{fill}" stroke="{stroke}" stroke-width="1.6"{dash}/>')
    out.append(f'<text x="{x+12}" y="{y+22}" font-size="13" font-weight="bold" fill="#1f2933">{esc(title)}</text>')
    yy = y + 42
    for line in wrap(sub, 34)[:4]:
        out.append(f'<text x="{x+12}" y="{yy}" font-size="10.5" fill="#3e4c59">{esc(line)}</text>')
        yy += 14
    return '\n'.join(out)


def core_layout(cores):
    """Assign each core a grid cell (row-major), and place its components on a ring."""
    n = len(cores)
    rows = math.ceil(n / COLS)
    width = MARGIN * 2 + COLS * CELL_W
    height = 220 + rows * CELL_H + MARGIN
    placed = {}
    cell_of = {}
    for idx, (core_id, comps) in enumerate(cores):
        r, c = divmod(idx, COLS)
        cx = MARGIN + c * CELL_W + CELL_W / 2
        cy = 200 + r * CELL_H + CELL_H / 2
        placed[core_id] = (cx - CORE_W / 2, cy - CORE_H / 2)
        cell_of[core_id] = (cx, cy)
        k = len(comps)
        for j, comp in enumerate(comps):
            ang = -math.pi / 2 + 2 * math.pi * j / max(k, 1)
            px = cx + RING_RX * math.cos(ang) - BOX_W / 2
            py = cy + RING_RY * math.sin(ang) - BOX_H / 2
            placed[comp] = (px, py)
    return placed, cell_of, width, height


def build_cores(nodes, clusters):
    """Each organizer cluster becomes a core. Its first node is the hub; the rest are its components."""
    cores = []
    seen = set()
    for c in clusters:
        ids = [i for i in c['nodes'] if i in nodes]
        if not ids or c['id'] == 'cluster_input':
            continue
        hub, comps = ids[0], ids[1:]
        cores.append((hub, comps))
        seen.update(ids)
    return cores, seen


def render(nodes, edges, clusters):
    cores, seen = build_cores(nodes, clusters)
    placed, cell_of, width, height = core_layout(cores)

    # Input nodes go in a strip above the grid, spread across the top.
    input_ids = next((c['nodes'] for c in clusters if c['id'] == 'cluster_input'), [])
    step = (width - 2 * MARGIN) / max(len(input_ids), 1)
    for k, nid in enumerate(input_ids):
        placed[nid] = (MARGIN + k * step + step / 2 - BOX_W / 2, 120)

    out = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" font-family="Helvetica, Arial, sans-serif">',
           '<rect width="100%" height="100%" fill="#ffffff"/>',
           f'<text x="{width/2}" y="38" text-anchor="middle" font-size="22" font-weight="bold" fill="#1f2933">Sol: Systems Mind Map</text>',
           f'<text x="{width/2}" y="62" text-anchor="middle" font-size="12" fill="#616e7c">Each core is an organizer. Its components orbit it. Arrows carry data; labels give type and direction. Dashed = read or poll, dotted = collider or scene.</text>',
           '<defs><marker id="arr" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="7" markerHeight="7" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#666"/></marker></defs>']

    # Core halos: one soft region per organizer, behind its boxes
    for core_id, comps in cores:
        cx, cy = cell_of[core_id]
        fill = fill_of(nodes[core_id]['attrs'])
        out.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{RING_RX + BOX_W/2 + 30}" ry="{RING_RY + BOX_H/2 + 30}" fill="{fill}" fill-opacity="0.35" stroke="none"/>')

    # Edges: drawn before boxes so boxes sit on top
    for e in edges:
        if e['src'] not in placed or e['dst'] not in placed:
            continue
        x1, y1 = _centre(placed[e['src']], e['src'], cores)
        x2, y2 = _centre(placed[e['dst']], e['dst'], cores)
        if e['back']:
            x1, y1, x2, y2 = x2, y2, x1, y1
        dash = ' stroke-dasharray="5 4"' if e['dashed'] else (' stroke-dasharray="1 3"' if e['dotted'] else '')
        mx, my = (x1 + x2) / 2, (y1 + y2) / 2
        # bend the curve away from the middle of the page so it reads as a link
        bend = 0.18 * math.hypot(x2 - x1, y2 - y1)
        nx, ny = (-(y2 - y1), x2 - x1)
        L = math.hypot(nx, ny) or 1
        cxp, cyp = mx + bend * nx / L, my + bend * ny / L
        out.append(f'<path d="M{x1},{y1} Q{cxp},{cyp} {x2},{y2}" fill="none" stroke="#666" stroke-width="1.1"{dash} marker-end="url(#arr)"/>')
        if e['label']:
            out.append(f'<text x="{cxp}" y="{cyp}" font-size="9" text-anchor="middle" fill="#3e4c59" paint-order="stroke" stroke="#ffffff" stroke-width="3">{esc(e["label"])}</text>')

    # Cores (hub boxes), drawn larger and bold
    for core_id, comps in cores:
        x, y = placed[core_id]
        out.append(box_svg(x, y, CORE_W, CORE_H, nodes[core_id]['attrs']))
    # Component boxes
    for core_id, comps in cores:
        for comp in comps:
            if comp in placed and comp in nodes:
                x, y = placed[comp]
                out.append(box_svg(x, y, BOX_W, BOX_H, nodes[comp]['attrs']))
    # Input boxes
    for nid in input_ids:
        x, y = placed[nid]
        out.append(box_svg(x, y, BOX_W, 84, nodes[nid]['attrs']))

    out.append('</svg>')
    return '\n'.join(out) + '\n'


def _centre(pos, nid, cores):
    w = CORE_W if any(nid == c for c, _ in cores) else BOX_W
    h = CORE_H if any(nid == c for c, _ in cores) else BOX_H
    return (pos[0] + w / 2, pos[1] + h / 2)


if __name__ == '__main__':
    nodes, edges, _, _, _ = None, None, None, None, None
    nodes, clusters, edges = parse(SRC)
    svg = render(nodes, edges, clusters)
    with open(OUT, 'w') as f:
        f.write(svg)
    cores, seen = build_cores(nodes, clusters)
    print(f'cores: {len(cores)}; nodes: {len(nodes)}; edges: {len(edges)}; output: {os.path.normpath(OUT)}')
    sys.exit(0)
