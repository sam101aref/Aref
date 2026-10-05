// Battle and cutscene backdrops: three horizontally tileable layers per biome (far, mid, ground).
// 100 SVG units = 1 Unity unit, y down, the battle ground line at y = 0. The sky is drawn by the
// game as a gradient, and day / dusk / night are tints applied at runtime.

import { O, OT, shade, mix, rng, asset } from './lib.mjs';

const LAYERS = {
  // width (units), top (units above ground), bottom (units below ground), pixels per unit
  far: { w: 40, top: 11, bottom: 1, ppu: 32 },
  mid: { w: 30, top: 9, bottom: 1, ppu: 48 },
  ground: { w: 16, top: 0.8, bottom: 6, ppu: 64 },
};

/** A periodic ridge line across width W: base y minus a sum of sines with whole periods. */
function ridge(W, base, amps, seed, step = 20) {
  const r = rng(seed);
  const waves = amps.map((a, i) => ({ a, k: i + 1 + Math.floor(r() * 2), p: r() * Math.PI * 2 }));
  const pts = [];
  for (let x = 0; x <= W; x += step) {
    let y = base;
    for (const w of waves)
      y -= w.a * (0.5 + 0.5 * Math.sin((x / W) * Math.PI * 2 * w.k + w.p));
    pts.push([x, y]);
  }
  return pts;
}

function ridgePath(pts, bottom) {
  const W = pts[pts.length - 1][0];
  return `M0,${bottom} ` + pts.map(([x, y]) => `L${x.toFixed(1)},${y.toFixed(1)}`).join(' ') + ` L${W},${bottom} Z`;
}

/** Peaky mountain range: triangular peaks with snow caps, periodic over W. */
function peaks(W, count, base, minH, maxH, color, snow, seed, outline = false) {
  const r = rng(seed);
  let s = '';
  for (let i = 0; i < count; i++) {
    const x = (i + r() * 0.6) * (W / count);
    const h = minH + r() * (maxH - minH);
    const w = h * (1.1 + r() * 0.6);
    const lx = x - w / 2;
    const rx = x + w / 2;
    const sx = x + (r() - 0.5) * w * 0.15;
    s += `<path d="M${lx},${base} L${sx - w * 0.08},${base - h * 0.92} L${sx},${base - h} L${sx + w * 0.1},${base - h * 0.9} L${rx},${base} Z" fill="${color}" ${outline ? OT : ''}/>`;
    s += `<path d="M${sx},${base - h} L${sx + w * 0.1},${base - h * 0.9} L${rx},${base} L${sx + w * 0.05},${base} Z" fill="${shade(color, -0.12)}"/>`;
    if (snow) {
      const sh = h * 0.28;
      s += `<path d="M${sx - w * 0.14},${base - h + sh} L${sx - w * 0.08},${base - h * 0.92} L${sx},${base - h} L${sx + w * 0.1},${base - h * 0.9} L${sx + w * 0.17},${base - h + sh} L${sx + w * 0.08},${base - h + sh * 0.7} L${sx + w * 0.02},${base - h + sh * 1.1} L${sx - w * 0.06},${base - h + sh * 0.75} Z" fill="${snow}"/>`;
    }
  }
  return s;
}

/** Repeats content at -W and +W so anything crossing an edge tiles seamlessly. */
function tile(W, content) {
  return `<g transform="translate(${-W} 0)">${content}</g>${content}<g transform="translate(${W} 0)">${content}</g>`;
}

// ------------------------------------------------------------------ props used inside backdrops

function poplar(x, y, h, color) {
  return `<path d="M${x - 2},${y} L${x - 2},${y - h * 0.25} L${x + 2},${y - h * 0.25} L${x + 2},${y} Z" fill="#5a3e28"/>` +
    `<path d="M${x},${y - h} C${x + h * 0.12},${y - h * 0.8} ${x + h * 0.11},${y - h * 0.4} ${x},${y - h * 0.18} C${x - h * 0.11},${y - h * 0.4} ${x - h * 0.12},${y - h * 0.8} ${x},${y - h} Z" fill="${color}" ${OT}/>` +
    `<path d="M${x},${y - h * 0.9} C${x + h * 0.06},${y - h * 0.7} ${x + h * 0.06},${y - h * 0.45} ${x + 1},${y - h * 0.25}" fill="none" stroke="${shade(color, 0.2)}" stroke-width="3"/>`;
}

function house(x, y, w, h, wall, dome) {
  const roof = shade(wall, -0.15);
  let s = `<path d="M${x},${y} L${x},${y - h} L${x + w},${y - h} L${x + w},${y} Z" fill="${wall}" ${OT}/>`;
  s += `<path d="M${x - 4},${y - h} L${x + w + 4},${y - h} L${x + w + 4},${y - h - 8} L${x - 4},${y - h - 8} Z" fill="${roof}" ${OT}/>`;
  if (dome)
    s += `<path d="M${x + w * 0.2},${y - h - 8} C${x + w * 0.2},${y - h - w * 0.55} ${x + w * 0.8},${y - h - w * 0.55} ${x + w * 0.8},${y - h - 8} Z" fill="${dome}" ${OT}/>`;
  // Arched door and a window
  s += `<path d="M${x + w * 0.38},${y} L${x + w * 0.38},${y - h * 0.38} C${x + w * 0.38},${y - h * 0.55} ${x + w * 0.62},${y - h * 0.55} ${x + w * 0.62},${y - h * 0.38} L${x + w * 0.62},${y} Z" fill="#4a3020"/>`;
  s += `<path d="M${x + w * 0.12},${y - h * 0.62} L${x + w * 0.12},${y - h * 0.78} C${x + w * 0.12},${y - h * 0.86} ${x + w * 0.24},${y - h * 0.86} ${x + w * 0.24},${y - h * 0.78} L${x + w * 0.24},${y - h * 0.62} Z" fill="#4a3020"/>`;
  return s;
}

function windcatcher(x, y, h, wall) {
  let s = `<path d="M${x},${y} L${x},${y - h} L${x + 26},${y - h} L${x + 26},${y} Z" fill="${shade(wall, 0.05)}" ${OT}/>`;
  for (let i = 0; i < 3; i++)
    s += `<path d="M${x + 4 + i * 7},${y - h + 8} L${x + 4 + i * 7},${y - h + 30}" stroke="#4a3020" stroke-width="3"/>`;
  s += `<path d="M${x - 3},${y - h} L${x + 29},${y - h} L${x + 29},${y - h - 6} L${x - 3},${y - h - 6} Z" fill="${shade(wall, -0.15)}" ${OT}/>`;
  return s;
}

function broadTree(x, y, h, leaf, trunk = '#4a3426') {
  let s = `<path d="M${x - 5},${y} C${x - 4},${y - h * 0.3} ${x - 3},${y - h * 0.5} ${x - 2},${y - h * 0.6} L${x + 2},${y - h * 0.6} C${x + 3},${y - h * 0.5} ${x + 4},${y - h * 0.3} ${x + 5},${y} Z" fill="${trunk}" ${OT}/>`;
  const r = h * 0.28;
  s += `<circle cx="${x - r * 0.6}" cy="${y - h * 0.62}" r="${r * 0.8}" fill="${shade(leaf, -0.12)}" ${OT}/>`;
  s += `<circle cx="${x + r * 0.6}" cy="${y - h * 0.64}" r="${r * 0.85}" fill="${shade(leaf, -0.06)}" ${OT}/>`;
  s += `<circle cx="${x}" cy="${y - h * 0.8}" r="${r}" fill="${leaf}" ${OT}/>`;
  s += `<circle cx="${x - r * 0.3}" cy="${y - h * 0.86}" r="${r * 0.35}" fill="${shade(leaf, 0.15)}"/>`;
  return s;
}

function pine(x, y, h, color, snow) {
  let s = `<path d="M${x - 3},${y} L${x - 3},${y - h * 0.15} L${x + 3},${y - h * 0.15} L${x + 3},${y} Z" fill="#4a3426"/>`;
  for (let i = 0; i < 3; i++) {
    const t = y - h * 0.12 - i * h * 0.27;
    const w = h * (0.32 - i * 0.07);
    s += `<path d="M${x - w},${t} L${x},${t - h * 0.42} L${x + w},${t} Z" fill="${shade(color, i * 0.06)}" ${OT}/>`;
    if (snow)
      s += `<path d="M${x - w * 0.45},${t - h * 0.22} L${x},${t - h * 0.42} L${x + w * 0.45},${t - h * 0.22} L${x + w * 0.2},${t - h * 0.25} L${x},${t - h * 0.2} L${x - w * 0.2},${t - h * 0.25} Z" fill="${snow}"/>`;
  }
  return s;
}

function tent(x, y, w, h, a, b) {
  let s = `<path d="M${x - w / 2},${y} L${x},${y - h} L${x + w / 2},${y} Z" fill="${a}" ${OT}/>`;
  for (let i = 1; i < 4; i += 2)
    s += `<path d="M${x},${y - h} L${x - w / 2 + (w / 4) * i},${y} L${x - w / 2 + (w / 4) * (i + 1)},${y} Z" fill="${b}"/>`;
  s += `<path d="M${x - w / 2},${y} L${x},${y - h} L${x + w / 2},${y} Z" fill="none" ${OT}/>`;
  s += `<path d="M${x - 8},${y} L${x},${y - h * 0.45} L${x + 8},${y} Z" fill="#2a1a12"/>`;
  s += `<path d="M${x},${y - h} L${x},${y - h - 18} L${x + 14},${y - h - 13} L${x},${y - h - 8}" fill="${b}" ${OT}/>`;
  return s;
}

function rock(x, y, w, h, color) {
  return `<path d="M${x - w / 2},${y} L${x - w * 0.4},${y - h * 0.7} L${x - w * 0.1},${y - h} L${x + w * 0.3},${y - h * 0.85} L${x + w / 2},${y - h * 0.3} L${x + w / 2},${y} Z" fill="${color}" ${OT}/>` +
    `<path d="M${x - w * 0.1},${y - h} L${x + w * 0.3},${y - h * 0.85} L${x + w / 2},${y - h * 0.3} L${x + w / 2},${y} L${x + w * 0.1},${y} Z" fill="${shade(color, -0.15)}"/>`;
}

function grassTufts(W, y, color, count, seed, height = 14) {
  const r = rng(seed);
  let s = '';
  for (let i = 0; i < count; i++) {
    const x = r() * W;
    const h = height * (0.6 + r() * 0.8);
    s += `<path d="M${x - 6},${y + 2} L${x - 4},${y - h * 0.7} L${x - 1},${y} L${x + 1},${y - h} L${x + 3},${y} L${x + 6},${y - h * 0.6} L${x + 8},${y + 2} Z" fill="${color}"/>`;
  }
  return s;
}

function stones(W, y0, y1, color, count, seed) {
  const r = rng(seed);
  let s = '';
  for (let i = 0; i < count; i++) {
    const x = r() * W;
    const y = y0 + r() * (y1 - y0);
    const rx = 4 + r() * 9;
    s += `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${rx * 0.55}" fill="${color}"/>`;
  }
  return s;
}

/** The ground layer: a surface band, soil strata, tufts and stones. */
function groundLayer(W, c) {
  let s = '';
  const surface = ridge(W, 0, [6, 3], c.seed, 16).map(([x, y]) => [x, y + 4]);
  s += `<path d="${ridgePath(surface, 620)}" fill="${c.top}"/>`;
  s += `<path d="M0,26 L${W},26 L${W},620 L0,620 Z" fill="${c.soil}"/>`;
  s += `<path d="${ridgePath(ridge(W, 30, [10, 5], c.seed + 1, 32), 70)}" fill="${c.top}"/>`;
  s += `<path d="M0,180 L${W},180 L${W},620 L0,620 Z" fill="${shade(c.soil, -0.18)}"/>`;
  s += `<path d="${ridgePath(ridge(W, 186, [14, 8], c.seed + 2, 32), 230)}" fill="${c.soil}"/>`;
  s += `<path d="M0,380 L${W},380 L${W},620 L0,620 Z" fill="${shade(c.soil, -0.32)}"/>`;
  s += stones(W, 60, 560, shade(c.soil, -0.25), 26, c.seed + 3);
  s += stones(W, 40, 300, shade(c.soil, 0.12), 14, c.seed + 4);
  s += `<path d="${'M0,4 ' + surface.map(([x, y]) => `L${x.toFixed(1)},${y.toFixed(1)}`).join(' ')}" fill="none" stroke="${shade(c.top, -0.3)}" stroke-width="3"/>`;
  if (c.tuft)
    s += grassTufts(W, 4, c.tuft, c.tufts || 40, c.seed + 5, c.tuftHeight || 14);
  if (c.extra)
    s += c.extra(W);
  return s;
}

// ------------------------------------------------------------------ biomes

export const BIOMES = {
  village: {
    far(W) {
      return `<path d="${ridgePath(ridge(W * 100, -150, [180, 90, 40], 11), 100)}" fill="#a9bccb"/>` +
        peaks(W * 100, 7, -120, 420, 760, '#93aabd', '#f4f7fa', 12) +
        `<path d="${ridgePath(ridge(W * 100, -40, [120, 60], 13), 100)}" fill="#8fa48c"/>`;
    },
    mid(W) {
      const r = rng(21);
      let s = `<path d="${ridgePath(ridge(W * 100, -60, [40, 20], 22), 100)}" fill="#b9a46a"/>`;
      let x = 40;
      while (x < W * 100 - 120) {
        const kind = r();
        if (kind < 0.25)
          s += poplar(x, -40, 260 + r() * 140, '#4f7a3a');
        else if (kind < 0.35)
          s += windcatcher(x, -40, 170 + r() * 40, '#c99a63');
        else {
          const w = 110 + r() * 80;
          s += house(x, -40, w, 90 + r() * 60, mix('#c99a63', '#d8b07a', r()), r() < 0.4 ? '#b88550' : null);
          x += w - 40;
        }
        x += 70 + r() * 90;
      }
      s += `<path d="${ridgePath(ridge(W * 100, -10, [16, 8], 23), 100)}" fill="#c2a85a"/>`;
      return s;
    },
    ground: { top: '#a8854f', soil: '#8a6a40', tuft: '#7b8f3a', seed: 31 },
  },
  border: {
    far(W) {
      return `<path d="${ridgePath(ridge(W * 100, -120, [200, 100, 50], 41), 100)}" fill="#d7b98c"/>` +
        `<path d="${ridgePath(ridge(W * 100, -40, [120, 70], 42), 100)}" fill="#c6a273"/>`;
    },
    mid(W) {
      const total = W * 100;
      const wall = '#b48d5e';
      let s = `<path d="M0,-40 L${total},-40 L${total},-190 L0,-190 Z" fill="${wall}"/>`;
      // Crenellations along the wall top
      for (let x = 0; x < total; x += 40)
        s += `<path d="M${x + 6},-190 L${x + 6},-212 L${x + 26},-212 L${x + 26},-190 Z" fill="${wall}"/>`;
      s += `<path d="M0,-150 L${total},-150 M0,-100 L${total},-100" stroke="${shade(wall, -0.15)}" stroke-width="3"/>`;
      for (let x = 0; x < total; x += 60)
        s += `<path d="M${x},-190 L${x},-150 M${x + 30},-150 L${x + 30},-100 M${x},-100 L${x},-40" stroke="${shade(wall, -0.15)}" stroke-width="2"/>`;
      // Towers with banners
      const towers = 3;
      for (let i = 0; i < towers; i++) {
        const tx = (i + 0.5) * (total / towers);
        s += `<path d="M${tx - 70},-40 L${tx - 64},-300 L${tx + 64},-300 L${tx + 70},-40 Z" fill="${shade(wall, 0.06)}" ${OT}/>`;
        for (let k = 0; k < 4; k++)
          s += `<path d="M${tx - 66 + k * 36},-300 L${tx - 66 + k * 36},-326 L${tx - 44 + k * 36},-326 L${tx - 44 + k * 36},-300 Z" fill="${shade(wall, 0.06)}" ${OT}/>`;
        s += `<path d="M${tx - 14},-180 L${tx - 14},-220 C${tx - 14},-236 ${tx + 14},-236 ${tx + 14},-220 L${tx + 14},-180 Z" fill="#3a2418"/>`;
        s += `<path d="M${tx},-326 L${tx},-440" stroke="#4a3020" stroke-width="5"/>`;
        s += `<path d="M${tx + 2},-436 L${tx + 70},-426 L${tx + 56},-404 L${tx + 70},-382 L${tx + 2},-392 Z" fill="${i % 2 ? '#7a2a8a' : '#b8262a'}" ${OT}/>`;
        s += `<circle cx="${tx + 30}" cy="-410" r="9" fill="#e8b94a"/>`;
      }
      s += `<path d="M0,-190 L${total},-190" stroke="${shade(wall, -0.3)}" stroke-width="3"/>`;
      s += `<path d="${ridgePath(ridge(total, -10, [20, 10], 43), 100)}" fill="#c4a06a"/>`;
      return s;
    },
    ground: { top: '#b89566', soil: '#94744c', tuft: '#c8b060', tufts: 26, seed: 51 },
  },
  forest: {
    far(W) {
      let s = `<path d="${ridgePath(ridge(W * 100, -260, [260, 120, 60], 61), 100)}" fill="#6f8f86"/>`;
      s += `<path d="M0,-260 L${W * 100},-260 L${W * 100},-200 L0,-200 Z" fill="#ffffff" opacity="0.18"/>`;
      s += `<path d="${ridgePath(ridge(W * 100, -100, [160, 80, 40], 62), 100)}" fill="#557868"/>`;
      s += `<path d="M0,-120 L${W * 100},-120 L${W * 100},-60 L0,-60 Z" fill="#ffffff" opacity="0.15"/>`;
      return s;
    },
    mid(W) {
      const r = rng(71);
      let s = '';
      for (let x = 20; x < W * 100; x += 50 + r() * 70)
        s += broadTree(x, -20, 380 + r() * 260, mix('#2f5a36', '#4a7a3e', r()), '#3e2c20');
      for (let x = 0; x < W * 100; x += 30 + r() * 40)
        s += `<path d="M${x},-20 C${x - 20},-40 ${x - 30},-60 ${x - 34},-70 C${x - 20},-56 ${x - 8},-48 ${x},-40 C${x + 8},-48 ${x + 20},-56 ${x + 34},-70 C${x + 30},-60 ${x + 20},-40 ${x},-20 Z" fill="#3e6a34"/>`;
      s += `<path d="M0,-40 L${W * 100},-40 L${W * 100},10 L0,10 Z" fill="#ffffff" opacity="0.12"/>`;
      return s;
    },
    ground: { top: '#5a6a34', soil: '#4a3c2a', tuft: '#6a8a3a', tufts: 60, tuftHeight: 18, seed: 81 },
  },
  valley: {
    far(W) {
      const total = W * 100;
      const r = rng(91);
      let s = `<path d="${ridgePath(ridge(total, -200, [140, 60], 92), 100)}" fill="#d29a7a"/>`;
      // Flat-topped mesas
      for (let i = 0; i < 4; i++) {
        const x = (i + r() * 0.5) * (total / 4);
        const w = 300 + r() * 300;
        const h = 380 + r() * 260;
        s += `<path d="M${x},100 L${x + 30},${-h} L${x + w - 30},${-h} L${x + w},100 Z" fill="#b8705a"/>`;
        s += `<path d="M${x + w * 0.55},${-h} L${x + w - 30},${-h} L${x + w},100 L${x + w * 0.6},100 Z" fill="#9a5a48"/>`;
        s += `<path d="M${x + 20},${-h * 0.6} L${x + w - 20},${-h * 0.6} M${x + 10},${-h * 0.3} L${x + w - 10},${-h * 0.3}" stroke="#a8624e" stroke-width="5"/>`;
      }
      s += `<path d="${ridgePath(ridge(total, -40, [80, 40], 93), 100)}" fill="#c08a68"/>`;
      return s;
    },
    mid(W) {
      const r = rng(101);
      const total = W * 100;
      let s = `<path d="${ridgePath(ridge(total, -60, [40, 20], 102), 100)}" fill="#b08a68"/>`;
      for (let x = 60; x < total - 60; x += 220 + r() * 160) {
        if (r() < 0.65)
          s += tent(x, -40, 170 + r() * 60, 130 + r() * 40, '#e8dcc0', r() < 0.5 ? '#b8262a' : '#2a6a7a');
        else
          s += rock(x, -40, 160, 120 + r() * 80, '#9a7a62');
      }
      s += `<path d="${ridgePath(ridge(total, -10, [14, 8], 103), 100)}" fill="#a88a6a"/>`;
      return s;
    },
    ground: { top: '#9a7a5c', soil: '#7a5a44', tuft: '#9a9a5a', tufts: 18, seed: 111 },
  },
  mountain: {
    far(W) {
      const total = W * 100;
      return `<path d="${ridgePath(ridge(total, -300, [220, 120], 121), 100)}" fill="#b5c4d4"/>` +
        peaks(total, 6, -100, 600, 950, '#8ea2b8', '#f6f9fc', 122) +
        peaks(total, 9, 0, 300, 520, '#74889e', '#eef3f8', 123);
    },
    mid(W) {
      const r = rng(131);
      const total = W * 100;
      let s = `<path d="${ridgePath(ridge(total, -80, [60, 30], 132), 100)}" fill="#dfe8f0"/>`;
      for (let x = 30; x < total; x += 60 + r() * 120) {
        if (r() < 0.75)
          s += pine(x, -50 + r() * 20, 220 + r() * 200, '#2f5048', '#f0f5fa');
        else
          s += rock(x, -40, 140, 100 + r() * 60, '#7d8794');
      }
      s += `<path d="${ridgePath(ridge(total, -10, [20, 10], 133), 100)}" fill="#e8eef4"/>`;
      return s;
    },
    ground: { top: '#eef3f8', soil: '#9aa4b0', seed: 141, extra: W => stones(W, -2, 8, '#7d8794', 10, 142) },
  },
  peak: {
    far(W) {
      const total = W * 100;
      const r = rng(151);
      let s = peaks(total, 5, 0, 380, 700, '#8a96a8', '#f6f9fc', 152);
      // Sea of clouds below the summit
      s += `<path d="M0,-90 L${total},-90 L${total},100 L0,100 Z" fill="#f4f7fb"/>`;
      for (let i = 0; i < 40; i++) {
        const x = (i / 40) * total + r() * 60;
        const rr = 50 + r() * 80;
        s += `<circle cx="${x}" cy="${-90 + rr * 0.3}" r="${rr}" fill="${i % 3 ? '#ffffff' : '#e8eef6'}"/>`;
      }
      s += `<path d="M0,20 L${total},20" stroke="#dfe7f0" stroke-width="30"/>`;
      return s;
    },
    mid(W) {
      const r = rng(161);
      const total = W * 100;
      let s = '';
      for (let x = 40; x < total; x += 200 + r() * 200)
        s += rock(x, -10, 220 + r() * 120, 160 + r() * 140, '#7a7880');
      s += `<path d="${ridgePath(ridge(total, -20, [30, 14], 162), 100)}" fill="#e4eaf0"/>`;
      return s;
    },
    ground: { top: '#e4eaf0', soil: '#7a7880', seed: 171, extra: W => stones(W, -2, 10, '#5e5c64', 12, 172) },
  },
  river: {
    far(W) {
      const total = W * 100;
      return `<path d="${ridgePath(ridge(total, -60, [90, 40], 181), 100)}" fill="#a8b88a"/>` +
        `<path d="${ridgePath(ridge(total, -20, [40, 20], 182), 100)}" fill="#94a874"/>`;
    },
    mid(W) {
      const total = W * 100;
      const r = rng(191);
      let s = `<path d="M0,-90 L${total},-90 L${total},-30 L0,-30 Z" fill="#4a8ab8"/>`;
      for (let i = 0; i < 22; i++) {
        const x = r() * total;
        const y = -80 + r() * 44;
        s += `<path d="M${x},${y} L${x + 40 + r() * 60},${y}" stroke="#a8d0f0" stroke-width="3" stroke-linecap="round"/>`;
      }
      for (let x = 100; x < total; x += 400 + r() * 300)
        s += broadTree(x, -96, 260 + r() * 100, '#5a8a3e');
      for (let x = 0; x < total; x += 12 + r() * 14) {
        const h = 50 + r() * 70;
        s += `<path d="M${x},-20 L${x + 2},${-20 - h} L${x + 4},-20 Z" fill="${mix('#6a8a3a', '#a8a050', r())}"/>`;
        if (r() < 0.3)
          s += `<ellipse cx="${x + 2}" cy="${-20 - h}" rx="3" ry="9" fill="#7a5030"/>`;
      }
      return s;
    },
    ground: { top: '#6e8e3e', soil: '#6a5236', tuft: '#8aa848', tufts: 50, seed: 201 },
  },
};

function layerAsset(biome, name) {
  const L = LAYERS[name];
  const W = L.w * 100;
  const bbox = [0, -L.top * 100, W, L.bottom * 100];
  const def = BIOMES[biome][name];
  const content = name === 'ground' ? groundLayer(W, def) : def(L.w);
  const a = asset(`Backdrops/${biome}_${name}`, bbox, tile(W, content), { scale: L.ppu / 100, ppu: L.ppu, anchor: [W / 2, 0] });
  a.tiled = true;
  return a;
}

export function backdropAssets() {
  const out = [];
  for (const biome of Object.keys(BIOMES))
    for (const name of ['far', 'mid', 'ground'])
      out.push(layerAsset(biome, name));
  return out;
}

/** A preview of one biome with all layers stacked over a sky, 30 units wide. */
export function composeBackdrop(biome, layersOnly = false) {
  const w = 3000;
  const top = -1100;
  const bottom = 400;
  const scale = 0.5;
  const layer = name => {
    const L = LAYERS[name];
    const W = L.w * 100;
    const def = BIOMES[biome][name];
    const content = name === 'ground' ? groundLayer(W, def) : def(L.w);
    let g = '';
    for (let x = -W; x < w + W; x += W)
      g += `<g transform="translate(${x} 0)">${content}</g>`;
    return g;
  };
  if (layersOnly)
    return { layers: `<g transform="translate(-2000 0)">${layer('far')}${layer('mid')}${layer('ground')}</g>` };
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w * scale}" height="${(bottom - top) * scale}" viewBox="0 ${top} ${w} ${bottom - top}">` +
    `<defs><linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#6fa8dc"/><stop offset="1" stroke-color="#f6e2b8" stop-color="#f6e2b8"/></linearGradient></defs>` +
    `<rect x="0" y="${top}" width="${w}" height="${bottom - top}" fill="url(#sky)"/>${layer('far')}${layer('mid')}${layer('ground')}</svg>`;
  return { svg, width: w * scale, height: (bottom - top) * scale };
}
