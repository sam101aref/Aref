// Draws the launcher icon (icon.png, 512×512) with the game's own art code: Sam with the ox-head mace on lapis.
// Usage: cd Sam && npm install && npx playwright install chromium && node android/render-icon.js
const path = require('path');
const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 512, height: 512 } });
  const js = (f) => path.resolve(__dirname, '../game/js', f);
  await page.setContent('<canvas id="c" width="512" height="512"></canvas>');
  for (const f of ['i18n.js', 'art.js']) await page.addScriptTag({ path: js(f) });
  await page.evaluate(() => {
    const ctx = document.getElementById('c').getContext('2d'), P = SAM.PAL, A = SAM.Art;
    const g = ctx.createRadialGradient(256, 200, 40, 256, 256, 360);
    g.addColorStop(0, P.LAPIS); g.addColorStop(1, P.LAPIS_NIGHT);
    ctx.fillStyle = g; ctx.fillRect(0, 0, 512, 512);
    ctx.save(); ctx.globalAlpha = 0.9; A.circle(ctx, 256, 230, 170, P.SAFFRON, P.GOLD_DARK, 4); ctx.restore();
    A.goldCloud(ctx, 110, 90, 1.1); A.goldCloud(ctx, 410, 120, 0.9);
    A.figure(ctx, A.LOOKS.sam, A.POSES.standMace, 236, 700, 1, 4.6, { weapon: 'mace' });
    ctx.strokeStyle = P.GOLD; ctx.lineWidth = 18; ctx.strokeRect(9, 9, 494, 494);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 3; ctx.strokeRect(20, 20, 472, 472);
  });
  await page.locator('#c').screenshot({ path: path.join(__dirname, 'icon.png') });
  await browser.close();
})();
