// Renders the game's art from SVG to PNG with headless Chromium (Playwright).
//
//   node tools/art/render.mjs            render every sprite into Assets/_Project/Resources/Art
//   node tools/art/render.mjs --preview  render preview sheets into tools/art/preview
//
// The manifest (Assets/_Project/Art/art-manifest.json) holds each sprite's pivot, pixels per
// unit and 9-slice border; the editor setup step (ArtImport) applies it to the texture importers.
// Replacing a PNG with hand-drawn art of the same name keeps everything working.

import { createRequire } from 'module';
import { mkdirSync, writeFileSync } from 'fs';
import { dirname, join, resolve } from 'path';
import { fileURLToPath } from 'url';
import { characterAssets, composeCharacter, LOOKS } from './characters.mjs';
import { backdropAssets, composeBackdrop, BIOMES } from './backdrops.mjs';
import { propAssets } from './props.mjs';
import { uiAssets } from './ui.mjs';

const require = createRequire(import.meta.url);
let playwright;
try {
  playwright = require('playwright');
} catch {
  playwright = require('/opt/node-tools/node_modules/playwright');
}

const here = dirname(fileURLToPath(import.meta.url));
const root = resolve(here, '../..');
const outDir = join(root, 'Assets/_Project/Resources/Art');
const manifestPath = join(root, 'Assets/_Project/Art/art-manifest.json');

async function launch() {
  const options = {};
  if (process.env.PLAYWRIGHT_BROWSERS_PATH)
    options.executablePath = undefined;
  try {
    return await playwright.chromium.launch(options);
  } catch {
    return await playwright.chromium.launch({ executablePath: '/opt/pw-browsers/chromium' });
  }
}

async function renderSvg(page, svg, width, height) {
  await page.setViewportSize({ width, height });
  await page.setContent(`<!doctype html><html><head><style>html,body{margin:0;padding:0;background:transparent}svg{display:block}</style></head><body>${svg}</body></html>`);
  return page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width, height } });
}

async function renderAll() {
  const assets = [...characterAssets(), ...backdropAssets(), ...propAssets(), ...uiAssets()];
  const browser = await launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  const sprites = [];
  for (const a of assets) {
    const file = join(outDir, a.path + '.png');
    mkdirSync(dirname(file), { recursive: true });
    writeFileSync(file, await renderSvg(page, a.svg, a.width, a.height));
    sprites.push({
      path: 'Assets/_Project/Resources/Art/' + a.path + '.png',
      ppu: a.ppu,
      pivotX: a.pivot[0],
      pivotY: a.pivot[1],
      border: a.border || [0, 0, 0, 0],
      tiled: !!a.tiled,
    });
  }
  await browser.close();
  mkdirSync(dirname(manifestPath), { recursive: true });
  // One sprite per line; read by the editor with JsonUtility.
  writeFileSync(manifestPath, '{"sprites":[\n' + sprites.map(e => JSON.stringify(e)).join(',\n') + '\n]}\n');
  console.log(`Rendered ${assets.length} sprites.`);
}

async function preview() {
  const dir = join(here, 'preview');
  mkdirSync(dir, { recursive: true });
  const browser = await launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });

  // Characters: every look side by side.
  const ids = Object.keys(LOOKS);
  const cols = 7;
  const cell = 200;
  let defs = '';
  let body = '';
  ids.forEach((id, i) => {
    const x = (i % cols) * cell + 90;
    const y = Math.floor(i / cols) * 300 + 280;
    const c = composeCharacter(id);
    defs += c.defs;
    const scale = LOOKS[id].monster ? 1 : 1;
    body += `<g transform="translate(${x} ${y}) scale(${scale})">${c.content}</g><text x="${x}" y="${y + 20}" font-size="14" text-anchor="middle" fill="#fff">${id}</text>`;
  });
  const w = cols * cell;
  const h = Math.ceil(ids.length / cols) * 300 + 20;
  const sheet = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><defs>${defs}</defs><rect width="100%" height="100%" fill="#5b7f99"/>${body}</svg>`;
  writeFileSync(join(dir, 'characters.png'), await renderSvg(page, sheet, w, h));

  // A battle mock-up: the view a phone shows (camera half-height 7.5), Arash on the left.
  {
    const left = -14.5, right = 25.5, top = -14.5, bottom = 2;
    const W = (right - left) * 100, H = (bottom - top) * 100;
    const k = 0.45;
    const sky = `<linearGradient id="bsky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#4a5a8a"/><stop offset="1" stop-color="#f2a066"/></linearGradient>`;
    const b = composeBackdrop('border', true);
    let defs = sky;
    let chars = '';
    const place = (id, x, h, flip, opts = {}) => {
      const c = composeCharacter(id, opts);
      defs += c.defs;
      const sc = opts.scale || 1;
      chars += `<g transform="translate(${x * 100} ${-h * 100}) scale(${flip ? -sc : sc} ${sc})">${c.content}</g>`;
      if (h > 0)
        chars += `<rect x="${x * 100 - 120}" y="${-h * 100}" width="240" height="${h * 100}" rx="12" fill="#7d7468" stroke="#2b1d14" stroke-width="3"/>`;
    };
    place('arash', -6, 0, false, { angle: -8 });
    place('shieldbearer', 9, 0, true, { tunic: '#6b5242' });
    place('turanian', 13, 2.5, true, { tunic: '#8c2a1f', angle: -18 });
    place('rider', 17, 0, true, { tunic: '#80331f' });
    place('shaman', 21, 3.5, true, { tunic: '#5c3375', angle: 60 });
    const alert = `<path d="M1288,-560 L1312,-560 L1307,-518 L1293,-518 Z" fill="#e02a2a" stroke="#fff" stroke-width="4"/><circle cx="1300" cy="-506" r="7.5" fill="#e02a2a" stroke="#fff" stroke-width="4"/>`;
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W * k}" height="${H * k}" viewBox="${left * 100} ${top * 100} ${W} ${H}"><defs>${defs}</defs>` +
      `<rect x="${left * 100}" y="${top * 100}" width="${W}" height="${H}" fill="url(#bsky)"/><g style="filter:url(#dusk)">${b.layers}</g>${chars}${alert}</svg>`;
    writeFileSync(join(dir, 'battle_mockup.png'), await renderSvg(page, svg.replace('</defs>', '<filter id="dusk"><feColorMatrix type="matrix" values="0.95 0 0 0 0  0 0.8 0 0 0  0 0 0.74 0 0  0 0 0 1 0"/></filter></defs>'), Math.round(W * k), Math.round(H * k)));
  }

  for (const biome of Object.keys(BIOMES)) {
    const b = composeBackdrop(biome);
    writeFileSync(join(dir, `backdrop_${biome}.png`), await renderSvg(page, b.svg, b.width, b.height));
  }
  // Props and UI: every sprite in a grid.
  for (const [name, list] of [['props', propAssets()], ['ui', uiAssets()]]) {
    if (!list.length)
      continue;
    const cw = 260;
    const ch = 260;
    const per = 8;
    let inner = '';
    list.forEach((a, i) => {
      const x = (i % per) * cw;
      const y = Math.floor(i / per) * ch;
      const k = Math.min(1, (cw - 20) / a.width, (ch - 40) / a.height);
      const svg = a.svg.replace('<svg ', `<svg x="${x + (cw - a.width * k) / 2}" y="${y + 10}" width="${a.width * k}" height="${a.height * k}" `).replace(/ width="\d+" height="\d+" viewBox/, ' viewBox');
      inner += svg + `<text x="${x + cw / 2}" y="${y + ch - 10}" font-size="13" text-anchor="middle" fill="#fff">${a.path}</text>`;
    });
    const w = per * cw;
    const h = Math.ceil(list.length / per) * ch;
    const sheet = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}"><rect width="100%" height="100%" fill="#4a6a84"/>${inner}</svg>`;
    writeFileSync(join(dir, name + '.png'), await renderSvg(page, sheet, w, h));
  }
  await browser.close();
  console.log('Preview written to ' + dir);
}

if (process.argv.includes('--preview'))
  await preview();
else
  await renderAll();
