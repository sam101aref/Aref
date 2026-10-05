// Renders cutscene panels from Assets/_Project/Editor/Content/StoryScript.json to PNG, the way the
// game frames them at the end of each panel's camera move (approximately: no animation).
//
//   node tools/art/cutscene-preview.mjs [cutscene ids…]   e.g. prologue lv1_1 ending
//
// Output: tools/art/preview/cutscenes/{id}_{n}.png

import { createRequire } from 'module';
import { mkdirSync, readFileSync, writeFileSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { fileURLToPath } from 'url';
import { composeCharacter } from './characters.mjs';
import { composeBackdrop } from './backdrops.mjs';
import { propAssets } from './props.mjs';

const require = createRequire(import.meta.url);
let playwright;
try {
  playwright = require('playwright');
} catch {
  playwright = require('/opt/node-tools/node_modules/playwright');
}

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '../..');
const script = JSON.parse(readFileSync(join(root, 'Assets/_Project/Editor/Content/StoryScript.json'), 'utf8'));
const font = readFileSync(join(root, 'Assets/_Project/Resources/Fonts/Vazirmatn-Regular.ttf')).toString('base64');
const props = Object.fromEntries(propAssets().map(a => [a.path.replace(/^Props\//, ''), a]));

// Default colours of the looks (see LookStyle in CharacterParts.cs).
const STYLE = {
  arash: ['#1a6b8c', '#a8262c'], roshana: ['#38734d', '#804d33'], mobad: ['#f5f0de', '#e6d9b3'],
  manuchehr: ['#6b338c', '#b32429'], afrasiab: ['#421f1f', '#731414'], villager: ['#9e7852', '#80664d'],
  envoy: ['#dbcc99', '#80664d'], turanian: ['#8c261f', '#4d3326'], shieldbearer: ['#6b5242', '#4d3326'],
  spearman: ['#9e804d', '#4d3326'], rider: ['#803326', '#523826'], slinger: ['#94855c', '#4d3326'],
  firearcher: ['#c75c1f', '#4d3326'], assassin: ['#33303b', '#26242b'], raider: ['#754d33', '#4d3326'],
  shaman: ['#5c3375', '#4d3326'], commander: ['#9e1f1a', '#661a14'], barman: ['#524238', '#38291f'],
  garsivaz: ['#5c266b', '#42144d'],
};
const TIME = {
  day: { top: '#66a3db', bottom: '#f5e0b8', tint: [1, 1, 1] },
  dusk: { top: '#4d548c', bottom: '#fa9e66', tint: [0.95, 0.76, 0.72] },
  night: { top: '#0a0f29', bottom: '#2b406b', tint: [0.42, 0.48, 0.66] },
};

function panelHtml(panel) {
  const cam = panel.camera || {};
  const z = cam.z1 || 4;
  const cx = cam.x1 || 0;
  const cy = cam.y1 || 2.6;
  const hw = z * 16 / 9;
  const left = (cx - hw) * 100, top = -(cy + z) * 100, W = hw * 200, H = z * 200;
  const time = TIME[panel.time] || TIME.day;
  const [r, g, b] = time.tint;
  let defs = `<linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${time.top}"/><stop offset="1" stop-color="${time.bottom}"/></linearGradient>` +
    `<filter id="tod"><feColorMatrix type="matrix" values="${r} 0 0 0 0  0 ${g} 0 0 0  0 0 ${b} 0 0  0 0 0 1 0"/></filter>`;
  const backdrop = composeBackdrop(panel.biome || 'village', true);
  let back = '';
  let front = '';
  for (const p of panel.props || []) {
    const a = props[p.sprite];
    if (!a)
      continue;
    const [x0, y0, x1, y1] = a.bbox;
    const s = p.scale || 1;
    const bw = (x1 - x0) * s, bh = (y1 - y0) * s;
    const px = p.x * 100 - a.pivot[0] * bw;
    const py = -p.y * 100 - (1 - a.pivot[1]) * bh;
    const flip = p.flip ? `transform="translate(${2 * px + bw} 0) scale(-1 1)"` : '';
    const el = `<g ${flip}><svg x="${px}" y="${py}" width="${bw}" height="${bh}" viewBox="${x0} ${y0} ${x1 - x0} ${y1 - y0}">${a.defs ? `<defs>${a.defs}</defs>` : ''}${a.content}</svg></g>`;
    if (p.front)
      front += el;
    else
      back += el;
  }
  let actors = '';
  for (const a of panel.actors || []) {
    const style = STYLE[a.look] || [];
    const angle = a.pose === 'aim' ? -12 : a.pose === 'raise' ? -72 : 65;
    const c = composeCharacter(a.look, { tunic: style[0] || '#1a6b8c', capeColor: style[1] || '#a8262c', angle });
    defs += c.defs;
    const s = a.scale || 1;
    const mirror = a.face === 'left' ? -s : s;
    const rot = a.pose === 'fallen' ? ` rotate(${a.face === 'left' ? -88 : 88})` : '';
    actors += `<g transform="translate(${a.x * 100} ${-(a.y || 0) * 100 - (a.pose === 'fallen' ? 30 : 0)})${rot} scale(${mirror} ${s})">${c.content}</g>`;
  }
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720" viewBox="${left} ${top} ${W} ${H}"><defs>${defs}</defs>` +
    `<rect x="${left}" y="${top}" width="${W}" height="${H}" fill="url(#sky)"/><g filter="url(#tod)">${backdrop.layers}${back}</g>` +
    `<g filter="url(#tod)">${actors}${front}</g></svg>`;
  const text = panel.text || {};
  const speaker = panel.speaker && panel.speaker !== 'narrator' ? `<div class="who">${panel.speaker}</div>` : '';
  return `<!doctype html><html><head><style>
    @font-face { font-family: V; src: url(data:font/ttf;base64,${font}); }
    html,body { margin:0; width:1280px; height:720px; overflow:hidden; font-family: V, sans-serif; }
    .cap { position:absolute; left:0; right:0; bottom:0; height:150px; background:rgba(0,0,0,0.62); color:#faf0d8; padding:14px 60px; box-sizing:border-box; }
    .fa { direction: rtl; font-size: 26px; line-height: 1.5; }
    .en { font-size: 18px; opacity: 0.8; margin-top: 6px; }
    .who { position:absolute; top:-34px; right:40px; color:#e6b84f; font-size:22px; }
  </style></head><body>${svg}<div class="cap">${speaker}<div class="fa">${text.fa || ''}</div><div class="en">${text.en || ''}</div></div></body></html>`;
}

function allCutscenes() {
  const list = {};
  for (const chapter of script.chapters) {
    list[chapter.number === 0 ? 'prologue' : 'ch' + chapter.number] = chapter.intro;
    for (const level of chapter.levels) {
      list['lv' + level.id] = level.cutscene;
      if (level.outro && level.outro.panels && level.outro.panels.length)
        list['lv' + level.id + 'o'] = level.outro;
    }
  }
  list.ending = script.ending;
  return list;
}

const cutscenes = allCutscenes();
const ids = process.argv.slice(2).length ? process.argv.slice(2) : Object.keys(cutscenes);
const dir = join(here, 'preview/cutscenes');
mkdirSync(dir, { recursive: true });
let browser;
try {
  browser = await playwright.chromium.launch();
} catch {
  browser = await playwright.chromium.launch({ executablePath: '/opt/pw-browsers/chromium' });
}
const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
let count = 0;
for (const id of ids) {
  const c = cutscenes[id];
  if (!c) {
    console.warn('No cutscene ' + id);
    continue;
  }
  for (let i = 0; i < c.panels.length; i++) {
    await page.setContent(panelHtml(c.panels[i]));
    await page.screenshot({ path: join(dir, `${id}_${i + 1}.png`) });
    count++;
  }
}
await browser.close();
console.log(`Rendered ${count} panels into ${dir}`);
