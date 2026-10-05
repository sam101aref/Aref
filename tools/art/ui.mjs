// Interface art: 9-slice frames and buttons, bars, icons and a girih pattern.
// UI sprites use 100 pixels per unit so a sliced border shows at 1 px per reference pixel.

import { GOLD, GOLD_DARK, STEEL, STEEL_DARK, shade, pearls, asset } from './lib.mjs';

const UI = (name, size, content, border = null) =>
  asset('UI/' + name, [0, 0, size[0], size[1]], content, { scale: 1, ppu: 100, anchor: [size[0] / 2, size[1] / 2], border });

const DARK = '#1b120c';

function frame() {
  const s = 192;
  let c = `<rect x="4" y="4" width="${s - 8}" height="${s - 8}" rx="18" fill="#13213f" stroke="${DARK}" stroke-width="4"/>`;
  c += `<rect x="10" y="10" width="${s - 20}" height="${s - 20}" rx="14" fill="none" stroke="${GOLD}" stroke-width="5"/>`;
  c += `<rect x="20" y="20" width="${s - 40}" height="${s - 40}" rx="8" fill="none" stroke="${GOLD_DARK}" stroke-width="2"/>`;
  // Corner arabesques
  for (const [x, y, r] of [[22, 22, 0], [s - 22, 22, 90], [s - 22, s - 22, 180], [22, s - 22, 270]])
    c += `<g transform="translate(${x} ${y}) rotate(${r})"><path d="M-4,-4 C10,-6 22,2 24,14 C16,8 8,8 2,12 C6,6 4,0 -4,-4 Z" fill="${GOLD}"/><circle cx="2" cy="2" r="5" fill="${GOLD}" stroke="${DARK}" stroke-width="1.5"/></g>`;
  return UI('frame', [s, s], c, [48, 48, 48, 48]);
}

function panel() {
  const s = 96;
  return UI('panel', [s, s], `<rect x="2" y="2" width="${s - 4}" height="${s - 4}" rx="16" fill="#000000" fill-opacity="0.55" stroke="${GOLD_DARK}" stroke-width="2.5"/>`, [24, 24, 24, 24]);
}

function button(name, base) {
  const w = 160;
  const h = 96;
  let c = `<rect x="3" y="7" width="${w - 6}" height="${h - 10}" rx="22" fill="${shade(base, -0.45)}"/>`;
  c += `<rect x="3" y="3" width="${w - 6}" height="${h - 12}" rx="22" fill="${base}" stroke="${DARK}" stroke-width="3"/>`;
  c += `<rect x="10" y="9" width="${w - 20}" height="${(h - 12) * 0.42}" rx="14" fill="#ffffff" fill-opacity="0.22"/>`;
  c += `<rect x="7" y="7" width="${w - 14}" height="${h - 20}" rx="18" fill="none" stroke="${GOLD}" stroke-width="2.5" stroke-opacity="0.9"/>`;
  return UI('button_' + name, [w, h], c, [36, 36, 36, 36]);
}

function bar(name, fill) {
  const w = 64;
  const h = 32;
  const c = fill
    ? `<rect x="2" y="2" width="${w - 4}" height="${h - 4}" rx="12" fill="${fill}"/><rect x="6" y="5" width="${w - 12}" height="8" rx="4" fill="#ffffff" fill-opacity="0.35"/>`
    : `<rect x="1" y="1" width="${w - 2}" height="${h - 2}" rx="14" fill="#000000" fill-opacity="0.6" stroke="${GOLD_DARK}" stroke-width="2"/>`;
  return UI(name, [w, h], c, [16, 14, 16, 14]);
}

// ------------------------------------------------------------------ icons (128 px)

const ICON = 128;
const icon = (name, content) => UI('Icons/' + name, [ICON, ICON], content);
const STROKE = `stroke="${DARK}" stroke-width="4" stroke-linejoin="round" stroke-linecap="round"`;

function icons() {
  return [
    icon('coin', `<circle cx="64" cy="66" r="50" fill="${GOLD_DARK}" ${STROKE}/><circle cx="64" cy="62" r="50" fill="${GOLD}" ${STROKE}/><circle cx="64" cy="62" r="38" fill="none" stroke="${GOLD_DARK}" stroke-width="3"/>` +
      `<path d="M44,78 C52,58 64,46 82,40 M76,38 L84,40 L80,48" fill="none" stroke="${GOLD_DARK}" stroke-width="6" stroke-linecap="round"/>` + pearls(32, 62, 96, 62, 2, 3, GOLD_DARK)),
    icon('gem', `<path d="M24,48 L44,20 L84,20 L104,48 L64,112 Z" fill="#1fb0b8" ${STROKE}/><path d="M24,48 L104,48 M44,20 L54,48 L64,112 L74,48 L84,20" fill="none" stroke="#0f7a80" stroke-width="3"/><path d="M48,26 L56,44 L40,44 Z" fill="#c8f6f8"/>`),
    icon('star', `<path d="M64,10 L79,44 L116,47 L88,71 L97,108 L64,88 L31,108 L40,71 L12,47 L49,44 Z" fill="${GOLD}" ${STROKE}/><path d="M64,26 L72,46 L56,46 Z" fill="#fff2c0"/>`),
    icon('lock', `<path d="M40,58 L40,40 C40,18 88,18 88,40 L88,58" fill="none" stroke="${STEEL_DARK}" stroke-width="12"/><rect x="26" y="56" width="76" height="58" rx="10" fill="${GOLD}" ${STROKE}/><circle cx="64" cy="80" r="8" fill="${DARK}"/><path d="M64,84 L64,100" stroke="${DARK}" stroke-width="6"/>`),
    icon('heart', `<path d="M64,112 C30,88 12,66 14,44 C16,24 40,14 64,36 C88,14 112,24 114,44 C116,66 98,88 64,112 Z" fill="#d83a3a" ${STROKE}/><path d="M30,42 C32,32 42,28 50,32" fill="none" stroke="#ffb0b0" stroke-width="6" stroke-linecap="round"/>`),
    icon('arrows', `<g transform="rotate(-45 64 64)"><path d="M10,52 L104,52 M10,76 L104,76" stroke="${DARK}" stroke-width="9"/><path d="M10,52 L104,52 M10,76 L104,76" stroke="#c89a5a" stroke-width="5"/>` +
      `<path d="M100,42 L122,52 L100,62 Z M100,66 L122,76 L100,86 Z" fill="${STEEL}" ${STROKE}/><path d="M6,52 L22,40 L30,40 L20,52 L30,64 L22,64 Z M6,76 L22,64 L30,64 L20,76 L30,88 L22,88 Z" fill="#b8262a" ${STROKE}/></g>`),
    icon('shield', `<path d="M64,10 L106,26 C106,70 92,98 64,118 C36,98 22,70 22,26 Z" fill="#1f5f7a" ${STROKE}/><path d="M64,22 L94,34 C94,68 84,88 64,104 Z" fill="#2f7f9a"/><path d="M64,10 L106,26 C106,70 92,98 64,118 C36,98 22,70 22,26 Z" fill="none" stroke="${GOLD}" stroke-width="5"/>`),
    icon('helmet', `<path d="M22,84 C20,46 40,18 64,14 C88,18 108,46 106,84 Z" fill="${STEEL}" ${STROKE}/><path d="M18,84 L110,84 L110,98 L18,98 Z" fill="${GOLD}" ${STROKE}/><path d="M58,84 L70,84 L70,116 L58,116 Z" fill="${STEEL}" ${STROKE}/><path d="M40,70 C40,46 50,30 62,24" fill="none" stroke="#ffffff" stroke-width="6" stroke-linecap="round"/>`),
    icon('armor', `<path d="M30,20 L50,14 C56,24 72,24 78,14 L98,20 L112,46 L96,54 L96,114 L32,114 L32,54 L16,46 Z" fill="${STEEL}" ${STROKE}/>` +
      [0, 1, 2, 3].map(r => `<path d="M38,${56 + r * 14} C50,${64 + r * 14} 78,${64 + r * 14} 90,${56 + r * 14}" fill="none" stroke="${STEEL_DARK}" stroke-width="3"/>`).join('') +
      `<circle cx="64" cy="44" r="9" fill="${GOLD}" ${STROKE}/>`),
    icon('quiver', `<g transform="rotate(20 64 64)"><path d="M50,8 L46,30 L54,30 Z M64,4 L60,30 L68,30 Z M78,8 L74,30 L82,30 Z" fill="#b8262a" ${STROKE}/><rect x="40" y="28" width="48" height="90" rx="12" fill="#8a5028" ${STROKE}/><rect x="40" y="28" width="48" height="14" rx="5" fill="${GOLD}" ${STROKE}/>` + pearls(64, 58, 64, 104, 4, 3.5, GOLD) + `</g>`),
    icon('outfit', `<path d="M40,14 L54,10 C58,20 70,20 74,10 L88,14 L116,40 L100,56 L92,48 L94,116 L34,116 L36,48 L28,56 L12,40 Z" fill="#1a6b8c" ${STROKE}/><path d="M54,10 L64,40 L74,10" fill="none" stroke="${GOLD}" stroke-width="5"/><path d="M36,100 L92,100" stroke="${GOLD}" stroke-width="6"/>` + pearls(42, 108, 86, 108, 5, 2.5, '#fff2c0')),
    icon('bow', `<path d="M90,8 L26,64 L90,120" fill="none" stroke="#efe6d0" stroke-width="3"/><path d="M90,8 C76,16 70,30 78,46 C86,58 96,60 96,64 C96,68 86,70 78,82 C70,98 76,112 90,120" fill="none" stroke="${DARK}" stroke-width="14" stroke-linecap="round"/>` +
      `<path d="M90,8 C76,16 70,30 78,46 C86,58 96,60 96,64 C96,68 86,70 78,82 C70,98 76,112 90,120" fill="none" stroke="#8a5028" stroke-width="8" stroke-linecap="round"/><circle cx="90" cy="8" r="6" fill="${GOLD}" ${STROKE}/><circle cx="90" cy="120" r="6" fill="${GOLD}" ${STROKE}/>`),
    icon('plus', `<circle cx="64" cy="64" r="50" fill="#000000" fill-opacity="0.35" stroke="${GOLD}" stroke-width="5" stroke-dasharray="12 8"/><path d="M64,38 L64,90 M38,64 L90,64" stroke="${GOLD}" stroke-width="12" stroke-linecap="round"/>`),
    icon('farr', `<g>${Array.from({ length: 12 }, (_, i) => `<path d="M64,64 L${64 + 58 * Math.cos(i * Math.PI / 6 - 0.13)},${64 + 58 * Math.sin(i * Math.PI / 6 - 0.13)} L${64 + 58 * Math.cos(i * Math.PI / 6 + 0.13)},${64 + 58 * Math.sin(i * Math.PI / 6 + 0.13)} Z" fill="${GOLD}"/>`).join('')}</g>` +
      `<circle cx="64" cy="64" r="32" fill="#fff2c0" ${STROKE}/><circle cx="64" cy="64" r="20" fill="${GOLD}"/>`),
    icon('rain', `<g transform="rotate(20 64 64)">${[28, 52, 76, 100].map((x, i) => `<path d="M${x},${10 + (i % 2) * 18} L${x},${80 + (i % 2) * 18}" stroke="${DARK}" stroke-width="8"/><path d="M${x},${10 + (i % 2) * 18} L${x},${80 + (i % 2) * 18}" stroke="${GOLD}" stroke-width="4"/><path d="M${x - 7},${78 + (i % 2) * 18} L${x},${96 + (i % 2) * 18} L${x + 7},${78 + (i % 2) * 18} Z" fill="#fff2c0" ${STROKE}/>`).join('')}</g>`),
    icon('pause', `<rect x="34" y="26" width="22" height="76" rx="6" fill="#f8f0dc" ${STROKE}/><rect x="72" y="26" width="22" height="76" rx="6" fill="#f8f0dc" ${STROKE}/>`),
    icon('settings', `<g fill="#f8f0dc" ${STROKE}>${Array.from({ length: 8 }, (_, i) => `<rect x="56" y="10" width="16" height="26" rx="4" transform="rotate(${i * 45} 64 64)"/>`).join('')}<circle cx="64" cy="64" r="34"/></g><circle cx="64" cy="64" r="13" fill="#13213f" ${STROKE}/>`),
    icon('back', `<path d="M76,24 L36,64 L76,104" fill="none" stroke="${DARK}" stroke-width="22" stroke-linecap="round" stroke-linejoin="round"/><path d="M76,24 L36,64 L76,104" fill="none" stroke="#f8f0dc" stroke-width="12" stroke-linecap="round" stroke-linejoin="round"/>`),
    icon('book', `<path d="M64,30 C48,18 26,18 12,24 L12,106 C26,100 48,100 64,112 Z" fill="#f4e8c8" ${STROKE}/><path d="M64,30 C80,18 102,18 116,24 L116,106 C102,100 80,100 64,112 Z" fill="#efe0b8" ${STROKE}/>` +
      `<path d="M24,44 C36,40 48,42 56,46 M24,60 C36,56 48,58 56,62 M72,46 C80,42 92,40 104,44 M72,62 C80,58 92,56 104,60" fill="none" stroke="#a89060" stroke-width="3"/><path d="M60,32 L60,112" stroke="#b8262a" stroke-width="5"/>`),
    icon('chest', `<path d="M14,112 L14,62 L114,62 L114,112 Z" fill="#8a5a2c" ${STROKE}/><path d="M14,62 C14,26 114,26 114,62 Z" fill="#a06a34" ${STROKE}/><path d="M14,62 L114,62 M36,38 L36,112 M92,38 L92,112" stroke="${GOLD}" stroke-width="8"/><rect x="54" y="52" width="20" height="26" rx="3" fill="${GOLD}" ${STROKE}/>`),
    icon('check', `<path d="M24,66 L52,94 L104,34" fill="none" stroke="${DARK}" stroke-width="22" stroke-linecap="round" stroke-linejoin="round"/><path d="M24,66 L52,94 L104,34" fill="none" stroke="#5ad06a" stroke-width="12" stroke-linecap="round" stroke-linejoin="round"/>`),
    icon('swap', `<path d="M24,46 L96,46 M80,28 L100,46 L80,64 M104,82 L32,82 M48,64 L28,82 L48,100" fill="none" stroke="${DARK}" stroke-width="16" stroke-linecap="round" stroke-linejoin="round"/><path d="M24,46 L96,46 M80,28 L100,46 L80,64 M104,82 L32,82 M48,64 L28,82 L48,100" fill="none" stroke="#f8f0dc" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"/>`),
  ];
}

/** A tileable girih pattern of ten-pointed stars for menu backgrounds. */
function pattern() {
  const s = 256;
  let c = `<rect width="${s}" height="${s}" fill="#13213f"/>`;
  const star = (cx, cy, r) => {
    const pts = [];
    for (let i = 0; i < 20; i++) {
      const a = (i * Math.PI) / 10 - Math.PI / 2;
      const rr = i % 2 ? r * 0.62 : r;
      pts.push(`${(cx + rr * Math.cos(a)).toFixed(1)},${(cy + rr * Math.sin(a)).toFixed(1)}`);
    }
    return `<polygon points="${pts.join(' ')}" fill="#1a2c52" stroke="${GOLD_DARK}" stroke-width="3" stroke-opacity="0.7"/><circle cx="${cx}" cy="${cy}" r="${r * 0.28}" fill="#21386a" stroke="${GOLD_DARK}" stroke-width="2" stroke-opacity="0.6"/>`;
  };
  for (const [x, y] of [[0, 0], [s, 0], [0, s], [s, s], [s / 2, s / 2]])
    c += star(x, y, 70);
  for (const [x, y] of [[s / 2, 0], [s / 2, s], [0, s / 2], [s, s / 2]])
    c += `<rect x="${x - 18}" y="${y - 18}" width="36" height="36" transform="rotate(45 ${x} ${y})" fill="#1a2c52" stroke="${GOLD_DARK}" stroke-width="2.5" stroke-opacity="0.6"/>`;
  const a = UI('pattern', [s, s], c);
  a.tiled = true;
  return a;
}

function divider() {
  const w = 512;
  const h = 40;
  return UI('divider', [w, h], `<path d="M10,20 L220,20 M292,20 L502,20" stroke="${GOLD}" stroke-width="3"/><path d="M256,4 L276,20 L256,36 L236,20 Z" fill="${GOLD}" stroke="${DARK}" stroke-width="2"/><circle cx="226" cy="20" r="4" fill="${GOLD}"/><circle cx="286" cy="20" r="4" fill="${GOLD}"/>`);
}

export function uiAssets() {
  return [
    frame(), panel(),
    button('turquoise', '#178a92'), button('gold', '#d9a53a'), button('lapis', '#24407a'), button('crimson', '#a8302c'), button('grey', '#5a5e6a'),
    bar('bar_bg', null), bar('bar_fill', '#ffffff'),
    ...icons(), pattern(), divider(),
  ];
}
