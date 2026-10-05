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
