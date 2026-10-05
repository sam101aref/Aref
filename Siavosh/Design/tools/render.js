// Renders the jobs written by export_game.py to PNG with Chromium (Playwright) and prints, as JSON,
// each sprite's trimmed box in SVG units and its pixel size.
const { chromium } = require('playwright');
const fs = require('fs');

const MARGIN = 4; // SVG units around the drawn shape, for stroke widths

(async () => {
  const jobs = JSON.parse(fs.readFileSync(process.argv[2], 'utf8'));
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 2600, height: 1400 } });
  const css = process.argv[3] ? fs.readFileSync(process.argv[3], 'utf8') : '';
  await page.setContent(`<html><head><style>${css}</style></head><body style="margin:0;background:transparent"><div id="host"></div>
    <span style="font-family:'Noto Nastaliq Urdu'">سیاوش</span><span style="font-family:Cinzel">Siavosh</span></body></html>`);
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(500);

  const boxes = {};
  for (const job of jobs) {
    const result = await page.evaluate((job) => {
      const host = document.getElementById('host');
      const ns = 'http://www.w3.org/2000/svg';
      if (job.kind === 'text') {
        const rtl = /[؀-ۿ]/.test(job.text);
        host.innerHTML = `<svg id="s" xmlns="${ns}" width="10" height="10" style="overflow:visible"><g id="g">
          <text x="0" y="0" font-family="${job.family}, serif" font-size="${job.fontsize}" font-weight="${rtl ? 700 : 800}"
            direction="${rtl ? 'rtl' : 'ltr'}" fill="#FFFFFF" stroke="#2B1B12" stroke-width="${job.fontsize * 0.07}"
            paint-order="stroke" stroke-linejoin="round"${rtl ? '' : ' letter-spacing="4"'}>${job.text}</text></g></svg>`;
      } else {
        host.innerHTML = `<svg id="s" xmlns="${ns}" width="10" height="10" style="overflow:visible"><g id="g">${job.content}</g></svg>`;
      }
      const svg = document.getElementById('s');
      let box;
      if (job.trim === false && job.size) {
        box = { x: 0, y: 0, w: job.size[0], h: job.size[1] };
      } else {
        const b = document.getElementById('g').getBBox();
        const m = job.kind === 'text' ? job.fontsize * 0.12 : 4;
        box = { x: b.x - m, y: b.y - m, w: b.width + 2 * m, h: b.height + 2 * m };
      }
      let scale = job.scale || 1;
      if (job.world && job.world[0] === 'height')
        scale = (job.world[1] * 200) / box.h;
      const pw = Math.max(1, Math.ceil(box.w * scale));
      const ph = Math.max(1, Math.ceil(box.h * scale));
      svg.setAttribute('viewBox', `${box.x} ${box.y} ${box.w} ${box.h}`);
      svg.setAttribute('width', pw);
      svg.setAttribute('height', ph);
      svg.setAttribute('preserveAspectRatio', 'none');
      svg.style.overflow = 'hidden';
      svg.style.display = 'block';
      return { x: box.x, y: box.y, w: box.w, h: box.h, pw, ph };
    }, job);
    const el = await page.$('#s');
    await el.screenshot({ path: job.out, omitBackground: true });
    boxes[job.name] = result;
  }
  await browser.close();
  process.stdout.write(JSON.stringify(boxes));
})().catch((e) => { console.error(e); process.exit(1); });
