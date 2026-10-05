// Checks every level map: shape, required cells, and that the goal can be reached.
// Moves follow the game's rules: walk, step off an edge (falls of 3+ rows kill), climb up one row
// from under a gap, hang from an edge and drop, a standing jump over one empty cell and a running
// jump over two. Plates open gates, and a target in clear line of sight lowers its bridge.
// Usage: node Sam/tools/check-levels.js
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const ctx = { window: {} };
vm.createContext(ctx);
vm.runInContext(fs.readFileSync(path.join(__dirname, '../game/js/levels.js'), 'utf8'), ctx);
const LEVELS = ctx.window.SAM.LEVELS;

const FLOORISH = /[_|t^~xa-cA-ChH*FSEMgKDr1-9o]/;
let failed = false;
const fail = (lv, msg) => { failed = true; console.log(`  ✗ level ${lv.id}: ${msg}`); };

for (const lv of LEVELS) {
  const map = lv.map;
  const R = map.length, W = map[0].length;
  if (R % 3) fail(lv, `rows ${R} is not a multiple of 3`);
  if (W % 10) fail(lv, `width ${W} is not a multiple of 10`);
  map.forEach((row, i) => { if (row.length !== W) fail(lv, `row ${i} has ${row.length} cells, expected ${W}`); });
  const bad = map.join('').replace(/[._#|t^~xa-cA-CwoOhH*FSEMgKDr1-9]/g, '');
  if (bad) fail(lv, `unknown cells: ${bad}`);
  const count = (ch) => map.join('').split(ch).length - 1;
  if (count('S') !== 1) fail(lv, 'needs exactly one S');
  if (count('*') !== 3) fail(lv, `has ${count('*')} leaves, expected 3`);
  const goal = count('D') ? 'D' : count('K') ? 'K' : 'E';
  if (!count(goal)) fail(lv, 'has no exit or boss');

  const ch = (r, c) => (r < 0 ? '.' : r >= R ? 'void' : c < 0 || c >= W ? '#' : map[r][c]);
  const solid = (r, c) => ch(r, c) === '#';
  // gates are walls until opened; bridges are floors only once lowered
  const blocked = (r, c, open) => solid(r, c) || (/[A-C]/.test(ch(r, c)) && !open.has(ch(r, c)));
  const floor = (r, c, open) => {
    const k = ch(r, c);
    if (k === 'O') return open.has('O');
    return FLOORISH.test(k);
  };
  const stand = (r, c, open) => r < R && !solid(r, c) && (floor(r, c, open) || solid(r + 1, c));
  const air = (r, c, open) => !blocked(r, c, open) && !stand(r, c, open);

  // fall straight down column c starting in row r; returns landing row or -1
  const fallTo = (r, c, open, maxRows) => {
    for (let rr = r; rr < R; rr++) {
      if (ch(rr, c) === 'w') return -1;
      if (stand(rr, c, open)) return rr - r <= maxRows ? rr : -1;
      if (blocked(rr, c, open)) return -1;
    }
    return -1;
  };

  let start;
  map.forEach((row, r) => { const c = row.indexOf('S'); if (c >= 0) start = [r, c]; });
  const key = (r, c, open) => `${r},${c},${[...open].sort().join('')}`;
  const seen = new Set();
  const queue = [[start[0], start[1], new Set()]];
  let reached = false;

  while (queue.length) {
    let [r, c, open] = queue.shift();
    open = new Set(open);
    const k0 = ch(r, c);
    if (/[a-c]/.test(k0)) open.add(k0.toUpperCase());
    // a target in the same row with nothing solid between can be shot
    for (let cc = 0; cc < W; cc++) {
      if (ch(r, cc) !== 'o') continue;
      let clear = true;
      for (let i = Math.min(c, cc) + 1; i < Math.max(c, cc); i++) if (blocked(r, i, open)) clear = false;
      if (clear) open.add('O');
    }
    const k = key(r, c, open);
    if (seen.has(k)) continue;
    seen.add(k);
    if (k0 === goal) { reached = true; break; }

    const push = (nr, nc) => { if (nr >= 0) queue.push([nr, nc, open]); };
    for (const d of [-1, 1]) {
      const n = c + d;
      if (stand(r, n, open)) push(r, n); // walk
      else if (air(r, n, open)) {
        { const t = fallTo(r, n, open, 2); if (t >= 0) push(t, n); } // step off
        if (!blocked(r + 1, n, open)) { const t = fallTo(r + 1, n, open, 2); if (t >= 0) push(t, n); } // hang, drop
        if (stand(r, c + 2 * d, open)) push(r, c + 2 * d); // standing jump
        if (air(r, c + 2 * d, open) && stand(r, c + 3 * d, open) && stand(r, c - d, open)) push(r, c + 3 * d); // running jump
      }
      // climb up: open cell above me, floor above-and-beside
      if (air(r - 1, c, open) && stand(r - 1, n, open) && !blocked(r - 1, n, open)) push(r - 1, n);
    }
  }
  if (!reached) {
    fail(lv, `goal ${goal} cannot be reached from S`);
    const cells = new Set([...seen].map((s) => s.split(",").slice(0, 2).join(",")));
    map.forEach((row, r) => console.log("    " + [...row].map((k, c) => (cells.has(`${r},${c}`) ? "@" : k)).join("")));
  }
  else console.log(`  ✓ level ${lv.id} (${W / 10}×${R / 3} rooms): ${goal} reachable, ${seen.size} states explored`);
}
process.exit(failed ? 1 : 0);
