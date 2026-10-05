// Props, projectiles and effects. 100 SVG units = 1 Unity unit, y down.

import { O, OT, GOLD, GOLD_DARK, STEEL, STEEL_DARK, shade, pearls, rng, asset } from './lib.mjs';

function prop(name, bbox, content, anchor, extra = {}) {
  return asset('Props/' + name, bbox, content, { anchor, ...extra });
}

// ------------------------------------------------------------------ projectiles (pivot = tip)

function arrow(fletch = '#b8262a', head = '#c8d0d8', shaft = '#c89a5a') {
  return `<path d="M-92,0 L-6,0" stroke="#2b1d14" stroke-width="5.5" stroke-linecap="round"/><path d="M-92,0 L-6,0" stroke="${shaft}" stroke-width="3.2" stroke-linecap="round"/>` +
    `<path d="M-14,-5 L0,0 L-14,5 L-11,0 Z" fill="${head}" ${OT}/>` +
    `<path d="M-94,0 L-80,-7 L-72,-7 L-82,0 L-72,7 L-80,7 Z" fill="${fletch}" ${OT}/>`;
}

function projectiles() {
  const box = [-100, -14, 6, 14];
  const tip = [0, 0];
  const p = (name, content, b = box) => asset('Projectiles/' + name, b, content, { anchor: tip });
  return [
    p('arrow', arrow()),
    p('arrow_dark', arrow('#3a3a44', '#8a9098', '#6a5a4a')),
    p('arrow_gold', arrow('#ffe9a0', '#fff2c0', GOLD) + `<path d="M-92,0 L-6,0" stroke="#fff6d0" stroke-width="1.2"/>`),
    p('arrow_fire', arrow('#3a2a1a') +
      `<path d="M-22,0 C-20,-10 -12,-14 -8,-20 C-8,-12 -2,-8 -4,0 C-6,6 -14,8 -20,5 Z" fill="#f08a1a" ${OT}/><path d="M-18,0 C-16,-6 -12,-8 -10,-12 C-10,-6 -8,-2 -10,2 Z" fill="#ffd040"/>`),
    p('spear', `<path d="M-160,0 L-20,0" stroke="#2b1d14" stroke-width="7" stroke-linecap="round"/><path d="M-160,0 L-20,0" stroke="#8a5a2c" stroke-width="4.4" stroke-linecap="round"/>` +
      `<path d="M-26,0 C-20,-7 -10,-7 0,0 C-10,7 -20,7 -26,0 Z" fill="${STEEL}" ${O}/><path d="M-30,-5 L-26,-5 L-26,5 L-30,5 Z" fill="#b02a1a"/>`, [-166, -12, 4, 12]),
    p('stone', `<circle cx="-14" cy="0" r="13" fill="#8a8680" ${O}/><path d="M-22,-4 C-18,-9 -12,-10 -8,-8" fill="none" stroke="#b8b4ac" stroke-width="2.4" stroke-linecap="round"/>`, [-30, -16, 2, 16]),
    p('boulder', `<path d="M-70,-4 C-72,-26 -52,-38 -34,-36 C-14,-34 0,-20 -2,0 C-4,22 -22,36 -40,34 C-60,32 -70,16 -70,-4 Z" fill="#8a8478" ${O}/>` +
      `<path d="M-56,-18 C-48,-28 -36,-30 -26,-26 M-46,14 C-38,20 -26,18 -18,10" fill="none" stroke="#5e5a50" stroke-width="3"/><path d="M-50,-24 C-44,-30 -36,-31 -30,-29" fill="none" stroke="#b0aa9c" stroke-width="3" stroke-linecap="round"/>`, [-76, -42, 4, 40]),
  ];
}

// ------------------------------------------------------------------ battle props

function pavise(face, rim, emblem) {
  // A standing shield seen three-quarters from the front, feet on the ground.
  let s = `<path d="M-6,0 L-10,10 M10,0 L14,10" stroke="#4a3020" stroke-width="4"/>`;
  s += `<path d="M-24,-4 L-26,-112 C-14,-126 14,-126 26,-112 L24,-4 C10,2 -10,2 -24,-4 Z" fill="${face}" ${O}/>`;
  s += `<path d="M-24,-4 L-26,-112 C-14,-126 14,-126 26,-112 L24,-4 C10,2 -10,2 -24,-4 Z" fill="none" stroke="${rim}" stroke-width="5"/>`;
  s += `<path d="M-18,-106 C-8,-114 8,-114 18,-106" fill="none" stroke="${shade(face, 0.3)}" stroke-width="3" stroke-linecap="round"/>`;
  s += emblem;
  return s;
}

function battleProps() {
  const out = [];
  out.push(prop('alert', [-22, -64, 22, 4],
    `<path d="M-12,-60 L12,-60 L7,-18 L-7,-18 Z" fill="#e02a2a" stroke="#ffffff" stroke-width="4" stroke-linejoin="round"/>` +
    `<circle cx="0" cy="-6" r="7.5" fill="#e02a2a" stroke="#ffffff" stroke-width="4"/>`, [0, 0]));
  out.push(prop('bubble', [-64, -64, 64, 64],
    `<circle cx="0" cy="0" r="58" fill="#b07ae8" fill-opacity="0.22" stroke="#d6b6ff" stroke-width="5"/>` +
    `<path d="M-30,-40 C-20,-48 -6,-52 8,-50" fill="none" stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.8"/>`, [0, 0]));
  out.push(prop('turan_shield', [-18, -62, 18, 62],
    `<ellipse cx="0" cy="0" rx="13" ry="57" fill="#7a3020" ${O}/><ellipse cx="0" cy="0" rx="13" ry="57" fill="none" stroke="${STEEL_DARK}" stroke-width="4"/>` +
    `<ellipse cx="3" cy="0" rx="6" ry="14" fill="${STEEL}" ${OT}/>` + pearls(-4, -44, -4, 44, 9, 1.6, '#e8c890'), [0, 0]));

  out.push(prop('pavise_wood', [-34, -132, 34, 14],
    pavise('#9a6a3a', '#5a3a1e', `<path d="M-20,-80 L20,-80 M-20,-46 L20,-46 M0,-118 L0,-4" stroke="#6a4424" stroke-width="2.4"/>`), [0, 0]));
  out.push(prop('pavise_bronze', [-34, -132, 34, 14],
    pavise('#b07a3a', '#e0b060', `<circle cx="0" cy="-62" r="14" fill="#d8a050" ${O}/><circle cx="0" cy="-62" r="5" fill="#8a5a24"/>` + pearls(-16, -100, 16, -100, 5, 2, '#f0d090') + pearls(-16, -20, 16, -20, 5, 2, '#f0d090')), [0, 0]));
  out.push(prop('pavise_simurgh', [-34, -132, 34, 14],
    pavise('#1f5f7a', GOLD, `<path d="M0,-90 C8,-80 22,-78 20,-64 C14,-70 8,-68 4,-62 C10,-56 12,-46 6,-36 C2,-44 -2,-44 -6,-36 C-12,-46 -10,-56 -4,-62 C-8,-68 -14,-70 -20,-64 C-22,-78 -8,-80 0,-90 Z" fill="${GOLD}" ${OT}/>` +
      pearls(-16, -104, 16, -104, 5, 2, '#fff2c0')), [0, 0]));

  // Sliced props: rock platforms and wooden covers stretch to any size.
  out.push(asset('Props/rock_platform', [0, 0, 200, 200],
    `<path d="M4,40 C4,16 30,6 60,8 C100,2 150,4 180,10 C196,16 198,30 196,44 L190,196 L10,196 Z" fill="#7d7468" ${O}/>` +
    `<path d="M4,40 C4,16 30,6 60,8 C100,2 150,4 180,10 C196,16 198,30 196,44 C150,52 50,52 4,40 Z" fill="#9a9080" ${O}/>` +
    `<path d="M140,52 L150,196 L190,196 L196,44 Z" fill="#685f54"/>` +
    `<path d="M40,80 L60,120 M120,70 L110,110 M70,150 L90,180" stroke="#5a5248" stroke-width="3"/>`,
    { scale: 1, ppu: 100, anchor: [100, 100], border: [24, 24, 24, 56] }));
  out.push(asset('Props/cover_wood', [0, 0, 100, 200],
    `<path d="M10,196 L8,18 L22,4 L36,18 L34,196 Z" fill="#8a5a2c" ${O}/><path d="M38,196 L36,26 L50,10 L64,26 L62,196 Z" fill="#9a6a34" ${O}/><path d="M66,196 L64,18 L78,4 L92,18 L90,196 Z" fill="#8a5a2c" ${O}/>` +
    `<path d="M4,70 L96,64 L96,80 L4,86 Z" fill="#6a4424" ${O}/><path d="M4,140 L96,134 L96,150 L4,156 Z" fill="#6a4424" ${O}/>`,
    { scale: 1, ppu: 100, anchor: [50, 100], border: [8, 20, 8, 40] }));
  out.push(asset('Props/palisade', [0, 0, 100, 200],
    `<path d="M4,196 L4,30 L20,6 L36,30 L36,196 Z" fill="#7a4e26" ${O}/><path d="M34,196 L34,26 L50,2 L66,26 L66,196 Z" fill="#8a5a2c" ${O}/><path d="M64,196 L64,30 L80,6 L96,30 L96,196 Z" fill="#7a4e26" ${O}/>` +
    `<path d="M2,110 L98,104 L98,120 L2,126 Z" fill="#5a3a1a" ${O}/>`,
    { scale: 1, ppu: 100, anchor: [50, 100], border: [8, 20, 8, 40] }));

  out.push(prop('horse', [-150, -170, 150, 6], horse(), [0, 0]));
  out.push(prop('target', [-30, -72, 30, 6],
    `<path d="M-4,0 L-4,-30 L4,-30 L4,0 Z" fill="#6a4424" ${OT}/><circle cx="0" cy="-44" r="26" fill="#f0e6c8" ${O}/><circle cx="0" cy="-44" r="18" fill="#c03030"/><circle cx="0" cy="-44" r="11" fill="#f0e6c8"/><circle cx="0" cy="-44" r="5" fill="#c03030"/>`, [0, -44]));
  out.push(prop('lantern', [-26, -60, 26, 34],
    `<path d="M0,-58 L0,-40" stroke="#4a3020" stroke-width="3"/><path d="M-14,-40 L14,-40 L18,-30 L18,16 L14,24 L-14,24 L-18,16 L-18,-30 Z" fill="#f0b040" ${O}/>` +
    `<path d="M-8,-30 L-8,14 M0,-30 L0,14 M8,-30 L8,14" stroke="#b07020" stroke-width="2"/><circle cx="0" cy="-8" r="22" fill="#ffd060" opacity="0.25"/>`, [0, 0]));
  out.push(prop('apple', [-18, -22, 18, 18],
    `<path d="M0,-8 C-10,-16 -18,-6 -16,4 C-14,14 -6,18 0,14 C6,18 14,14 16,4 C18,-6 10,-16 0,-8 Z" fill="#d02828" ${O}/><path d="M0,-8 L2,-18" stroke="#4a3020" stroke-width="2.5"/><path d="M2,-16 C8,-20 12,-16 12,-14 C8,-12 4,-12 2,-16 Z" fill="#4a8a3a"/>`, [0, 0]));
  return out;
}

function horse() {
  const c = '#7a4a2a';
  const d = shade(c, -0.25);
  let s = '';
  // Far legs
  s += `<path d="M-74,-60 L-82,-4 L-70,-4 L-60,-60 Z M56,-60 L60,-4 L72,-4 L70,-60 Z" fill="${d}" ${O}/>`;
  // Tail
  s += `<path d="M-96,-110 C-124,-100 -132,-70 -128,-40 C-116,-60 -108,-80 -94,-96 Z" fill="#2a1a12" ${O}/>`;
  // Body and neck
  s += `<path d="M-98,-112 C-100,-136 -70,-140 -30,-136 C10,-134 50,-136 70,-130 C86,-150 98,-168 116,-166 C132,-166 144,-150 146,-136 L140,-128 C130,-130 120,-128 114,-120 C104,-104 96,-90 84,-74 C60,-60 0,-58 -60,-62 C-88,-66 -98,-84 -98,-112 Z" fill="${c}" ${O}/>`;
  s += `<path d="M-60,-72 C-20,-66 30,-66 70,-76" fill="none" stroke="${d}" stroke-width="4"/>`;
  // Mane, eye, ear
  s += `<path d="M70,-130 C80,-150 94,-166 112,-168 L104,-156 L96,-150 L88,-140 L80,-128 Z" fill="#2a1a12" ${O}/>`;
  s += `<circle cx="126" cy="-152" r="3" fill="#1a0e08"/><path d="M110,-166 L114,-182 L120,-166 Z" fill="${c}" ${O}/>`;
  // Saddle cloth with pearls
  s += `<path d="M-40,-138 C-20,-140 20,-140 30,-136 L26,-96 C0,-92 -24,-92 -44,-98 Z" fill="#a8262c" ${O}/>` + pearls(-38, -100, 24, -100, 7, 2, GOLD);
  // Near legs
  s += `<path d="M-60,-66 L-58,-4 L-44,-4 L-44,-66 Z M70,-70 L84,-4 L98,-4 L86,-72 Z" fill="${c}" ${O}/>`;
  s += `<path d="M-60,-8 L-42,-8 L-42,0 L-62,0 Z M82,-8 L100,-8 L100,0 L80,0 Z" fill="#2a1a12"/>`;
  return s;
}

// ------------------------------------------------------------------ scenery for cutscenes

function scenery() {
  const out = [];
  out.push(prop('tent_iran', [-130, -190, 130, 6], tentShape('#e8dcc0', '#1f6f86', GOLD), [0, 0]));
  out.push(prop('tent_turan', [-130, -190, 130, 6], tentShape('#3a2a24', '#8a2a1a', '#c8a060'), [0, 0]));
  out.push(prop('campfire', [-50, -90, 50, 8],
    `<path d="M-36,-4 L30,-14 M-30,-14 L36,-4" stroke="#5a3418" stroke-width="9" stroke-linecap="round"/>` +
    `<path d="M-24,-8 C-28,-36 -10,-48 -6,-76 C4,-56 22,-48 22,-24 C22,-12 12,-6 0,-6 C-12,-6 -22,-8 -24,-8 Z" fill="#f07a1a" ${OT}/>` +
    `<path d="M-12,-10 C-14,-28 -4,-36 -2,-52 C6,-38 14,-32 12,-18 C10,-10 0,-8 -12,-10 Z" fill="#ffc040"/>`, [0, 0]));
  out.push(prop('banner_iran', [-20, -330, 130, 6], banner('#7a2a8a', GOLD, true), [0, 0]));
  out.push(prop('banner_turan', [-20, -330, 130, 6], banner('#2a2a2a', '#c03030', false), [0, 0]));
  out.push(prop('cypress', [-50, -330, 50, 6],
    `<path d="M-6,0 L-6,-40 L6,-40 L6,0 Z" fill="#4a3020" ${OT}/>` +
    `<path d="M0,-326 C30,-280 40,-160 32,-90 C26,-50 14,-36 0,-34 C-14,-36 -26,-50 -32,-90 C-40,-160 -30,-280 0,-326 Z" fill="#2f5a3a" ${O}/>` +
    `<path d="M4,-300 C20,-250 24,-160 18,-90" fill="none" stroke="#467a50" stroke-width="5" stroke-linecap="round"/>`, [0, 0]));
  out.push(prop('oak', [-150, -330, 150, 6], oakShape(), [0, 0]));
  out.push(prop('house', [-110, -260, 110, 6], houseShape(), [0, 0]));
  out.push(prop('throne', [-70, -200, 70, 6],
    `<path d="M-50,0 L-50,-90 L50,-90 L50,0 Z" fill="${GOLD_DARK}" ${O}/><path d="M-40,-90 L-40,-190 C-20,-200 20,-200 40,-190 L40,-90 Z" fill="#7a2a8a" ${O}/>` +
    `<path d="M-60,-96 L60,-96 L60,-84 L-60,-84 Z" fill="${GOLD}" ${O}/><path d="M-44,-188 C-20,-204 20,-204 44,-188" fill="none" stroke="${GOLD}" stroke-width="7"/>` +
    pearls(-30, -150, 30, -150, 6, 3, GOLD) + `<circle cx="0" cy="-176" r="9" fill="${GOLD}" ${OT}/>`, [0, 0]));
  out.push(prop('scroll', [-40, -26, 40, 26],
    `<path d="M-30,-16 L30,-16 L30,16 L-30,16 Z" fill="#efe2bc" ${O}/><path d="M-36,-20 C-30,-20 -26,-16 -26,0 C-26,16 -30,20 -36,20 C-40,20 -40,-20 -36,-20 Z" fill="#d8c898" ${O}/>` +
    `<path d="M-18,-6 L20,-6 M-18,2 L14,2 M-18,10 L18,10" stroke="#8a7a5a" stroke-width="2.5"/><circle cx="22" cy="12" r="6" fill="#b02a2a" ${OT}/>`, [0, 0]));
  out.push(prop('sun', [-110, -110, 110, 110],
    `<circle cx="0" cy="0" r="100" fill="#fff0b0" opacity="0.35"/><circle cx="0" cy="0" r="62" fill="#ffe08a" opacity="0.6"/><circle cx="0" cy="0" r="44" fill="#fff6d8"/>`, [0, 0]));
  out.push(prop('moon', [-90, -90, 90, 90],
    `<circle cx="0" cy="0" r="80" fill="#e8f0ff" opacity="0.18"/><circle cx="0" cy="0" r="40" fill="#f4f6ff"/><circle cx="-10" cy="-8" r="8" fill="#dfe4f0"/><circle cx="12" cy="10" r="6" fill="#dfe4f0"/>`, [0, 0]));
  out.push(prop('cloud', [-160, -80, 160, 20],
    `<path d="M-150,10 C-160,-14 -130,-34 -100,-26 C-96,-60 -50,-74 -20,-54 C0,-82 60,-80 74,-46 C110,-60 156,-36 150,10 Z" fill="#ffffff" opacity="0.92"/><path d="M-150,10 L150,10" stroke="#e8eef6" stroke-width="8"/>`, [0, 0]));
  out.push(prop('damavand', [-700, -720, 700, 10], damavandShape(), [0, 0], { scale: 0.64, ppu: 64 }));
  out.push(prop('simurgh', [-260, -200, 260, 120], simurghShape(), [0, 0]));
  out.push(prop('chest', [-60, -90, 60, 6],
    `<path d="M-50,0 L-50,-50 L50,-50 L50,0 Z" fill="#8a5a2c" ${O}/><path d="M-50,-50 C-50,-84 50,-84 50,-50 Z" fill="#a06a34" ${O}/>` +
    `<path d="M-50,-50 L50,-50 M-30,-74 L-30,0 M30,-74 L30,0" stroke="${GOLD}" stroke-width="6"/><path d="M-9,-58 L9,-58 L9,-36 L-9,-36 Z" fill="${GOLD}" ${OT}/>`, [0, 0]));
  return out;
}

function tentShape(a, b, trim) {
  let s = `<path d="M-120,0 L0,-170 L120,0 Z" fill="${a}" ${O}/>`;
  s += `<path d="M0,-170 L-60,0 L-30,0 Z M0,-170 L30,0 L60,0 Z" fill="${b}"/>`;
  s += `<path d="M-120,0 L0,-170 L120,0 Z" fill="none" ${O}/>`;
  s += `<path d="M-18,0 L0,-80 L18,0 Z" fill="#2a1a12"/>`;
  s += `<path d="M-100,-24 C-50,-16 50,-16 100,-24" fill="none" stroke="${trim}" stroke-width="6"/>`;
  s += `<path d="M0,-170 L0,-188 L22,-182 L0,-176" fill="${b}" ${OT}/>`;
  return s;
}

function banner(cloth, accent, star) {
  let s = `<path d="M0,0 L0,-326" stroke="#4a3020" stroke-width="7"/><circle cx="0" cy="-326" r="6" fill="${GOLD}" ${OT}/>`;
  s += `<path d="M2,-316 L110,-300 L96,-262 L110,-224 L2,-238 Z" fill="${cloth}" ${O}/>`;
  if (star)
    s += `<path d="M48,-292 L53,-279 L67,-279 L56,-270 L60,-257 L48,-265 L36,-257 L40,-270 L29,-279 L43,-279 Z" fill="${accent}" ${OT}/>`;
  else
    s += `<path d="M30,-292 C50,-300 70,-288 66,-270 C56,-280 42,-282 30,-276 Z" fill="${accent}" ${OT}/>`;
  s += pearls(10, -244, 96, -232, 7, 2.2, accent);
  return s;
}

function oakShape() {
  let s = `<path d="M-14,0 C-12,-60 -16,-110 -30,-150 L-10,-150 C0,-120 2,-100 6,-80 C12,-110 24,-130 40,-150 L56,-146 C34,-120 20,-80 18,0 Z" fill="#4a3426" ${O}/>`;
  const blobs = [[-70, -190, 70], [0, -240, 86], [76, -196, 66], [-20, -170, 60], [40, -160, 56]];
  for (const [x, y, r] of blobs)
    s += `<circle cx="${x}" cy="${y}" r="${r}" fill="${shade('#3e6e3e', (x % 3) * 0.05)}" ${O}/>`;
  s += `<circle cx="-20" cy="-270" r="26" fill="#5a8a50"/><circle cx="-80" cy="-216" r="18" fill="#5a8a50"/>`;
  return s;
}

function houseShape() {
  let s = `<path d="M-100,0 L-100,-150 L100,-150 L100,0 Z" fill="#c99a63" ${O}/>`;
  s += `<path d="M-108,-150 L108,-150 L108,-166 L-108,-166 Z" fill="#a87a48" ${O}/>`;
  s += `<path d="M-60,-166 C-60,-250 60,-250 60,-166 Z" fill="#b88550" ${O}/>`;
  s += `<path d="M-24,0 L-24,-70 C-24,-100 24,-100 24,-70 L24,0 Z" fill="#4a3020" ${O}/>`;
  s += `<path d="M-80,-90 L-80,-120 C-80,-136 -56,-136 -56,-120 L-56,-90 Z M56,-90 L56,-120 C56,-136 80,-136 80,-120 L80,-90 Z" fill="#4a3020"/>`;
  s += pearls(-90, -158, 90, -158, 10, 2.4, '#efd8a8');
  return s;
}

function damavandShape() {
  let s = `<path d="M-690,0 L-120,-660 C-80,-700 60,-700 110,-660 L690,0 Z" fill="#8a8f9e" ${O}/>`;
  s += `<path d="M110,-660 L690,0 L120,0 Z" fill="#747a8a"/>`;
  s += `<path d="M-230,-530 L-120,-660 C-80,-700 60,-700 110,-660 L240,-520 L170,-540 L110,-500 L40,-560 L-40,-510 L-120,-560 Z" fill="#f4f7fb"/>`;
  s += `<path d="M-60,-690 C-40,-720 30,-720 50,-690" fill="none" stroke="#c8cdd8" stroke-width="8"/>`;
  return s;
}

function simurghShape() {
  // The Simurgh in flight: peacock-like tail, wide wings, crest.
  let s = '';
  s += `<path d="M-60,10 C-120,30 -200,60 -250,110 C-190,100 -130,84 -80,60 C-130,100 -170,120 -200,118 C-130,126 -70,96 -40,50 Z" fill="#2a8a7a" ${O}/>`;
  for (let i = 0; i < 5; i++)
    s += `<circle cx="${-200 + i * 30}" cy="${100 - i * 14}" r="9" fill="${GOLD}" ${OT}/><circle cx="${-200 + i * 30}" cy="${100 - i * 14}" r="4" fill="#1f4f7a"/>`;
  s += `<path d="M-40,0 C-80,-90 -160,-150 -240,-170 C-180,-120 -150,-80 -140,-30 C-110,-40 -80,-30 -60,-10 Z" fill="#c8402a" ${O}/>`;
  s += `<path d="M-200,-150 C-160,-110 -140,-80 -130,-46 M-170,-160 C-130,-120 -110,-80 -100,-40" fill="none" stroke="${GOLD}" stroke-width="5"/>`;
  s += `<path d="M-60,-10 C-20,-30 40,-40 90,-30 C120,-40 140,-60 170,-60 C190,-60 200,-46 196,-36 L222,-30 L196,-24 C190,-10 170,-4 150,-6 C120,-6 100,10 70,24 C20,40 -40,36 -60,-10 Z" fill="#e0a030" ${O}/>`;
  s += `<path d="M20,0 C60,-160 140,-220 230,-210 C170,-160 140,-100 120,-30 C90,-40 50,-30 20,0 Z" fill="#d84a30" ${O}/>`;
  s += `<path d="M60,-60 C100,-130 150,-180 210,-196 M90,-40 C120,-100 160,-140 200,-160" fill="none" stroke="${GOLD}" stroke-width="5"/>`;
  s += `<path d="M164,-62 C170,-90 186,-100 196,-110 C190,-90 186,-76 180,-60 Z" fill="#2a8a7a" ${OT}/>`;
  s += `<circle cx="180" cy="-46" r="4" fill="#1a0e08"/>`;
  return s;
}

// ------------------------------------------------------------------ effects

function effects() {
  const box = [-40, -40, 40, 40];
  const c = [0, 0];
  return [
    asset('Fx/spark', box, `<path d="M0,-36 L8,-8 L36,0 L8,8 L0,36 L-8,8 L-36,0 L-8,-8 Z" fill="#fff2b0"/><circle cx="0" cy="0" r="10" fill="#ffffff"/>`, { anchor: c }),
    asset('Fx/puff', box, `<circle cx="-12" cy="6" r="18" fill="#ffffff" opacity="0.8"/><circle cx="10" cy="0" r="22" fill="#ffffff" opacity="0.8"/><circle cx="0" cy="-14" r="16" fill="#ffffff" opacity="0.8"/>`, { anchor: c }),
    asset('Fx/heal', box, `<path d="M-8,-30 L8,-30 L8,-8 L30,-8 L30,8 L8,8 L8,30 L-8,30 L-8,8 L-30,8 L-30,-8 L-8,-8 Z" fill="#5ad06a" stroke="#ffffff" stroke-width="4"/>`, { anchor: c }),
    asset('Fx/bolt', [-14, -100, 14, 0], `<path d="M4,-98 L-10,-52 L2,-52 L-6,-2 L12,-60 L0,-60 L10,-98 Z" fill="#fff6a0" stroke="#a0d8ff" stroke-width="3" stroke-linejoin="round"/>`, { anchor: [0, -50] }),
    asset('Fx/rain', box, `<path d="M-20,-30 L-26,-10 M0,-34 L-6,-14 M20,-30 L14,-10 M-10,0 L-16,20 M10,0 L4,20" stroke="#8ad0ff" stroke-width="4" stroke-linecap="round"/>`, { anchor: c }),
    asset('Fx/flame', [-30, -70, 30, 6], `<path d="M-24,0 C-28,-28 -10,-40 -6,-66 C4,-46 22,-40 22,-16 C22,-6 12,0 0,0 C-12,0 -22,0 -24,0 Z" fill="#f07a1a"/><path d="M-12,-2 C-14,-20 -4,-28 -2,-44 C6,-30 14,-24 12,-10 C10,-2 0,0 -12,-2 Z" fill="#ffc040"/>`, { anchor: [0, 0] }),
    asset('Fx/glow', [-64, -64, 64, 64], `<defs><radialGradient id="g"><stop offset="0" stop-color="#ffffff" stop-opacity="1"/><stop offset="1" stop-color="#ffffff" stop-opacity="0"/></radialGradient></defs><circle cx="0" cy="0" r="62" fill="url(#g)"/>`, { anchor: c }),
  ];
}

export function propAssets() {
  return [...projectiles(), ...battleProps(), ...scenery(), ...effects()];
}
