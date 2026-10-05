// Plays all four chapters in headless Chromium with scripted input, using the real game code,
// and fails if Sam dies, a chapter does not finish, or the page throws.
// Usage: cd Sam && npm install && npx playwright install chromium && npm run playtest
const path = require('path');
const { chromium } = require('playwright');

const GAME = 'file://' + path.resolve(__dirname, '../game/index.html');

// Helpers that run inside the page. The game is stepped frame by frame (SAM.manual), so runs are repeatable.
function installHelpers() {
  SAM.manual = true;
  const step = () => SAM.game.update(1 / 60);
  const tap = (k) => { SAM.Input.down(k); step(); SAM.Input.up(k); };
  window.T = {
    level(i) { SAM.game.startLevel(i); Math.random = mulberry(7); },
    run(n, hold = [], taps = []) {
      hold.forEach((k) => SAM.Input.down(k)); taps.forEach((k) => SAM.Input.down(k));
      for (let i = 0; i < n; i++) { step(); if (i === 0) taps.forEach((k) => SAM.Input.up(k)); }
      hold.forEach((k) => SAM.Input.up(k));
      return T.st();
    },
    walk(x, walk) {
      const p = SAM.game.player, k = x > p.x ? 'right' : 'left';
      const keys = walk ? [k, 'walk'] : [k];
      keys.forEach((key) => SAM.Input.down(key));
      for (let i = 0; i < 900; i++) { if ((k === 'right' ? p.x >= x : p.x <= x) || p.dead || p.foe) break; step(); }
      keys.forEach((key) => SAM.Input.up(key));
      return T.st();
    },
    face(d) { const p = SAM.game.player; if (p.face !== d) T.run(2, [d > 0 ? 'right' : 'left']); return T.run(20); },
    climb() { T.run(40, [], ['up']); return T.run(40, [], ['up']); },
    runJump(d) { return T.run(70, [d > 0 ? 'right' : 'left'], ['jump']); },
    climbDown() { T.run(30, [], ['down']); return T.run(40, [], ['down']); },
    // wait for the blades to open, then dash through
    blades(col) {
      const w = SAM.game.world;
      for (let i = 0; i < 300; i++) { const ph = (w.time + col * 0.37) % 1.8; if (ph > 0.5 && ph < 0.6) break; step(); }
      return T.walk(col * 64 + 80);
    },
    // block the guard's wind-up, strike back
    fight() {
      for (let i = 0; i < 60 * 60; i++) {
        const p = SAM.game.player, g = p.foe;
        if (p.dead || SAM.game.overlay) break;
        if (!g) { if (i > 30) break; step(); continue; }
        if (p.state !== 'stand') { step(); continue; }
        if (g.state === 'wind' && g.t > (g.phase2() ? 0.1 : 0.2)) { tap('parry'); continue; }
        if (g.state === 'recoil') { tap('attack'); continue; }
        if (g.state === 'idle' && Math.abs(g.x - p.x) < 72 && i % 50 === 0) { tap('attack'); continue; }
        step();
      }
      return T.st();
    },
    // arrows into the open jaws, the mace on the stunned head, hop over fire
    dragon() {
      for (let i = 0; i < 60 * 120; i++) {
        const g = SAM.game, d = g.world.enemies.find((e) => e.isDragon), p = g.player;
        if (g.overlay || p.dead) break;
        if (p.state !== 'stand') { step(); continue; }
        const fire = g.world.entities.find((e) => e.vx < 0 && e.life && e.x - p.x < 62 && e.x - p.x > 20);
        if (fire) { tap('up'); continue; }
        if (d.state === 'stun') {
          if (p.face < 0) { tap('right'); continue; }
          if (Math.abs(p.x + 50 - (d.hx - 20)) > 40) { tap('right'); continue; }
          tap('attack'); continue;
        }
        if (d.state === 'bite' || p.x > 2745) { tap('left'); continue; }
        if (p.x < 2700) { tap('right'); continue; }
        if (d.state === 'roar' && d.t > 0.35 && p.face > 0) { tap('shoot'); continue; }
        if (p.face < 0) { tap('right'); continue; }
        step();
      }
      return T.st();
    },
    st() {
      const p = SAM.game.player;
      return { x: Math.round(p.x), y: Math.round(p.y), state: p.state, hp: p.hp, dead: p.dead, reason: p.reason, overlay: SAM.game.overlay, leaves: SAM.game.world.leavesTaken };
    },
  };
  function mulberry(a) { return function () { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
}

// Each route is a list of [helper, ...args] steps through one chapter.
const ROUTES = [
  ['The Fortress of the Forefathers', 0, [
    ['walk', 5 * 64 + 20], ['run', 20], ['climb'], ['walk', 13 * 64 + 10], ['runJump', 1],
    ['walk', 21 * 64 + 20], ['walk', 23 * 64 + 30, true], ['walk', 26 * 64 + 20], ['run', 40],
    ['walk', 27 * 64 + 59, true], ['run', 10, ['right', 'walk']], ['climbDown'],
    ['walk', 38 * 64 + 40], ['run', 80], ['walk', 46 * 64], ['fight'], ['walk', 48 * 64 + 20], ['run', 80, [], ['up']],
  ]],
  ['The Dragon of Kashafrud', 1, [
    ['walk', 3 * 64 + 20], ['run', 20], ['run', 80, [], ['shoot']], ['walk', 10 * 64 + 40], ['walk', 13 * 64 + 5],
    ['runJump', 1], ['run', 10], ['walk', 22 * 64 + 20], ['run', 20], ['climb'], ['walk', 27 * 64 + 59, true],
    ['run', 10, ['right', 'walk']], ['climbDown'], ['walk', 41 * 64], ['dragon'],
  ]],
  ['Mount Alborz', 2, [
    ['walk', 6 * 64 + 20], ['run', 20], ['climb'], ['walk', 12 * 64 + 20], ['runJump', 1],
    ['walk', 16 * 64 + 30, true], ['face', -1], ['climb'], ['walk', 12 * 64 + 6, true], ['walk', 10 * 64 + 40, true],
    ['walk', 5 * 64 + 10], ['run', 30], ['walk', 4 * 64 + 40, true], ['face', -1], ['climb'],
    ['walk', 2 * 64 + 30, true], ['face', 1], ['climb'], ['walk', 6 * 64 + 20, true], ['walk', 10 * 64 + 10], ['runJump', 1],
    ['walk', 15 * 64 + 30, true], ['face', 1], ['climb'], ['face', -1], ['climb'], ['rocks'],
    ['walk', 5 * 64 + 40], ['run', 20], ['face', -1], ['climb'], ['walk', 2 * 64 + 30, true], ['face', 1], ['climb'],
    ['walk', 9 * 64 + 20], ['runJump', 1], ['walk', 18 * 64 + 20], ['run', 80, [], ['up']],
  ]],
  ['The Demons of Mazandaran', 3, [
    ['walk', 4 * 64 + 10, true], ['blades', 5], ['walk', 11 * 64], ['fight'], ['walk', 13 * 64 + 20, true], ['face', 1], ['climb'],
    ['walk', 17 * 64 + 50, true], ['climbDown'], ['walk', 21 * 64 + 10, true], ['walk', 24 * 64 + 20], ['run', 30],
    ['walk', 26 * 64 + 30, true], ['walk', 32 * 64], ['fight'], ['walk', 34 * 64 + 20, true], ['blades', 35],
    ['walk', 40 * 64 + 20, true], ['face', 1], ['climb'], ['walk', 43 * 64 + 20, true], ['walk', 41 * 64 + 10, true], ['climbDown'],
    ['walk', 42 * 64 + 10], ['runJump', 1], ['walk', 47 * 64], ['fight'], ['walk', 56 * 64], ['fight'], ['run', 120],
  ]],
];

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 960, height: 618 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.addInitScript(() => {
    try { localStorage.setItem('sam-nariman-v1', JSON.stringify({ lang: 'en', sound: false, unlocked: 4, leaves: {}, best: {} })); } catch (e) { /* ignore */ }
  });
  await page.goto(GAME);
  await page.waitForFunction(() => window.SAM && SAM.game);
  await page.evaluate(installHelpers);
  await page.evaluate(() => {
    // Alborz: wait until both rock columns are between falls
    T.rocks = () => { for (let i = 0; i < 240; i++) { const c = SAM.game.world.cells[2]; if (c[11].timer > 2.2 && c[8].timer > 1.2) break; SAM.game.update(1 / 60); } return T.st(); };
  });

  let failed = false;
  for (const [name, index, steps] of ROUTES) {
    await page.evaluate((i) => T.level(i), index);
    let st;
    for (const [fn, ...args] of steps) {
      st = await page.evaluate(([fn, args]) => T[fn](...args), [fn, args]);
      if (st.dead) break;
    }
    const ok = !st.dead && st.overlay === 'complete';
    if (!ok) failed = true;
    console.log(`${ok ? '✓' : '✗'} ${name}: ${ok ? `finished with ${st.hp} health, ${st.leaves}/3 leaves` : JSON.stringify(st)}`);
  }
  if (errors.length) { failed = true; console.log('page errors:\n' + errors.join('\n')); }
  await browser.close();
  process.exit(failed ? 1 : 0);
})();
