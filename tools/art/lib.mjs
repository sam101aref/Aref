// Shared helpers for the SVG art generator.

/** Outline used on every shape: dark brown, round joins. */
export const OUTLINE = '#2b1d14';
export const O = `stroke="${OUTLINE}" stroke-width="2.6" stroke-linejoin="round" stroke-linecap="round"`;
export const OT = `stroke="${OUTLINE}" stroke-width="1.6" stroke-linejoin="round" stroke-linecap="round"`;

/** Greys used for parts that are tinted at runtime (multiply), so shading survives any colour. */
export const G = { base: '#f0f0f0', light: '#ffffff', shade: '#c2c2c2', deep: '#969696', line: '#7a7a7a' };

export const GOLD = '#e8b94a';
export const GOLD_DARK = '#a8781f';
export const STEEL = '#b9c1c9';
export const STEEL_DARK = '#7d8791';

function clamp(v) {
  return Math.max(0, Math.min(255, Math.round(v)));
}

function parse(hex) {
  const h = hex.replace('#', '');
  return [0, 2, 4].map(i => parseInt(h.substr(i, 2), 16));
}

function toHex(rgb) {
  return '#' + rgb.map(c => clamp(c).toString(16).padStart(2, '0')).join('');
}

/** Lightens (amount > 0) or darkens (amount < 0) a colour. */
export function shade(hex, amount) {
  const rgb = parse(hex);
  return toHex(rgb.map(c => (amount >= 0 ? c + (255 - c) * amount : c * (1 + amount))));
}

export function mix(a, b, t) {
  const x = parse(a);
  const y = parse(b);
  return toHex(x.map((c, i) => c + (y[i] - c) * t));
}

/** Small deterministic random generator so the art is identical on every run. */
export function rng(seed) {
  let s = seed >>> 0 || 1;
  return () => {
    s ^= s << 13;
    s ^= s >>> 17;
    s ^= s << 5;
    return ((s >>> 0) % 100000) / 100000;
  };
}

/** A row of small circles along a line: the Sassanid "pearl border". */
export function pearls(x0, y0, x1, y1, count, r, fill) {
  let out = '';
  for (let i = 0; i < count; i++) {
    const t = count === 1 ? 0.5 : i / (count - 1);
    out += `<circle cx="${(x0 + (x1 - x0) * t).toFixed(1)}" cy="${(y0 + (y1 - y0) * t).toFixed(1)}" r="${r}" fill="${fill}"/>`;
  }
  return out;
}

/**
 * An asset to render. Coordinates are SVG units; `scale` is pixels per SVG unit.
 * bbox = [x0, y0, x1, y1] (y down). anchor = pivot point in the same space.
 */
export function asset(path, bbox, content, { scale = 1.28, anchor = null, ppu = 128, border = null, defs = '' } = {}) {
  const [x0, y0, x1, y1] = bbox;
  const w = Math.round((x1 - x0) * scale);
  const h = Math.round((y1 - y0) * scale);
  const pivot = anchor ? [(anchor[0] - x0) / (x1 - x0), (y1 - anchor[1]) / (y1 - y0)] : [0.5, 0.5];
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="${x0} ${y0} ${x1 - x0} ${y1 - y0}">${defs ? `<defs>${defs}</defs>` : ''}${content}</svg>`;
  return { path, svg, width: w, height: h, pivot: pivot.map(v => +v.toFixed(4)), ppu, border };
}
