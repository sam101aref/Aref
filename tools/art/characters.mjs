// Characters: every look is drawn as separate body parts so the game can animate and ragdoll them.
// Coordinates: 100 SVG units = 1 Unity unit, y down, feet at (0, 0), facing right.
// Parts marked "tinted" are drawn in greys and coloured at runtime (outfits, enemy colours).

import { O, OT, G, GOLD, GOLD_DARK, STEEL, STEEL_DARK, shade, pearls, asset } from './lib.mjs';

const SKIN = { light: '#e6b48a', tan: '#c98e63', dark: '#a56a43', div: '#5e8f7c', white: '#e4ebef' };

// Pivot points of the parts in the game's archer prefab (see BattleSceneBuilder).
export const ANCHORS = {
  legs: [0, -40],
  torso: [0, -125],
  head: [0, -195],
  shoulder: [10, -155],
};

const BOX = {
  legs: [-48, -98, 52, 6],
  torso: [-62, -188, 62, -4],
  cape: [-78, -184, 34, -8],
  back: [-72, -206, 40, -60],
  head: [-46, -236, 46, -158],
  hat: [-56, -292, 58, -160],
  arm: [-16, -16, 70, 16],
  weapon: [-62, -80, 132, 80],
};

// ------------------------------------------------------------------ looks

export const LOOKS = {
  arash: { skin: SKIN.light, hair: '#2a1810', beard: 'short', hat: 'parthian', torso: 'tunic', trim: GOLD, belt: '#5b3a1f', trousers: '#2a3557', boots: '#5a3418', cape: true, back: 'quiver', weapon: 'bow', bow: '#6e3f1c' },
  roshana: { skin: SKIN.light, hair: '#3a2214', female: true, hat: 'scarf', torso: 'dress', trim: GOLD, belt: '#6b3b22', trousers: '#4a2f3f', boots: '#5a3418', back: 'quiver', weapon: 'bow', bow: '#7a4822' },
  mobad: { skin: SKIN.tan, hair: '#efefea', beard: 'long', old: true, hat: 'mobadcap', torso: 'robe', trim: GOLD, belt: '#c9b37a', trousers: '#e8e2d0', boots: '#7a5a3a', weapon: 'staff' },
  manuchehr: { skin: SKIN.tan, hair: '#1f140d', beard: 'long', hat: 'crown', torso: 'robe', trim: GOLD, belt: GOLD_DARK, trousers: '#3b2a5e', boots: '#5a2a18', cape: true, weapon: 'none' },
  afrasiab: { skin: SKIN.tan, hair: '#120c08', beard: 'long', hat: 'darkcrown', torso: 'robe', trim: '#b02a2a', belt: '#2a1a14', trousers: '#2b2024', boots: '#2a1a14', cape: true, weapon: 'sword' },
  villager: { skin: SKIN.tan, hair: '#3b2616', beard: 'mustache', hat: 'cloth', torso: 'tunic', trim: '#8a6a3a', belt: '#6b4a2a', trousers: '#5c4a36', boots: '#4a3020', weapon: 'none' },
  envoy: { skin: SKIN.tan, hair: '#2a1a10', beard: 'short', hat: 'turban', torso: 'robe', trim: GOLD, belt: '#8a5a2a', trousers: '#4a3a5a', boots: '#4a3020', weapon: 'none' },
  turanian: { skin: SKIN.tan, hair: '#1a110b', beard: 'droopy', hat: 'furhat', torso: 'tunic', trim: '#4a3426', fur: true, belt: '#2a1a12', trousers: '#3a2a22', boots: '#2a1a12', back: 'quiver', weapon: 'bow', bow: '#4a2a14' },
  shieldbearer: { skin: SKIN.tan, hair: '#1a110b', beard: 'short', hat: 'conical', torso: 'tunic', trim: '#5a4030', armor: 'scale', belt: '#2a1a12', trousers: '#3a2a22', boots: '#2a1a12', weapon: 'bow', bow: '#4a2a14' },
  spearman: { skin: SKIN.dark, hair: '#1a110b', beard: 'mustache', hat: 'leather', torso: 'tunic', trim: '#6a4a2a', belt: '#3a2414', trousers: '#4a3626', boots: '#2a1a12', weapon: 'spear' },
  rider: { skin: SKIN.tan, hair: '#1a110b', beard: 'droopy', hat: 'furhat', torso: 'tunic', trim: '#4a3426', fur: true, belt: '#2a1a12', trousers: '#3a2a22', boots: '#2a1a12', cape: true, weapon: 'bow', bow: '#4a2a14' },
  slinger: { skin: SKIN.dark, hair: '#2a1a10', beard: 'none', hat: 'headband', torso: 'vest', trim: '#7a5a2a', belt: '#4a2a14', trousers: '#5a4a3a', boots: '#3a2414', weapon: 'sling' },
  firearcher: { skin: SKIN.tan, hair: '#1a110b', beard: 'short', hat: 'hood', hood: '#7a2414', torso: 'tunic', trim: '#e07a1a', belt: '#2a1a12', trousers: '#3a2018', boots: '#2a1a12', back: 'quiver', weapon: 'firebow', bow: '#3a1a0c' },
  assassin: { skin: SKIN.tan, hair: '#0e0a08', beard: 'none', hat: 'maskhood', hood: '#26232c', torso: 'tunic', trim: '#4a4652', belt: '#141216', trousers: '#1e1c22', boots: '#141216', weapon: 'darkbow', bow: '#1e1a1a' },
  raider: { skin: SKIN.dark, hair: '#1a110b', beard: 'full', hat: 'headband', torso: 'vest', trim: '#6a2a1a', fur: true, belt: '#2a1a12', trousers: '#3a2a22', boots: '#2a1a12', weapon: 'sword' },
  shaman: { skin: SKIN.tan, hair: '#1a110b', beard: 'short', paint: true, hat: 'horns', torso: 'robe', trim: '#7a3ab0', fur: true, belt: '#3a2414', trousers: '#2a2030', boots: '#2a1a12', weapon: 'orbstaff' },
  commander: { skin: SKIN.tan, hair: '#1a110b', beard: 'full', hat: 'goldhelm', torso: 'tunic', trim: GOLD, armor: 'scale', belt: '#2a1a12', trousers: '#3a2a22', boots: '#2a1a12', cape: true, weapon: 'bow', bow: '#3a1a0c' },
  barman: { skin: SKIN.dark, hair: '#120c08', beard: 'full', hat: 'hornhelm', torso: 'tunic', trim: '#6a4a2a', armor: 'heavy', fur: true, belt: '#2a1a12', trousers: '#2a2020', boots: '#1a1210', cape: true, weapon: 'bow', bow: '#2a1a0c' },
  garsivaz: { skin: SKIN.tan, hair: '#120c08', beard: 'pointed', hat: 'tallturban', torso: 'robe', trim: '#c9a227', belt: '#2a1a12', trousers: '#2a1a30', boots: '#1a1210', cape: true, weapon: 'firebow', bow: '#2a1428' },
  div: { monster: true, skin: SKIN.div, fur: '#4a3a2a', horn: '#e8dcc0', weapon: 'boulder' },
  whitediv: { monster: true, skin: SKIN.white, fur: '#c8ccd2', horn: '#3a3440', weapon: 'boulder' },
};

// ------------------------------------------------------------------ legs

function boot(x, color, dark) {
  return `<path d="M${x - 9},-22 L${x + 9},-22 L${x + 11},-9 C${x + 19},-9 ${x + 26},-8 ${x + 28},-4 C${x + 30},-8 ${x + 32},-10 ${x + 30},-13 C${x + 34},-8 ${x + 32},0 ${x + 26},0 L${x - 10},0 Z" fill="${color}" ${O}/>` +
    `<path d="M${x - 9},-22 L${x + 9},-22 L${x + 9},-18 L${x - 9},-18 Z" fill="${dark}"/>`;
}

function legs(l) {
  if (l.monster)
    return monsterLegs(l);
  const t = l.trousers;
  const back = shade(t, -0.28);
  let s = '';
  // Back leg, then front leg: baggy Parthian trousers gathered at the ankle.
  s += `<path d="M-20,-88 C-28,-62 -24,-38 -18,-20 L-2,-20 C-2,-44 0,-66 4,-88 Z" fill="${back}" ${O}/>`;
  s += boot(-10, shade(l.boots, -0.25), shade(l.boots, -0.5));
  s += `<path d="M-6,-88 C-4,-62 0,-40 2,-20 L20,-20 C22,-44 22,-66 22,-88 Z" fill="${t}" ${O}/>`;
  s += `<path d="M6,-70 C8,-56 10,-44 10,-30 M14,-80 C16,-64 16,-50 16,-36" fill="none" stroke="${shade(t, -0.35)}" stroke-width="2"/>`;
  s += boot(10, l.boots, shade(l.boots, -0.35));
  return s;
}

function monsterLegs(l) {
  const skin = l.skin;
  const dark = shade(skin, -0.3);
  let s = '';
  s += `<path d="M-22,-90 C-30,-60 -26,-34 -20,-14 L-4,-14 C-4,-40 -2,-64 2,-90 Z" fill="${dark}" ${O}/>`;
  s += `<path d="M-22,-16 L-2,-16 L4,-2 L-26,0 Z" fill="${dark}" ${O}/>`;
  s += `<path d="M-6,-90 C-2,-60 2,-36 4,-14 L22,-14 C24,-40 22,-66 22,-90 Z" fill="${skin}" ${O}/>`;
  s += `<path d="M2,-16 L24,-16 L34,-4 L36,0 L0,0 Z" fill="${skin}" ${O}/>`;
  // Claws
  s += `<path d="M26,-3 L32,1 M30,-6 L37,-2" stroke="${l.horn}" stroke-width="3" stroke-linecap="round"/>`;
  // Fur loincloth
  s += `<path d="M-30,-96 L30,-96 L28,-62 L18,-70 L10,-58 L2,-70 L-8,-58 L-16,-70 L-26,-60 Z" fill="${l.fur}" ${O}/>`;
  return s;
}

// ------------------------------------------------------------------ torso

const TORSO_SHAPES = {
  tunic: 'M-24,-174 C-10,-180 10,-180 24,-174 C30,-160 30,-146 26,-130 L22,-114 C30,-100 36,-88 38,-74 C14,-68 -14,-68 -36,-74 C-34,-88 -28,-100 -22,-114 L-26,-130 C-30,-146 -30,-160 -24,-174 Z',
  robe: 'M-24,-174 C-10,-180 10,-180 24,-174 C30,-160 30,-146 26,-130 L22,-114 C30,-80 36,-44 42,-12 C14,-6 -14,-6 -40,-12 C-36,-44 -28,-80 -22,-114 L-26,-130 C-30,-146 -30,-160 -24,-174 Z',
  dress: 'M-22,-174 C-10,-179 10,-179 22,-174 C28,-160 27,-146 23,-130 L19,-116 C28,-96 36,-78 40,-56 C14,-50 -14,-50 -38,-56 C-34,-78 -26,-96 -19,-116 L-23,-130 C-27,-146 -28,-160 -22,-174 Z',
  vest: 'M-24,-174 C-10,-180 10,-180 24,-174 C30,-160 30,-146 26,-130 L22,-114 C26,-106 30,-98 32,-90 C10,-86 -10,-86 -30,-90 C-28,-98 -24,-106 -22,-114 L-26,-130 C-30,-146 -30,-160 -24,-174 Z',
};

const TORSO_HEMS = {
  tunic: { y: -74, w: 37 },
  robe: { y: -12, w: 41 },
  dress: { y: -56, w: 39 },
  vest: { y: -90, w: 31 },
};

/** The tunic itself, in greys (tinted at runtime). */
function torsoBase(l) {
  if (l.monster)
    return monsterTorso(l);
  const shape = TORSO_SHAPES[l.torso];
  const hem = TORSO_HEMS[l.torso];
  let s = `<path d="${shape}" fill="${G.base}" ${O}/>`;
  // Shadow on the back half and under the belt.
  s += `<path d="M-24,-172 C-30,-158 -29,-144 -25,-130 L-21,-114 C-27,-100 -${hem.w - 4},${hem.y + 20} -${hem.w - 1},${hem.y} C-24,${hem.y + 3} -16,${hem.y + 4} -8,${hem.y + 4} C-10,${hem.y - 20} -10,-110 -8,-132 C-8,-150 -12,-164 -24,-172 Z" fill="${G.shade}"/>`;
  s += `<path d="M10,-176 C16,-160 18,-146 16,-128" fill="none" stroke="${G.light}" stroke-width="4" stroke-linecap="round"/>`;
  if (l.torso !== 'vest') {
    s += `<path d="M2,-108 L-2,${hem.y + 2} M16,-108 L${hem.w - 14},${hem.y + 3} M-12,-108 L-${hem.w - 12},${hem.y + 2}" fill="none" stroke="${G.line}" stroke-width="1.8"/>`;
  } else {
    // A plain shirt shows at the arms and below the vest.
    s += `<path d="M-22,-112 C-24,-100 -26,-90 -28,-82 L28,-82 C26,-90 24,-100 22,-112 Z" fill="${G.shade}" ${OT}/>`;
  }
  s += `<path d="${shape}" fill="none" ${O}/>`;
  return s;
}

function monsterTorso(l) {
  const skin = l.skin;
  let s = `<path d="M-30,-176 C-12,-186 14,-186 32,-176 C40,-160 38,-140 30,-124 C28,-112 26,-102 24,-92 L-24,-92 C-26,-104 -30,-116 -34,-128 C-40,-146 -40,-164 -30,-176 Z" fill="${skin}" ${O}/>`;
  // Muscles
  const line = shade(skin, -0.35);
  s += `<path d="M4,-162 C14,-158 22,-150 26,-142 M2,-140 C2,-128 4,-114 6,-100 M14,-128 C20,-124 22,-116 22,-108 M-10,-130 C-14,-120 -14,-110 -12,-100" fill="none" stroke="${line}" stroke-width="2.4" stroke-linecap="round"/>`;
  s += `<path d="M-30,-176 C-38,-160 -38,-144 -32,-128 C-24,-140 -20,-160 -22,-178 Z" fill="${shade(skin, -0.2)}"/>`;
  // Fur over the shoulder
  s += `<path d="M-34,-178 C-20,-192 10,-190 30,-178 L24,-166 L16,-174 L8,-164 L0,-174 L-8,-164 L-16,-174 L-24,-162 Z" fill="${l.fur}" ${O}/>`;
  return s;
}

/** Belt, trims and ornaments in fixed colours, drawn over the tinted tunic. */
function torsoDetail(l) {
  if (l.monster)
    return `<path d="M-26,-96 L26,-96 L26,-88 L-26,-88 Z" fill="${shade(l.fur, -0.3)}" ${O}/>`;
  const hem = TORSO_HEMS[l.torso];
  let s = '';
  // Hem band with pearls
  s += `<path d="M-${hem.w - 1},${hem.y - 1} C-14,${hem.y + 5} 14,${hem.y + 5} ${hem.w},${hem.y - 1}" fill="none" stroke="${l.trim}" stroke-width="6"/>`;
  s += pearls(-hem.w + 6, hem.y + 1, hem.w - 6, hem.y + 1, 7, 1.4, '#fff6dc');
  // Collar and front opening
  if (l.fur) {
    s += `<path d="M-26,-176 C-14,-186 12,-186 26,-176 C22,-166 12,-162 0,-162 C-12,-162 -22,-166 -26,-176 Z" fill="${l.trim}" ${O}/>`;
    s += `<path d="M-18,-174 l3,-5 M-8,-176 l2,-6 M4,-176 l1,-6 M14,-174 l2,-5" stroke="${shade(l.trim, -0.4)}" stroke-width="2"/>`;
  } else {
    s += `<path d="M2,-178 L14,-152 L24,-176" fill="none" stroke="${l.trim}" stroke-width="4" stroke-linejoin="round"/>`;
    s += `<path d="M14,-152 L12,-118" fill="none" stroke="${l.trim}" stroke-width="3.5"/>`;
  }
  // Belt with a gold buckle
  s += `<path d="M-23,-118 C-8,-114 8,-114 23,-118 L23,-108 C8,-104 -8,-104 -23,-108 Z" fill="${l.belt}" ${O}/>`;
  s += `<rect x="12" y="-118" width="8" height="10" rx="1.5" fill="${GOLD}" ${OT}/>`;
  if (l.torso === 'robe')
    s += pearls(14, -100, 30, -24, 6, 1.6, l.trim);
  if (l.armor)
    s += armor(l.armor);
  return s;
}

/** Armour plates over the chest; also used for the player's armour items. */
export function armor(kind) {
  let s = '';
  if (kind === 'leather') {
    s += `<path d="M-24,-172 C-10,-178 12,-178 24,-172 C28,-156 27,-140 24,-122 L-22,-122 C-26,-140 -28,-156 -24,-172 Z" fill="#7a4a26" ${O}/>`;
    s += `<path d="M-18,-160 L18,-160 M-20,-146 L20,-146 M-20,-132 L20,-132" stroke="#4a2a14" stroke-width="1.6" stroke-dasharray="3 3"/>`;
    s += `<path d="M-6,-178 L-4,-122 M8,-178 L10,-122" stroke="#5a3418" stroke-width="2"/>`;
    return s;
  }
  const plate = kind === 'immortal' ? '#d9b04a' : kind === 'heavy' ? '#8d959e' : STEEL;
  const plateDark = kind === 'immortal' ? '#8a6a1a' : STEEL_DARK;
  const rows = kind === 'heavy' ? 6 : 5;
  s += `<path d="M-25,-172 C-10,-178 12,-178 25,-172 C29,-156 28,-140 25,-120 L-23,-120 C-27,-140 -29,-156 -25,-172 Z" fill="${plateDark}" ${O}/>`;
  for (let r = 0; r < rows; r++) {
    const y = -170 + r * (48 / rows);
    for (let c = 0; c < 7; c++) {
      const x = -21 + c * 7 + (r % 2) * 3.5;
      if (x > 23)
        continue;
      s += `<path d="M${x - 3.4},${y} L${x + 3.4},${y} L${x + 3.4},${y + 5} C${x + 3.4},${y + 8} ${x - 3.4},${y + 8} ${x - 3.4},${y + 5} Z" fill="${plate}" stroke="${plateDark}" stroke-width="0.9"/>`;
    }
  }
  if (kind === 'immortal') {
    s += `<circle cx="4" cy="-148" r="8" fill="${GOLD}" ${OT}/><circle cx="4" cy="-148" r="3.5" fill="#2a8a8a"/>`;
    s += `<path d="M-25,-172 C-10,-178 12,-178 25,-172" fill="none" stroke="${GOLD}" stroke-width="4"/>`;
  }
  if (kind === 'heavy')
    s += `<path d="M-30,-176 C-36,-168 -36,-160 -30,-154 L-18,-160 L-20,-176 Z" fill="${STEEL}" ${O}/><path d="M20,-176 C30,-174 34,-166 32,-158 L22,-160 Z" fill="${STEEL}" ${O}/>`;
  return s;
}

function cape(l) {
  let s = `<path d="M-18,-178 C-30,-172 -38,-152 -42,-122 C-46,-92 -52,-62 -62,-26 C-46,-20 -30,-20 -16,-26 C-18,-60 -14,-100 -6,-140 C-4,-160 -8,-174 -18,-178 Z" fill="${G.base}" ${O}/>`;
  s += `<path d="M-30,-140 C-34,-110 -40,-80 -48,-40 M-22,-130 C-24,-100 -26,-70 -30,-30" fill="none" stroke="${G.shade}" stroke-width="5" stroke-linecap="round"/>`;
  s += `<path d="M-62,-26 C-46,-20 -30,-20 -16,-26" fill="none" stroke="${G.deep}" stroke-width="3"/>`;
  return s;
}

function back(l) {
  if (l.back !== 'quiver')
    return '';
  let s = `<g transform="rotate(-22 -22 -140)">`;
  // Fletchings sticking out of the top
  s += `<path d="M-30,-196 L-26,-176 L-22,-196 Z" fill="#b8262a" ${OT}/>`;
  s += `<path d="M-22,-200 L-18,-176 L-14,-200 Z" fill="#f0e6d0" ${OT}/>`;
  s += `<path d="M-14,-194 L-12,-176 L-8,-194 Z" fill="#b8262a" ${OT}/>`;
  s += `<rect x="-34" y="-182" width="28" height="74" rx="6" fill="#7a4524" ${O}/>`;
  s += `<rect x="-34" y="-182" width="28" height="9" rx="3" fill="${GOLD}" ${OT}/>`;
  s += pearls(-20, -164, -20, -118, 5, 2, GOLD);
  s += `</g>`;
  return s;
}

// ------------------------------------------------------------------ head and hats

const HEAD_SHAPE = 'M-21,-200 C-22,-214 -10,-222 2,-222 C14,-222 21,-215 22,-205 L23,-200 L29,-193 L23,-190 L23,-186 L21,-182 C20,-177 15,-174 8,-174 C0,-174 -8,-176 -14,-182 C-20,-188 -21,-194 -21,-200 Z';
const MONSTER_HEAD = 'M-24,-196 C-26,-214 -12,-226 4,-226 C18,-226 28,-216 28,-204 L34,-196 L30,-188 L30,-182 C28,-172 20,-166 8,-166 C-4,-166 -14,-172 -20,-180 C-24,-186 -24,-192 -24,-196 Z';

function head(l) {
  if (l.monster)
    return monsterHead(l);
  const skin = l.skin;
  const skinDark = shade(skin, -0.22);
  const hair = l.hair;
  let s = '';
  // Neck
  s += `<path d="M-9,-180 L-9,-164 L9,-164 L9,-180 Z" fill="${skinDark}" ${O}/>`;
  // Hair behind the head
  if (l.female)
    s += `<path d="M-20,-206 C-30,-190 -30,-170 -24,-152 C-18,-150 -14,-152 -12,-156 C-16,-170 -14,-186 -8,-200 Z" fill="${hair}" ${O}/>` +
      `<path d="M-26,-156 C-30,-146 -28,-136 -24,-130 C-20,-136 -20,-146 -22,-154 Z" fill="${shade(hair, 0.15)}" ${OT}/>`;
  else
    s += `<path d="M-22,-198 C-27,-186 -23,-176 -14,-172 C-10,-178 -8,-188 -6,-198 Z" fill="${hair}" ${O}/>`;
  // Face
  s += `<path d="${HEAD_SHAPE}" fill="${skin}" ${O}/>`;
  s += `<path d="M-19,-196 C-18,-186 -12,-178 -4,-176 C-10,-182 -14,-190 -14,-200 Z" fill="${skinDark}" opacity="0.6"/>`;
  // Hair on top (mostly hidden under hats)
  s += `<path d="M-21,-202 C-20,-218 -2,-224 10,-222 C16,-221 20,-216 21,-211 C10,-214 -4,-212 -10,-204 C-12,-200 -16,-196 -21,-196 Z" fill="${hair}" ${O}/>`;
  // Ear
  s += `<path d="M-2,-200 C-8,-200 -8,-189 -2,-189" fill="${skinDark}" ${OT}/>`;
  // Eye and brow
  s += `<path d="M10,-199 C12,-201.5 16,-201.5 17.5,-199 C16,-197 12,-197 10,-199 Z" fill="#ffffff"/>`;
  s += `<circle cx="14.5" cy="-199" r="1.7" fill="#1b120c"/>`;
  s += `<path d="M8,-204.5 Q13,-207.5 19,-204.5" fill="none" stroke="${l.old ? '#d8d6cc' : hair}" stroke-width="2.4" stroke-linecap="round"/>`;
  if (l.female)
    s += `<path d="M17.5,-199 l2.5,-1.5" stroke="#1b120c" stroke-width="1"/>`;
  // Mouth
  s += `<path d="M18,-184.5 L22,-184.5" stroke="${shade(skin, -0.45)}" stroke-width="1.4" stroke-linecap="round"/>`;
  if (l.paint)
    s += `<path d="M6,-196 L20,-196 M4,-190 L12,-190" stroke="#c03030" stroke-width="2.2" stroke-linecap="round"/>`;
  s += beard(l.beard, hair);
  return s;
}

function beard(kind, hair) {
  switch (kind) {
    case 'short':
      return `<path d="M-6,-183 C0,-176 6,-172 14,-172 C19,-172 22,-176 23.5,-184 L20,-184 C18,-181 13,-181 9,-183 Z" fill="${hair}" ${O}/>` + mustache(hair);
    case 'long':
      return `<path d="M-6,-185 C-4,-166 6,-150 16,-147 C22,-156 25,-170 24,-186 L19,-186 C17,-182 11,-182 6,-184 Z" fill="${hair}" ${O}/>` +
        `<path d="M6,-176 C8,-166 12,-158 16,-152 M14,-178 C16,-170 18,-162 19,-156" fill="none" stroke="${shade(hair, -0.3)}" stroke-width="1.4"/>` + mustache(hair);
    case 'full':
      return `<path d="M-8,-188 C-8,-170 2,-160 14,-158 C22,-160 26,-172 25,-188 L19,-187 C17,-182 11,-182 6,-185 Z" fill="${hair}" ${O}/>` + mustache(hair);
    case 'pointed':
      return `<path d="M2,-182 C8,-176 14,-168 20,-156 C24,-168 24,-178 23,-185 L18,-185 C14,-182 8,-182 2,-182 Z" fill="${hair}" ${O}/>` + mustache(hair);
    case 'droopy':
      return `<path d="M13,-187 C18,-190 23,-189 25.5,-186 C26,-180 24,-172 22,-166 C20,-174 20,-181 17,-185 Z" fill="${hair}" ${O}/>`;
    case 'mustache':
      return mustache(hair);
    default:
      return '';
  }
}

function mustache(hair) {
  return `<path d="M14,-187.5 C18,-190 22.5,-189 25,-186.5 C21,-185 17.5,-185 14,-187.5 Z" fill="${hair}" ${OT}/>`;
}

function monsterHead(l) {
  const skin = l.skin;
  let s = '';
  s += `<path d="M-12,-176 L-12,-160 L12,-160 L12,-176 Z" fill="${shade(skin, -0.25)}" ${O}/>`;
  // Horns
  s += `<path d="M-14,-218 C-22,-238 -16,-256 -2,-264 C-8,-250 -8,-236 -2,-222 Z" fill="${l.horn}" ${O}/>`;
  s += `<path d="M10,-222 C14,-242 26,-254 40,-254 C32,-244 26,-232 22,-216 Z" fill="${l.horn}" ${O}/>`;
  s += `<path d="${MONSTER_HEAD}" fill="${skin}" ${O}/>`;
  // Wild hair / mane
  s += `<path d="M-24,-200 C-36,-196 -38,-180 -30,-168 L-24,-176 L-30,-186 L-22,-190 Z" fill="${l.fur}" ${O}/>`;
  // Glowing eye, heavy brow, tusk
  s += `<path d="M10,-202 L24,-206" stroke="${shade(skin, -0.5)}" stroke-width="4" stroke-linecap="round"/>`;
  s += `<circle cx="17" cy="-199" r="3.2" fill="#f2c230"/><circle cx="18" cy="-199" r="1.4" fill="#2a0a0a"/>`;
  s += `<path d="M14,-178 L30,-180" stroke="${shade(skin, -0.5)}" stroke-width="2" stroke-linecap="round"/>`;
  s += `<path d="M22,-179 L26,-192 L28,-179 Z" fill="${l.horn}" ${OT}/>`;
  return s;
}

/** Hats are their own sprite so helmet items can replace them. */
export function hat(kind, l = {}) {
  switch (kind) {
    case 'parthian': // tinted: Arash's cap, coloured by the outfit
      return `<path d="M-23,-206 C-28,-190 -27,-176 -24,-166 L-15,-168 C-16,-182 -15,-194 -13,-204 Z" fill="${G.shade}" ${O}/>` +
        `<path d="M-24,-200 C-28,-222 -16,-240 4,-242 C18,-243 28,-236 30,-226 C32,-218 28,-212 24,-214 C24,-220 20,-224 16,-222 C20,-216 22,-210 23,-204 Z" fill="${G.base}" ${O}/>` +
        `<path d="M-18,-214 C-14,-228 -4,-236 8,-238" fill="none" stroke="${G.light}" stroke-width="3" stroke-linecap="round"/>` +
        `<path d="M-25,-206 C-10,-211 10,-211 24,-207 L24,-200 C10,-204 -10,-204 -25,-199 Z" fill="${G.deep}" ${O}/>` +
        pearls(-18, -203.5, 18, -203.5, 6, 1.2, G.light);
    case 'felt':
      return `<path d="M-22,-202 C-22,-220 -10,-228 2,-228 C14,-228 22,-220 22,-204 Z" fill="#8a6440" ${O}/>` +
        `<path d="M-28,-204 C-10,-208 12,-208 30,-204 L28,-198 C10,-202 -10,-202 -28,-198 Z" fill="#6a4a2c" ${O}/>`;
    case 'scarf':
      return `<path d="M-24,-196 C-26,-216 -12,-226 4,-225 C16,-224 22,-216 22,-206 L18,-204 C14,-212 4,-214 -6,-208 C-12,-200 -14,-190 -12,-176 L-26,-168 Z" fill="#2f7f86" ${O}/>` +
        `<path d="M-22,-210 C-8,-218 8,-218 21,-210" fill="none" stroke="${GOLD}" stroke-width="2.6"/>` +
        pearls(-16, -214, 14, -214, 5, 1.2, '#fff6dc');
    case 'mobadcap':
      return `<path d="M-21,-204 L-18,-246 C-8,-252 8,-252 16,-246 L20,-206 Z" fill="#f4f0e6" ${O}/>` +
        `<path d="M-21,-212 L20,-212 L20,-204 L-21,-204 Z" fill="${GOLD}" ${OT}/>` +
        `<path d="M-6,-246 L-4,-214 M6,-247 L8,-214" stroke="#d8d2c0" stroke-width="1.6"/>`;
    case 'crown':
      return `<path d="M-44,-204 C-38,-198 -32,-196 -24,-200 C-30,-196 -36,-188 -46,-186 M-40,-210 C-34,-206 -28,-206 -22,-208" fill="none" stroke="#c03030" stroke-width="4" stroke-linecap="round"/>` +
        `<path d="M-23,-202 L-23,-226 L-15,-226 L-15,-234 L-7,-234 L-7,-226 L1,-226 L1,-238 L9,-238 L9,-226 L17,-226 L17,-234 L23,-234 L23,-202 Z" fill="${GOLD}" ${O}/>` +
        `<path d="M-23,-212 L23,-212" stroke="${GOLD_DARK}" stroke-width="2"/>` + pearls(-18, -207, 18, -207, 6, 1.6, '#fff6dc') +
        `<path d="M-6,-238 C-4,-246 4,-246 6,-238 Z" fill="${GOLD}" ${OT}/>` +
        `<circle cx="0" cy="-260" r="14" fill="#2c4f9a" ${O}/><path d="M-8,-268 C-2,-274 6,-272 10,-264" fill="none" stroke="#7aa0e0" stroke-width="2.5"/>` +
        `<path d="M-10,-250 C-4,-244 4,-244 10,-250" fill="none" stroke="${GOLD}" stroke-width="2"/>`;
    case 'darkcrown':
      return `<path d="M-23,-202 L-24,-232 L-14,-218 L-8,-240 L0,-220 L8,-242 L14,-218 L24,-234 L23,-202 Z" fill="#33303a" ${O}/>` +
        `<path d="M-23,-212 L23,-212" stroke="#7a1a1a" stroke-width="3"/>` +
        `<path d="M-2,-214 L2,-222 L6,-214 L2,-206 Z" fill="#d02828" ${OT}/>`;
    case 'cloth':
      return `<path d="M-23,-202 C-24,-218 -12,-226 2,-226 C14,-226 22,-218 22,-206 C10,-210 -6,-208 -23,-202 Z" fill="#9a7a50" ${O}/>`;
    case 'turban':
      return `<path d="M-25,-200 C-30,-222 -14,-238 4,-238 C20,-238 30,-226 26,-204 C14,-210 -8,-208 -25,-200 Z" fill="#efe6cf" ${O}/>` +
        `<path d="M-24,-212 C-10,-226 10,-230 26,-220 M-22,-222 C-8,-232 8,-234 22,-230" fill="none" stroke="#c8bc9c" stroke-width="2"/>` +
        `<circle cx="18" cy="-220" r="4" fill="#2a8a8a" ${OT}/>`;
    case 'furhat':
      return `<path d="M-18,-214 C-12,-236 -6,-252 0,-266 C6,-252 12,-236 18,-214 Z" fill="#7a2018" ${O}/>` +
        `<path d="M-30,-196 C-34,-190 -34,-180 -30,-172 L-24,-178 L-22,-198 Z" fill="#5b4636" ${O}/>` +
        `<path d="M-27,-216 C-20,-222 20,-222 27,-216 C29,-210 29,-204 27,-198 C20,-202 -20,-202 -27,-198 C-29,-204 -29,-210 -27,-216 Z" fill="#5b4636" ${O}/>` +
        `<path d="M-22,-214 l2,8 M-14,-216 l2,8 M-6,-217 l2,8 M2,-217 l2,8 M10,-216 l2,8 M18,-214 l2,7" stroke="#3a2a1e" stroke-width="1.6"/>` +
        `<circle cx="0" cy="-266" r="3" fill="${GOLD}" ${OT}/>`;
    case 'conical':
    case 'iron':
      return `<path d="M-25,-204 L-27,-176 L-10,-172 L-12,-200 Z" fill="${STEEL_DARK}" ${O}/>` +
        `<path d="M-23,-204 C-22,-230 -8,-246 2,-252 C12,-246 24,-230 24,-204 Z" fill="${STEEL}" ${O}/>` +
        `<path d="M-14,-212 C-12,-230 -4,-242 2,-248" fill="none" stroke="#e6ecf2" stroke-width="3" stroke-linecap="round"/>` +
        `<path d="M-24,-210 L24,-210 L24,-203 L-24,-203 Z" fill="${kind === 'iron' ? GOLD : STEEL_DARK}" ${O}/>` +
        `<path d="M19,-206 L24,-206 L24,-190 L20,-190 Z" fill="${STEEL}" ${OT}/>` +
        pearls(-18, -206.5, 14, -206.5, 6, 1.1, '#e6ecf2');
    case 'leather':
      return `<path d="M-23,-200 C-24,-222 -10,-232 2,-232 C14,-232 23,-222 22,-204 Z" fill="#8a5a30" ${O}/>` +
        `<path d="M-24,-202 L-26,-180 L-14,-178 L-12,-200 Z" fill="#6a4220" ${O}/>` +
        `<path d="M0,-232 L0,-204" stroke="#5a3418" stroke-width="2"/>`;
    case 'headband':
      return `<path d="M-22,-210 C-8,-214 8,-214 22,-210 L22,-203 C8,-207 -8,-207 -22,-203 Z" fill="#b02a1a" ${O}/>` +
        `<path d="M-21,-208 C-30,-206 -36,-200 -40,-190 M-21,-205 C-28,-200 -32,-192 -32,-182" fill="none" stroke="#b02a1a" stroke-width="4" stroke-linecap="round"/>`;
    case 'hood':
    case 'maskhood': {
      const c = l.hood || '#2c2a33';
      let s = `<path d="M-27,-196 C-29,-222 -10,-236 6,-234 C20,-232 29,-220 28,-204 L21,-207 C16,-215 4,-215 -2,-207 L-4,-176 L-26,-166 Z" fill="${c}" ${O}/>` +
        `<path d="M-18,-216 C-10,-226 2,-230 12,-228" fill="none" stroke="${shade(c, 0.25)}" stroke-width="3" stroke-linecap="round"/>`;
      if (kind === 'maskhood')
        s += `<path d="M6,-193 L29,-193 L26,-178 L8,-174 Z" fill="${shade(c, 0.12)}" ${O}/>`;
      return s;
    }
    case 'horns':
      return `<path d="M-22,-204 C-22,-220 -10,-226 2,-226 C14,-226 22,-220 22,-206 Z" fill="#5a3a2a" ${O}/>` +
        `<path d="M-16,-220 C-30,-232 -32,-250 -24,-262 C-22,-250 -16,-238 -6,-226 Z" fill="#e8dcc0" ${O}/>` +
        `<path d="M10,-224 C22,-236 34,-240 42,-252 C40,-238 30,-226 18,-218 Z" fill="#e8dcc0" ${O}/>` +
        `<path d="M-4,-226 L-8,-250 L0,-232 L4,-254 L6,-228" fill="#7a3ab0" ${O}/>` +
        pearls(-18, -207, 18, -207, 6, 1.6, '#c8a0f0');
    case 'goldhelm':
    case 'gold':
      return `<path d="M-6,-248 C-20,-262 -40,-258 -50,-240 C-40,-246 -28,-246 -18,-240 Z" fill="#c02828" ${O}/>` +
        `<path d="M-24,-204 L-26,-178 L-12,-174 L-12,-200 Z" fill="${STEEL_DARK}" ${O}/>` +
        `<path d="M-23,-204 C-22,-230 -8,-246 2,-250 C12,-246 24,-230 24,-204 Z" fill="${GOLD}" ${O}/>` +
        `<path d="M-14,-212 C-12,-230 -4,-240 2,-246" fill="none" stroke="#fff0b0" stroke-width="3" stroke-linecap="round"/>` +
        `<path d="M-24,-210 L24,-210 L24,-203 L-24,-203 Z" fill="${GOLD_DARK}" ${O}/>` +
        `<circle cx="2" cy="-252" r="4" fill="${GOLD}" ${O}/>` + pearls(-18, -206.5, 18, -206.5, 6, 1.2, '#fff6dc');
    case 'hornhelm':
      return `<path d="M-16,-226 C-30,-234 -40,-250 -36,-266 C-30,-252 -22,-242 -10,-236 Z" fill="#e8dcc0" ${O}/>` +
        `<path d="M14,-228 C28,-236 36,-250 32,-266 C26,-252 18,-242 6,-238 Z" fill="#e8dcc0" ${O}/>` +
        `<path d="M-24,-204 L-26,-176 L-10,-172 L-12,-200 Z" fill="${STEEL_DARK}" ${O}/>` +
        `<path d="M-23,-204 C-22,-228 -10,-242 1,-244 C12,-242 24,-228 24,-204 Z" fill="#8d959e" ${O}/>` +
        `<path d="M-24,-210 L24,-210 L24,-203 L-24,-203 Z" fill="#4a3a2a" ${O}/>`;
    case 'tallturban':
      return `<path d="M-24,-202 C-28,-232 -18,-262 2,-270 C20,-262 28,-232 24,-204 C10,-210 -10,-210 -24,-202 Z" fill="#5a2a7a" ${O}/>` +
        `<path d="M-24,-216 C-8,-230 12,-232 25,-222 M-22,-234 C-6,-246 10,-248 22,-240 M-14,-252 C-2,-260 8,-260 16,-254" fill="none" stroke="#3a1a52" stroke-width="2.2"/>` +
        `<path d="M10,-232 C14,-252 26,-262 34,-270 C30,-256 24,-244 16,-230 Z" fill="#e8dcc0" ${OT}/>` +
        `<circle cx="12" cy="-228" r="5" fill="#d02828" ${O}/>`;
    default:
      return '';
  }
}

// ------------------------------------------------------------------ arm and weapons (pivot space, +x along the aim)

function arm(l) {
  if (l.monster)
    return `<path d="M-8,-12 C10,-14 34,-12 58,-9 L58,9 C34,12 10,14 -8,12 C-14,6 -14,-6 -8,-12 Z" fill="${l.skin}" ${O}/>` +
      `<path d="M10,-6 C24,-8 36,-6 46,-4" fill="none" stroke="${shade(l.skin, -0.35)}" stroke-width="2"/>`;
  return `<path d="M-6,-9 C10,-10 30,-8 52,-6 L52,6 C30,8 10,10 -6,9 C-10,4 -10,-4 -6,-9 Z" fill="${G.base}" ${O}/>` +
    `<path d="M-4,4 C14,7 32,6 50,4" fill="none" stroke="${G.shade}" stroke-width="3.5" stroke-linecap="round"/>` +
    `<path d="M45,-7 L52,-6 L52,6 L45,7 Z" fill="${G.deep}" ${OT}/>`;
}

function hand(x, y, skin, r = 6) {
  return `<circle cx="${x}" cy="${y}" r="${r}" fill="${skin}" ${O}/>`;
}

function bowShape(color, tips, string = '#efe6d0') {
  return `<path d="M6,0 L56,-58 M6,0 L56,58" stroke="${string}" stroke-width="1.3"/>` +
    `<path d="M56,-58 C50,-52 46,-44 50,-30 C54,-16 62,-8 62,0 C62,8 54,16 50,30 C46,44 50,52 56,58" fill="none" stroke="${'#2b1d14'}" stroke-width="7.5" stroke-linecap="round"/>` +
    `<path d="M56,-58 C50,-52 46,-44 50,-30 C54,-16 62,-8 62,0 C62,8 54,16 50,30 C46,44 50,52 56,58" fill="none" stroke="${color}" stroke-width="4.6" stroke-linecap="round"/>` +
    `<path d="M50,-36 C52,-26 56,-18 60,-10 M50,36 C52,26 56,18 60,10" fill="none" stroke="${shade(color, 0.35)}" stroke-width="1.6"/>` +
    `<circle cx="56" cy="-58" r="3" fill="${tips}" ${OT}/><circle cx="56" cy="58" r="3" fill="${tips}" ${OT}/>`;
}

function nockedArrow(head = '#c8d0d8') {
  return `<path d="M6,0 L74,0" stroke="#2b1d14" stroke-width="4"/><path d="M6,0 L74,0" stroke="#c89a5a" stroke-width="2.2"/>` +
    `<path d="M74,-3.5 L84,0 L74,3.5 Z" fill="${head}" ${OT}/>` +
    `<path d="M6,0 L16,-5 L20,-5 L12,0 L20,5 L16,5 Z" fill="#b8262a" ${OT}/>`;
}

function weapon(l) {
  const skin = l.skin;
  switch (l.weapon) {
    case 'bow':
      return bowShape(l.bow, GOLD) + nockedArrow() + hand(6, 0, skin, 5.5) + hand(60, 0, skin);
    case 'darkbow':
      return bowShape(l.bow, '#6a6a7a', '#b8b0a0') + nockedArrow('#8a9098') + hand(6, 0, skin, 5.5) + hand(60, 0, skin);
    case 'firebow':
      return bowShape(l.bow, '#e07a1a') + nockedArrow() +
        `<path d="M78,0 C80,-10 88,-14 92,-20 C92,-12 98,-8 96,0 C94,6 86,8 80,5 Z" fill="#f08a1a" ${OT}/><path d="M82,0 C84,-6 88,-8 90,-12 C90,-6 92,-2 90,2 Z" fill="#ffd040"/>` +
        hand(6, 0, skin, 5.5) + hand(60, 0, skin);
    case 'spear':
      return `<path d="M-56,0 L104,0" stroke="#2b1d14" stroke-width="6.5" stroke-linecap="round"/><path d="M-56,0 L104,0" stroke="#8a5a2c" stroke-width="4" stroke-linecap="round"/>` +
        `<path d="M100,0 C106,-6 116,-6 128,0 C116,6 106,6 100,0 Z" fill="${STEEL}" ${O}/>` +
        `<path d="M96,-5 L100,-5 L100,5 L96,5 Z" fill="#b02a1a"/>` + hand(56, 0, skin);
    case 'sling':
      return `<path d="M58,0 C64,10 66,22 68,30 M58,0 C70,8 76,18 74,30" fill="none" stroke="#6a4a2a" stroke-width="1.8"/>` +
        `<path d="M64,28 C66,36 76,36 78,28 Z" fill="#7a5030" ${OT}/><circle cx="71" cy="30" r="5" fill="#8a8680" ${OT}/>` + hand(58, 0, skin);
    case 'staff':
      return `<path d="M58,-74 L58,62" stroke="#2b1d14" stroke-width="7" stroke-linecap="round"/><path d="M58,-74 L58,62" stroke="#8a6234" stroke-width="4.4" stroke-linecap="round"/>` +
        `<path d="M50,-80 C54,-92 62,-92 66,-80 C62,-84 54,-84 50,-80 Z" fill="${GOLD}" ${O}/>` +
        `<path d="M58,-96 C52,-102 54,-110 58,-116 C62,-110 64,-102 58,-96 Z" fill="#f0a020" ${OT}/>` + hand(58, 0, skin);
    case 'orbstaff':
      return `<path d="M58,-64 L58,62" stroke="#2b1d14" stroke-width="7" stroke-linecap="round"/><path d="M58,-64 L58,62" stroke="#5a3a2a" stroke-width="4.4" stroke-linecap="round"/>` +
        `<circle cx="58" cy="-74" r="14" fill="#b07ae8" opacity="0.35"/><circle cx="58" cy="-74" r="9" fill="#8a4ad0" ${O}/><circle cx="55" cy="-77" r="3" fill="#ead8ff"/>` +
        `<path d="M50,-60 L40,-48 M66,-60 L76,-50" stroke="#c8a0f0" stroke-width="3" stroke-linecap="round"/>` + hand(58, 0, skin);
    case 'sword':
      return `<path d="M60,0 C78,-2 96,-10 116,-26 C104,-8 86,6 62,6 Z" fill="${STEEL}" ${O}/>` +
        `<path d="M56,-8 L60,8" stroke="${GOLD}" stroke-width="5" stroke-linecap="round"/>` + hand(56, 0, skin);
    case 'boulder':
      return `<circle cx="74" cy="-10" r="24" fill="#8a8478" ${O}/><path d="M60,-20 C66,-28 76,-30 84,-26 M70,4 C76,6 84,2 88,-4" fill="none" stroke="#5e5a50" stroke-width="2.5"/>` +
        hand(58, 0, skin, 9);
    default:
      return hand(56, 0, skin);
  }
}

// ------------------------------------------------------------------ export

/** All character sprites, under Art/Characters/{look}/{part}. */
export function characterAssets() {
  const out = [];
  for (const [id, l] of Object.entries(LOOKS)) {
    const dir = `Characters/${id}/`;
    out.push(asset(dir + 'legs', BOX.legs, legs(l), { anchor: ANCHORS.legs }));
    out.push(asset(dir + 'torso', BOX.torso, torsoBase(l), { anchor: ANCHORS.torso }));
    out.push(asset(dir + 'detail', BOX.torso, torsoDetail(l), { anchor: ANCHORS.torso }));
    out.push(asset(dir + 'head', BOX.head, head(l), { anchor: ANCHORS.head }));
    const h = hat(l.hat, l);
    if (h)
      out.push(asset(dir + 'hat', BOX.hat, h, { anchor: ANCHORS.head }));
    if (l.cape)
      out.push(asset(dir + 'cape', BOX.cape, cape(l), { anchor: ANCHORS.torso }));
    const b = back(l);
    if (b)
      out.push(asset(dir + 'back', BOX.back, b, { anchor: ANCHORS.torso }));
    out.push(asset(dir + 'arm', BOX.arm, arm(l), { anchor: [0, 0] }));
    out.push(asset(dir + 'weapon', BOX.weapon, weapon(l), { anchor: [0, 0] }));
  }

  // Equipment for Arash: helmets replace the hat, armour goes over the tunic.
  for (const kind of ['felt', 'iron', 'gold'])
    out.push(asset(`Equipment/helmet_${kind}`, BOX.hat, hat(kind), { anchor: ANCHORS.head }));
  for (const kind of ['leather', 'scale', 'immortal'])
    out.push(asset(`Equipment/armor_${kind}`, BOX.torso, armor(kind), { anchor: ANCHORS.torso }));
  return out;
}

/** One composed character in SVG units, for the preview sheet. */
export function composeCharacter(id, { tunic = '#1a6b8c', capeColor = '#a8262c', capColor = '#b3262e', angle = -12 } = {}) {
  const l = LOOKS[id];
  const tint = (svg, color) => `<g style="filter:url(#tint-${color.replace('#', '')})">${svg}</g>`;
  const filters = new Set();
  const useTint = (svg, color) => {
    filters.add(color);
    return tint(svg, color);
  };
  let s = '';
  if (l.cape)
    s += useTint(cape(l), capeColor);
  s += back(l);
  s += legs(l);
  s += l.monster ? torsoBase(l) : useTint(torsoBase(l), tunic);
  s += torsoDetail(l);
  s += head(l);
  const h = hat(l.hat, l);
  if (h)
    s += l.hat === 'parthian' ? useTint(h, capColor) : h;
  const [px, py] = ANCHORS.shoulder;
  s += `<g transform="translate(${px} ${py}) rotate(${angle})">${l.monster ? arm(l) : useTint(arm(l), tunic)}${weapon(l)}</g>`;
  const defs = [...filters].map(c => {
    const [r, g, b] = [0, 2, 4].map(i => parseInt(c.replace('#', '').substr(i, 2), 16) / 255);
    return `<filter id="tint-${c.replace('#', '')}"><feColorMatrix type="matrix" values="${r} 0 0 0 0  0 ${g} 0 0 0  0 0 ${b} 0 0  0 0 0 1 0"/></filter>`;
  }).join('');
  return { defs, content: s };
}
