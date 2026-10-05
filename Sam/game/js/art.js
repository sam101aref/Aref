// Everything is drawn in code in a Persian-miniature style, like the design art of Siavosh:
// ink outlines, flat pigment colours, gold trim. Figures share one rig so the whole cast is consistent.
var SAM = window.SAM || (window.SAM = {});

SAM.PAL = {
  INK: '#2B1B12', PAPER: '#F4E8CC', PAPER_DARK: '#E6D3A8',
  GOLD: '#C99A2E', GOLD_LIGHT: '#E8C65A', GOLD_DARK: '#8E6A1C',
  LAPIS: '#1F3C88', LAPIS_DARK: '#14275E', LAPIS_NIGHT: '#0E1A3F',
  TURQUOISE: '#2E9C95', TURQUOISE_LIGHT: '#6CC5B8',
  VERMILION: '#C2412D', CRIMSON: '#8E1F2A', SAFFRON: '#E3A42B',
  LEAF: '#4F7A34', LEAF_DARK: '#2F5221', MEADOW: '#7FA34A',
  ROSE: '#D88E86', LILAC: '#8D6CA6', PLUM: '#5A2E5E', WHITE: '#FBF7EC',
  SKIN: '#F3D9BC', SKIN_SHADE: '#E2BC96', HAIR: '#1C120D', BEARD_WHITE: '#ECE6DA',
  STEEL: '#AEB7BF', STEEL_DARK: '#6F7A84', EARTH: '#9C6B3E', EARTH_DARK: '#6B4626',
  FIRE_CORE: '#FFF1B8', FIRE_MID: '#F7A21B', FIRE_OUT: '#D9381E', SMOKE: '#5B4A44',
};

SAM.Art = (function () {
  var P = SAM.PAL;
  var TAU = Math.PI * 2;

  // ------------------------------------------------------------------ cast

  var LOOKS = {
    sam: { robe: P.LAPIS, trim: P.GOLD, sash: P.VERMILION, legs: P.LAPIS_DARK, boots: P.EARTH_DARK,
      beard: P.HAIR, beardStreak: P.STEEL, hat: 'helmet', plume: P.WHITE },
    nariman: { robe: P.TURQUOISE, trim: P.GOLD, sash: P.CRIMSON, legs: P.LAPIS_DARK, boots: P.EARTH_DARK,
      beard: P.BEARD_WHITE, hat: 'turban', hatColor: P.WHITE, long: true },
    manuchehr: { robe: P.PLUM, trim: P.GOLD, sash: P.GOLD, legs: P.CRIMSON, boots: P.EARTH_DARK,
      beard: P.HAIR, hat: 'crown', long: true, pattern: P.GOLD_LIGHT },
    zal: { robe: P.WHITE, trim: P.GOLD, sash: P.TURQUOISE, legs: P.LAPIS, boots: P.EARTH_DARK,
      hair: P.BEARD_WHITE, hat: 'none', young: true },
    rudabeh: { robe: P.ROSE, trim: P.GOLD, sash: P.LAPIS, legs: P.CRIMSON, boots: P.CRIMSON,
      hair: P.HAIR, hat: 'tiara', young: true, long: true, pattern: P.WHITE },
    naqqal: { robe: P.LEAF_DARK, trim: P.SAFFRON, sash: P.EARTH, legs: P.EARTH_DARK, boots: P.HAIR,
      beard: P.BEARD_WHITE, hat: 'cap', hatColor: P.HAIR, long: true },
    mobed: { robe: P.WHITE, trim: P.GOLD_LIGHT, sash: P.WHITE, legs: P.PAPER_DARK, boots: P.EARTH,
      beard: P.BEARD_WHITE, hat: 'turban', hatColor: P.WHITE, long: true },
    bandit: { robe: P.EARTH, trim: P.SAFFRON, sash: P.CRIMSON, legs: P.EARTH_DARK, boots: P.HAIR,
      beard: P.HAIR, hat: 'turban', hatColor: P.SAFFRON },
    div: { skin: '#6E8B4E', skinShade: '#4F6A35', robe: '#B9893A', trim: P.INK, sash: P.CRIMSON,
      legs: '#6E8B4E', boots: '#4F6A35', hat: 'horns', bare: true, size: 1.16, spots: true },
    karkoy: { robe: P.CRIMSON, trim: P.GOLD, sash: P.HAIR, legs: P.HAIR, boots: P.HAIR,
      beard: P.HAIR, hat: 'helmet', plume: P.CRIMSON, size: 1.08 },
  };

  function shade(hex, k) {
    var n = parseInt(hex.slice(1), 16);
    var r = (n >> 16) & 255, g = (n >> 8) & 255, b = n & 255;
    r = Math.round(r * k); g = Math.round(g * k); b = Math.round(b * k);
    return 'rgb(' + Math.min(255, r) + ',' + Math.min(255, g) + ',' + Math.min(255, b) + ')';
  }

  function seg(ctx, x1, y1, x2, y2, w, color) {
    ctx.lineCap = 'round';
    ctx.strokeStyle = P.INK; ctx.lineWidth = w + 2.4;
    ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();
    ctx.strokeStyle = color; ctx.lineWidth = w;
    ctx.beginPath(); ctx.moveTo(x1, y1); ctx.lineTo(x2, y2); ctx.stroke();
  }

  function poly(ctx, pts, fill, stroke, lw) {
    ctx.beginPath();
    ctx.moveTo(pts[0], pts[1]);
    for (var i = 2; i < pts.length; i += 2) ctx.lineTo(pts[i], pts[i + 1]);
    ctx.closePath();
    if (fill) { ctx.fillStyle = fill; ctx.fill(); }
    if (stroke !== false) { ctx.strokeStyle = stroke || P.INK; ctx.lineWidth = lw || 1.3; ctx.lineJoin = 'round'; ctx.stroke(); }
  }

  function circle(ctx, x, y, r, fill, stroke, lw) {
    ctx.beginPath(); ctx.arc(x, y, r, 0, TAU);
    if (fill) { ctx.fillStyle = fill; ctx.fill(); }
    if (stroke !== false) { ctx.strokeStyle = stroke || P.INK; ctx.lineWidth = lw || 1.3; ctx.stroke(); }
  }

  // ------------------------------------------------------------------ poses
  // Angles in radians. Legs: hb/hf hip swing (+ = forward), kb/kf knee bend.
  // Arms: shB/shF shoulder (0 = hanging down, + = forward, PI = straight up), elB/elF elbow flex.

  var BASE = { hb: -0.08, kb: 0.06, hf: 0.1, kf: 0.08, lean: 0.02, shB: -0.12, elB: 0.3, shF: 0.15, elF: 0.4, wpn: 0, rot: 0, dy: 0 };

  function pose(o) {
    var p = {};
    for (var k in BASE) p[k] = BASE[k];
    for (var j in o) p[j] = o[j];
    return p;
  }

  function lerpPose(a, b, t) {
    var p = {};
    for (var k in a) p[k] = typeof a[k] === 'number' ? a[k] + ((b[k] !== undefined ? b[k] : a[k]) - a[k]) * t : a[k];
    for (var j in b) if (p[j] === undefined) p[j] = b[j];
    return p;
  }

  var POSES = {
    stand: pose({}),
    standMace: pose({ shF: 0.35, elF: 2.0, wpn: 0.5 }),
    crouch: pose({ hf: 1.25, kf: 2.3, hb: 0.85, kb: 2.1, lean: 0.4, shF: 0.7, elF: 0.6, shB: 0.4, elB: 0.6 }),
    jumpUp: pose({ hb: -0.1, kb: 0.15, hf: 0.15, kf: 0.25, shF: 2.95, elF: 0.05, shB: 2.85, elB: 0.05 }),
    jumpFwd: pose({ hf: 1.0, kf: 1.3, hb: -0.5, kb: 0.7, shF: 2.3, elF: 0.3, shB: 1.3, elB: 0.5, lean: 0.25 }),
    fall: pose({ hf: 0.35, kf: 0.6, hb: -0.2, kb: 0.4, shF: 2.5, elF: 0.4, shB: 2.1, elB: 0.5, lean: -0.05 }),
    hang: pose({ hb: -0.04, kb: 0.1, hf: 0.08, kf: 0.25, shF: 3.1, elF: 0, shB: 3.05, elB: 0, lean: 0 }),
    fight: pose({ hf: 0.45, kf: 0.55, hb: -0.45, kb: 0.35, lean: 0.08, shF: 1.15, elF: 0.95, wpn: -0.2, shB: -0.35, elB: 0.9 }),
    strikeUp: pose({ hf: 0.35, kf: 0.4, hb: -0.5, kb: 0.3, lean: -0.08, shF: 2.75, elF: 0.6, wpn: 0, shB: -0.5, elB: 0.6 }),
    strikeDown: pose({ hf: 0.75, kf: 0.6, hb: -0.55, kb: 0.2, lean: 0.28, shF: 0.95, elF: 0.15, wpn: 0, shB: -0.7, elB: 0.5 }),
    parry: pose({ hf: 0.35, kf: 0.5, hb: -0.5, kb: 0.4, lean: -0.06, shF: 1.75, elF: 1.2, wpn: -1.3, shB: -0.3, elB: 1.1 }),
    hurt: pose({ hf: 0.2, kf: 0.3, hb: -0.3, kb: 0.4, lean: -0.38, shF: 1.6, elF: 0.6, shB: 2.0, elB: 0.5 }),
    dead: pose({ hf: 0.05, kf: 0.1, hb: -0.05, kb: 0.1, shF: 2.6, elF: 0.2, shB: 2.4, elB: 0.2, rot: -Math.PI / 2, dy: -9 }),
    drink: pose({ shF: 2.3, elF: 2.5, lean: -0.12 }),
    shoot: pose({ hf: 0.35, kf: 0.2, hb: -0.35, kb: 0.15, shF: 1.57, elF: 0.02, shB: 1.5, elB: -2.2, lean: -0.04 }),
    raise: pose({ shF: 2.9, elF: 0.15, wpn: 0, shB: -0.2, elB: 0.3, lean: -0.05 }),
    point: pose({ shF: 1.9, elF: 0.2, shB: -0.1, elB: 0.3 }),
    bow: pose({ lean: 0.45, shF: 0.6, elF: 0.6, shB: 0.4, elB: 0.6, hf: 0.15, kf: 0.25 }),
    kneel: pose({ hf: 1.5, kf: 1.5, hb: -0.2, kb: 2.6, lean: 0.1, shF: 1.2, elF: 0.6, shB: 0.2, elB: 0.4 }),
  };

  function run(phase, amp) {
    amp = amp || 1;
    var s = Math.sin(phase), c = Math.cos(phase);
    return pose({
      hf: 0.75 * amp * s, hb: -0.75 * amp * s,
      kf: 0.2 + 1.15 * amp * Math.max(0, c), kb: 0.2 + 1.15 * amp * Math.max(0, -c),
      shF: -0.6 * amp * s, shB: 0.6 * amp * s, elF: 0.9, elB: 0.9, lean: 0.06 + 0.14 * amp,
    });
  }

  // ------------------------------------------------------------------ figure

  var TH = 22, SH = 22, TO = 30, UA = 17, FA = 16;

  function limbPts(hx, hy, a, k, l1, l2) {
    var kx = hx + Math.sin(a) * l1, ky = hy + Math.cos(a) * l1;
    var b = a - k;
    return [kx, ky, kx + Math.sin(b) * l2, ky + Math.cos(b) * l2];
  }

  function armPts(sx, sy, a, e) {
    var ex = sx + Math.sin(a) * UA, ey = sy + Math.cos(a) * UA;
    var b = a + e;
    return [ex, ey, ex + Math.sin(b) * FA, ey + Math.cos(b) * FA, b];
  }

  function drawWeapon(ctx, kind, x, y, ang, look) {
    var dx = Math.sin(ang), dy = Math.cos(ang);
    if (kind === 'mace') {
      var ex = x + dx * 34, ey = y + dy * 34;
      seg(ctx, x - dx * 6, y - dy * 6, ex, ey, 3.2, P.EARTH_DARK);
      // the ox head of the mace (gorz-e gavsar)
      ctx.save(); ctx.translate(ex + dx * 7, ey + dy * 7); ctx.rotate(-ang + Math.PI);
      poly(ctx, [-7, -2, 7, -2, 6, 9, 0, 13, -6, 9], P.STEEL, P.INK, 1.2);
      poly(ctx, [-7, 0, -14, -6, -12, -11, -6, -4], P.BEARD_WHITE, P.INK, 1);
      poly(ctx, [7, 0, 14, -6, 12, -11, 6, -4], P.BEARD_WHITE, P.INK, 1);
      circle(ctx, -2.5, 3, 1.1, P.INK, false); circle(ctx, 2.5, 3, 1.1, P.INK, false);
      ctx.fillStyle = P.GOLD; ctx.fillRect(-6, -3.5, 12, 2.5);
      ctx.restore();
    } else if (kind === 'sword') {
      seg(ctx, x - dx * 7, y - dy * 7, x + dx * 4, y + dy * 4, 3, P.CRIMSON);
      ctx.save(); ctx.translate(x, y); ctx.rotate(-ang);
      poly(ctx, [-1.8, 4, 1.8, 4, 4, 22, 2, 38, -1, 44, -2, 30], P.STEEL, P.INK, 1.1);
      poly(ctx, [-7, 3, 7, 3, 7, 6, -7, 6], P.GOLD, P.INK, 1);
      ctx.restore();
    } else if (kind === 'club') {
      ctx.save(); ctx.translate(x, y); ctx.rotate(-ang);
      poly(ctx, [-2.5, -6, 2.5, -6, 7, 30, 0, 40, -7, 30], P.EARTH, P.INK, 1.2);
      circle(ctx, -3, 26, 1.5, P.STEEL, P.INK, 0.8); circle(ctx, 3, 32, 1.5, P.STEEL, P.INK, 0.8); circle(ctx, 0, 19, 1.5, P.STEEL, P.INK, 0.8);
      ctx.restore();
    } else if (kind === 'staff') {
      seg(ctx, x - dx * 40, y - dy * 40, x + dx * 60, y + dy * 60, 3, P.EARTH_DARK);
    } else if (kind === 'bow') {
      ctx.strokeStyle = P.INK; ctx.lineWidth = 3.6;
      ctx.beginPath(); ctx.arc(x - 6, y, 22, -1.1, 1.1); ctx.stroke();
      ctx.strokeStyle = P.EARTH; ctx.lineWidth = 2.2;
      ctx.beginPath(); ctx.arc(x - 6, y, 22, -1.1, 1.1); ctx.stroke();
      ctx.strokeStyle = P.PAPER; ctx.lineWidth = 0.8;
      ctx.beginPath(); ctx.moveTo(x - 6 + 22 * Math.cos(-1.1), y + 22 * Math.sin(-1.1));
      ctx.lineTo(look.bowHandX || x - 20, y); ctx.lineTo(x - 6 + 22 * Math.cos(1.1), y + 22 * Math.sin(1.1)); ctx.stroke();
    }
  }

  function drawHead(ctx, look, hx, hy, lean) {
    var skin = look.skin || P.SKIN;
    ctx.save(); ctx.translate(hx, hy); ctx.rotate(lean * 0.5);
    if (look.hair) { // long hair falling behind
      poly(ctx, [-6, -6, -13, 4, -12, 20, -4, 14, 2, -4], look.hair, P.INK, 1.1);
    }
    circle(ctx, 0, 0, 8.5, skin, P.INK, 1.3);
    if (look.beard && !look.young) {
      poly(ctx, [-5, 2, 8, 2, 7.5, 9, 3, 16, -2, 11, -6, 6], look.beard, P.INK, 1.1);
      if (look.beardStreak) { ctx.strokeStyle = look.beardStreak; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(2, 4); ctx.lineTo(3, 13); ctx.stroke(); }
      ctx.strokeStyle = P.INK; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(3, 4.5); ctx.quadraticCurveTo(6, 5.5, 8.5, 4); ctx.stroke();
    }
    circle(ctx, 4.5, -1.5, 1.1, P.INK, false);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1; ctx.beginPath(); ctx.moveTo(2.5, -4.5); ctx.lineTo(6.5, -4.8); ctx.stroke();
    var hat = look.hat;
    if (hat === 'helmet') {
      poly(ctx, [-9, -2, -8.5, -9, -4, -13.5, 0, -15, 4, -13.5, 8.5, -9, 9, -3], P.STEEL, P.INK, 1.2);
      ctx.fillStyle = P.GOLD; ctx.fillRect(-9, -4.5, 18, 2.6);
      seg(ctx, 0, -15, 0, -21, 1.6, P.GOLD);
      ctx.save(); ctx.translate(0, -21); ctx.rotate(-0.9);
      poly(ctx, [0, 0, 4, -10, 2, -18, -2, -9], look.plume || P.WHITE, P.INK, 1);
      ctx.restore();
      poly(ctx, [-9, -3, -12, 8, -8, 9, -6, -1], P.STEEL_DARK, P.INK, 1); // neck guard
    } else if (hat === 'turban') {
      var c = look.hatColor || P.WHITE;
      poly(ctx, [-9.5, -2, -10, -10, -4, -16, 4, -16, 10, -9, 9.5, -2], c, P.INK, 1.2);
      ctx.strokeStyle = P.INK; ctx.lineWidth = 0.8;
      ctx.beginPath(); ctx.moveTo(-9, -6); ctx.quadraticCurveTo(0, -12, 9.5, -5); ctx.moveTo(-8, -10); ctx.quadraticCurveTo(0, -15, 8, -9); ctx.stroke();
      seg(ctx, 0, -16, 0, -19, 2, P.VERMILION);
    } else if (hat === 'crown') {
      poly(ctx, [-9, -3, -10, -15, -5, -10, 0, -18, 5, -10, 10, -15, 9, -3], P.GOLD, P.INK, 1.2);
      circle(ctx, 0, -8, 1.8, P.VERMILION, false); circle(ctx, -6, -7, 1.3, P.TURQUOISE, false); circle(ctx, 6, -7, 1.3, P.TURQUOISE, false);
    } else if (hat === 'tiara') {
      poly(ctx, [-8, -6, -6, -10, 0, -13, 6, -10, 8, -6, 0, -8], P.GOLD, P.INK, 1);
      circle(ctx, 0, -10, 1.4, P.VERMILION, false);
    } else if (hat === 'cap') {
      poly(ctx, [-9, -3, -8, -12, 0, -16, 8, -12, 9, -3], look.hatColor || P.HAIR, P.INK, 1.2);
      ctx.fillStyle = P.SAFFRON; ctx.fillRect(-9, -5, 18, 2);
    } else if (hat === 'horns') {
      poly(ctx, [-5, -6, -12, -14, -13, -24, -8, -14, -1, -8], P.BEARD_WHITE, P.INK, 1.1);
      poly(ctx, [4, -7, 10, -16, 14, -24, 12, -13, 7, -5], P.BEARD_WHITE, P.INK, 1.1);
      poly(ctx, [3, 5, 5, 11, 7, 5], P.WHITE, P.INK, 0.8); // tusk
      ctx.fillStyle = P.VERMILION; ctx.beginPath(); ctx.arc(4.5, -1.5, 1.6, 0, TAU); ctx.fill();
    } else if (!look.hair) {
      poly(ctx, [-9, -1, -8, -8, 0, -10, 8, -7, 8, -4, 0, -6, -6, -2], P.HAIR, P.INK, 1);
    }
    ctx.restore();
  }

  // Draws a figure with its feet at (x, y). face = 1 looks right, -1 left.
  function figure(ctx, look, p, x, y, face, s, opts) {
    opts = opts || {};
    var size = (s || 1) * (look.size || 1);
    ctx.save();
    ctx.translate(x, y);
    ctx.scale(face * size, size);
    if (p.rot) { ctx.translate(0, p.dy || 0); ctx.rotate(p.rot); }
    else if (p.dy) ctx.translate(0, p.dy);
    if (opts.alpha !== undefined) ctx.globalAlpha = opts.alpha;

    var lb = limbPts(0, 0, p.hb, p.kb, TH, SH);
    var lf = limbPts(0, 0, p.hf, p.kf, TH, SH);
    var hipY = -Math.max(lb[3], lf[3]);
    var hx = 0;
    lb = limbPts(hx, hipY, p.hb, p.kb, TH, SH);
    lf = limbPts(hx, hipY, p.hf, p.kf, TH, SH);
    var sl = Math.sin(p.lean), cl = Math.cos(p.lean);
    var nx = hx + sl * TO, ny = hipY - cl * TO;
    var sx = hx + sl * (TO - 3), sy = hipY - cl * (TO - 3);
    var headX = hx + sl * (TO + 10), headY = hipY - cl * (TO + 10);
    var ab = armPts(sx - 2, sy, p.shB + p.lean, p.elB);
    var af = armPts(sx + 2, sy, p.shF + p.lean, p.elF);
    var skin = look.skin || P.SKIN;
    var legs = look.legs || P.LAPIS_DARK;
    var boots = look.boots || P.EARTH_DARK;
    var flash = opts.flash;

    // back leg and arm, a shade darker
    seg(ctx, hx, hipY, lb[0], lb[1], 7, shade(legs, 0.8));
    seg(ctx, lb[0], lb[1], lb[2], lb[3], 6, shade(legs, 0.8));
    seg(ctx, lb[2] - 1, lb[3] - 2, lb[2] + 5, lb[3] - 1, 5, shade(boots, 0.8));
    if (opts.weaponBack) drawWeapon(ctx, opts.weaponBack, ab[2], ab[3], ab[4], look);
    seg(ctx, sx - 2, sy, ab[0], ab[1], 6.5, look.bare ? shade(skin, 0.85) : shade(look.robe, 0.8));
    seg(ctx, ab[0], ab[1], ab[2], ab[3], 5.5, look.bare ? shade(skin, 0.85) : shade(look.robe, 0.8));
    circle(ctx, ab[2], ab[3], 3, shade(skin, 0.9), P.INK, 1);

    // skirt of the robe, hanging from the waist towards the knees
    var hem = look.long ? 1.6 : (look.bare ? 0.55 : 1.15);
    var kbx = hx + (lb[0] - hx) * hem, kby = hipY + (lb[1] - hipY) * hem;
    var kfx = hx + (lf[0] - hx) * hem, kfy = hipY + (lf[1] - hipY) * hem;
    var left = Math.min(kbx, kfx) - 6, right = Math.max(kbx, kfx) + 6;
    var bottom = Math.max(kby, kfy) + 3;
    var robe = flash ? '#fff' : look.robe;
    poly(ctx, [hx - 9 + sl * 6, hipY - 6, hx + 9 + sl * 6, hipY - 6, right, bottom, (left + right) / 2, bottom + 2, left, bottom], robe, P.INK, 1.3);
    ctx.strokeStyle = look.trim; ctx.lineWidth = 2;
    ctx.beginPath(); ctx.moveTo(left + 1, bottom - 1.5); ctx.lineTo((left + right) / 2, bottom + 0.5); ctx.lineTo(right - 1, bottom - 1.5); ctx.stroke();

    // torso
    var px = Math.cos(p.lean) * 9, py = Math.sin(p.lean) * 9;
    var tw = look.bare ? skin : robe;
    poly(ctx, [nx - px * 1.15, ny - py * 1.15, nx + px * 1.15, ny + py * 1.15, hx + 9.5, hipY - 2, hx - 9.5, hipY - 2], tw, P.INK, 1.3);
    if (look.spots) { circle(ctx, hx + sl * 18 + 2, hipY - 18, 2, look.skinShade, false); circle(ctx, hx + sl * 12 - 3, hipY - 10, 1.6, look.skinShade, false); }
    if (look.pattern) { for (var i = 0; i < 3; i++) circle(ctx, hx + sl * (8 + i * 7) - 3 + (i % 2) * 6, hipY - 8 - i * 7, 1.3, look.pattern, false); }
    if (!look.bare) {
      ctx.strokeStyle = look.trim; ctx.lineWidth = 2.6;
      ctx.beginPath(); ctx.moveTo(nx + px * 0.2, ny + py * 0.2); ctx.lineTo(hx + 5, hipY - 2); ctx.stroke();
    }
    // sash
    poly(ctx, [hx - 10 + sl * 4, hipY - 8, hx + 10 + sl * 4, hipY - 8, hx + 10, hipY - 2, hx - 10, hipY - 2], look.sash, P.INK, 1);
    if (look.sash !== look.trim) circle(ctx, hx + 6, hipY - 5, 2, P.GOLD, P.INK, 0.8);

    drawHead(ctx, look, headX, headY, p.lean);

    // front leg
    seg(ctx, hx, hipY, lf[0], lf[1], 7, legs);
    seg(ctx, lf[0], lf[1], lf[2], lf[3], 6, legs);
    seg(ctx, lf[2] - 1, lf[3] - 2, lf[2] + 5, lf[3] - 1, 5, boots);

    // front arm and what it holds
    var weapon = opts.weapon;
    if (weapon && weapon !== 'bow') drawWeapon(ctx, weapon, af[2], af[3], af[4] + p.wpn, look);
    var sleeve = look.bare ? skin : look.robe;
    seg(ctx, sx + 2, sy, af[0], af[1], 6.5, flash ? '#fff' : sleeve);
    seg(ctx, af[0], af[1], af[2], af[3], 5.5, flash ? '#fff' : sleeve);
    if (!look.bare) { ctx.strokeStyle = look.trim; ctx.lineWidth = 2; ctx.beginPath(); ctx.arc(af[2] - Math.sin(af[4]) * 3, af[3] - Math.cos(af[4]) * 3, 3, 0, TAU); ctx.stroke(); }
    circle(ctx, af[2], af[3], 3.2, skin, P.INK, 1);
    if (weapon === 'bow') { look.bowHandX = ab[2]; drawWeapon(ctx, 'bow', af[2], af[3], 0, look); }
    if (opts.item === 'feather') drawFeather(ctx, af[2] + 2, af[3] - 6, 0.5, -0.4);
    ctx.restore();
  }

  // ------------------------------------------------------------------ props

  function drawFeather(ctx, x, y, s, rot) {
    ctx.save(); ctx.translate(x, y); ctx.rotate(rot || 0); ctx.scale(s, s);
    poly(ctx, [0, 30, -8, 8, -6, -14, 0, -26, 6, -14, 8, 8], P.VERMILION, P.INK, 1.4);
    poly(ctx, [0, 10, -4, -2, 0, -14, 4, -2], P.TURQUOISE, false);
    circle(ctx, 0, -4, 3.5, P.GOLD_LIGHT, P.INK, 1);
    circle(ctx, 0, -4, 1.6, P.LAPIS, false);
    seg(ctx, 0, 30, 0, 40, 1.2, P.GOLD_DARK);
    ctx.restore();
  }

  function flame(ctx, x, y, s, t, seed) {
    var f = Math.sin(t * 13 + (seed || 0)) * 0.12 + Math.sin(t * 7.3 + (seed || 0) * 2) * 0.1;
    ctx.save(); ctx.translate(x, y); ctx.scale(s * (1 + f * 0.4), s * (1 - f));
    poly(ctx, [-8, 0, -9, -10, -3, -20, 0, -30, 4, -18, 9, -10, 8, 0], P.FIRE_OUT, false);
    poly(ctx, [-5, 0, -5, -8, -1, -16, 1, -22, 4, -12, 5, 0], P.FIRE_MID, false);
    poly(ctx, [-2.5, 0, -2, -6, 0, -11, 2.5, -6, 2.5, 0], P.FIRE_CORE, false);
    ctx.restore();
  }

  function potion(ctx, x, y, big, t) {
    var liquid = big ? P.TURQUOISE_LIGHT : P.VERMILION;
    poly(ctx, [x - 7, y, x + 7, y, x + 9, y - 10, x + 4, y - 16, x + 3, y - 22, x - 3, y - 22, x - 4, y - 16, x - 9, y - 10], P.PAPER, P.INK, 1.2);
    poly(ctx, [x - 7, y - 1, x + 7, y - 1, x + 8.5, y - 9, x - 8.5, y - 9], liquid, false);
    ctx.fillStyle = P.GOLD; ctx.fillRect(x - 4, y - 25, 8, 3);
    var b = (t * 1.3) % 1;
    circle(ctx, x - 2 + Math.sin(t * 5) * 2, y - 26 - b * 10, 1.4 * (1 - b), liquid, false);
    if (big) { ctx.strokeStyle = P.GOLD; ctx.lineWidth = 1; ctx.strokeRect(x - 10, y - 30, 20, 31); }
  }

  function leafScroll(ctx, x, y, t) {
    var bob = Math.sin(t * 3) * 2;
    ctx.save(); ctx.translate(x, y - 22 + bob); ctx.rotate(Math.sin(t * 2) * 0.08);
    ctx.shadowColor = P.GOLD_LIGHT; ctx.shadowBlur = 10;
    poly(ctx, [-10, -13, 10, -13, 10, 13, -10, 13], P.PAPER, P.INK, 1.2);
    ctx.shadowBlur = 0;
    ctx.strokeStyle = P.GOLD; ctx.lineWidth = 1.4; ctx.strokeRect(-7.5, -10.5, 15, 21);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 0.8;
    for (var i = 0; i < 4; i++) { ctx.beginPath(); ctx.moveTo(-5, -6 + i * 4.5); ctx.lineTo(5, -6 + i * 4.5); ctx.stroke(); }
    ctx.restore();
  }

  function maceOnGround(ctx, x, y, t) {
    ctx.save();
    ctx.shadowColor = P.GOLD_LIGHT; ctx.shadowBlur = 14 + Math.sin(t * 4) * 6;
    drawWeapon(ctx, 'mace', x - 20, y - 6, Math.PI / 2, {});
    ctx.restore();
  }

  // ------------------------------------------------------------------ Simorgh

  function simorgh(ctx, x, y, s, t, face) {
    ctx.save(); ctx.translate(x, y); ctx.scale(s * (face || 1), s);
    var flap = Math.sin(t * 3) * 0.25;
    var tails = [[P.VERMILION, 0], [P.TURQUOISE, 0.25], [P.GOLD, 0.5], [P.LAPIS, 0.75], [P.ROSE, 1]];
    for (var i = 0; i < tails.length; i++) {
      var k = tails[i][1], w = Math.sin(t * 2 + k * 3) * 10;
      ctx.strokeStyle = P.INK; ctx.lineWidth = 7;
      ctx.beginPath(); ctx.moveTo(-20, 10);
      ctx.bezierCurveTo(-70, 20 + k * 30, -110, -20 + k * 60 + w, -170, 10 + k * 50 + w);
      ctx.stroke();
      ctx.strokeStyle = tails[i][0]; ctx.lineWidth = 5; ctx.stroke();
      circle(ctx, -170, 10 + k * 50 + w, 6, P.GOLD_LIGHT, P.INK, 1.2);
      circle(ctx, -170, 10 + k * 50 + w, 2.5, tails[i][0], false);
    }
    // far wing
    ctx.save(); ctx.rotate(-0.3 + flap);
    poly(ctx, [0, -5, -30, -60, -10, -90, 20, -100, 50, -70, 30, -20], shade(P.LAPIS, 0.8), P.INK, 1.4);
    ctx.restore();
    // body
    poly(ctx, [-25, 10, -10, -12, 18, -18, 34, -6, 26, 16, 0, 22], P.SAFFRON, P.INK, 1.4);
    for (var j = 0; j < 4; j++) poly(ctx, [-12 + j * 10, 4, -6 + j * 10, -4, 0 + j * 10, 4, -6 + j * 10, 10], P.VERMILION, P.INK, 0.8);
    // neck and head
    poly(ctx, [20, -12, 34, -40, 46, -46, 50, -36, 34, -4], P.TURQUOISE, P.INK, 1.3);
    circle(ctx, 46, -46, 8, P.TURQUOISE_LIGHT, P.INK, 1.3);
    poly(ctx, [52, -48, 66, -42, 52, -40], P.GOLD, P.INK, 1);
    circle(ctx, 48, -48, 1.6, P.INK, false);
    for (var c = 0; c < 3; c++) poly(ctx, [42 - c * 4, -53, 34 - c * 6, -70 - c * 3, 46 - c * 4, -56], [P.VERMILION, P.GOLD, P.ROSE][c], P.INK, 0.9);
    // near wing
    ctx.save(); ctx.rotate(-0.15 - flap);
    poly(ctx, [-6, -4, -40, -40, -30, -80, 0, -96, 34, -84, 40, -44, 22, -6], P.VERMILION, P.INK, 1.4);
    for (var f = 0; f < 4; f++) poly(ctx, [-24 + f * 14, -40 - f * 8, -18 + f * 14, -78 - f * 4, -10 + f * 14, -42 - f * 8], [P.GOLD, P.TURQUOISE, P.LAPIS, P.GOLD_LIGHT][f], P.INK, 0.9);
    ctx.restore();
    ctx.restore();
  }

  // ------------------------------------------------------------------ dragon

  // d: { ax, ay (water line), hx, hy (head), mouth 0..1, t, stun, flash, sink }
  function dragon(ctx, d) {
    var t = d.t;
    ctx.save();
    if (d.sink) ctx.translate(0, d.sink);
    // coils rising out of the water
    for (var c = 0; c < 2; c++) {
      var cx = d.ax + 110 + c * 95, r = 34 - c * 6;
      ctx.lineWidth = 30 - c * 6; ctx.strokeStyle = P.INK;
      ctx.beginPath(); ctx.arc(cx, d.ay + 6, r, Math.PI, 0); ctx.stroke();
      ctx.lineWidth = 26 - c * 6; ctx.strokeStyle = d.flash ? '#fff' : '#2F7A5E';
      ctx.beginPath(); ctx.arc(cx, d.ay + 6, r, Math.PI, 0); ctx.stroke();
      for (var k = 0; k < 5; k++) {
        var a = Math.PI + (k + 0.5) * Math.PI / 5;
        poly(ctx, [cx + Math.cos(a) * (r + 10), d.ay + 6 + Math.sin(a) * (r + 10), cx + Math.cos(a + 0.12) * (r + 22), d.ay + 6 + Math.sin(a + 0.12) * (r + 22), cx + Math.cos(a + 0.25) * (r + 10), d.ay + 6 + Math.sin(a + 0.25) * (r + 10)], P.VERMILION, P.INK, 0.8);
      }
    }
    // neck: a chain of scaled discs from the water to the head
    var bx = d.ax + 40, by = d.ay + 10;
    var c1x = d.ax + 70, c1y = d.ay - 170, c2x = d.hx + 90, c2y = d.hy - 40;
    var N = 22;
    for (var i = 0; i <= N; i++) {
      var u = i / N, v = 1 - u;
      var px = v * v * v * bx + 3 * v * v * u * c1x + 3 * v * u * u * c2x + u * u * u * (d.hx + 24);
      var py = v * v * v * by + 3 * v * v * u * c1y + 3 * v * u * u * c2y + u * u * u * d.hy;
      var rr = 24 - u * 9;
      if (i % 2 === 0) poly(ctx, [px - 4, py - rr + 2, px + 2, py - rr - 12, px + 6, py - rr + 2], P.VERMILION, P.INK, 0.8);
      circle(ctx, px, py, rr, d.flash ? '#fff' : (i % 2 ? '#2F7A5E' : '#37896A'), P.INK, 1.3);
      circle(ctx, px - rr * 0.25, py + rr * 0.4, rr * 0.45, '#D8C27A', false);
    }
    // head, facing left
    ctx.save(); ctx.translate(d.hx, d.hy);
    var m = d.mouth || 0;
    ctx.save(); ctx.rotate(-m * 0.55); // upper jaw
    poly(ctx, [24, -8, 0, -22, -36, -18, -58, -8, -60, 0, 10, 2], '#37896A', P.INK, 1.5);
    for (var tt = 0; tt < 6; tt++) poly(ctx, [-54 + tt * 9, 0, -50 + tt * 9, 8, -46 + tt * 9, 0], P.WHITE, P.INK, 0.7);
    // horns and whiskers
    poly(ctx, [8, -18, 40, -46, 48, -40, 18, -10], P.GOLD, P.INK, 1.2);
    poly(ctx, [-6, -20, 18, -52, 26, -48, 4, -16], P.GOLD_LIGHT, P.INK, 1.2);
    ctx.strokeStyle = P.VERMILION; ctx.lineWidth = 3;
    ctx.beginPath(); ctx.moveTo(-56, -6); ctx.bezierCurveTo(-70, -30 + Math.sin(t * 3) * 6, -50, -40, -70, -56); ctx.stroke();
    // eye
    if (d.stun) {
      ctx.strokeStyle = P.INK; ctx.lineWidth = 1.5; ctx.beginPath();
      for (var s = 0; s < 12; s++) { var aa = s * 0.9 + t * 8, ra = s * 0.6; ctx.lineTo(-14 + Math.cos(aa) * ra, -12 + Math.sin(aa) * ra); }
      ctx.stroke();
    } else {
      circle(ctx, -14, -12, 6, P.GOLD_LIGHT, P.INK, 1.3);
      poly(ctx, [-14, -17, -12, -12, -14, -7, -16, -12], P.INK, false);
    }
    ctx.restore();
    ctx.save(); ctx.rotate(m * 0.45); // lower jaw
    poly(ctx, [16, 2, -54, 4, -50, 14, -20, 20, 14, 14], '#2F7A5E', P.INK, 1.5);
    for (var lt = 0; lt < 5; lt++) poly(ctx, [-48 + lt * 10, 5, -44 + lt * 10, -3, -40 + lt * 10, 5], P.WHITE, P.INK, 0.7);
    ctx.restore();
    if (m > 0.2) { // the throat
      ctx.globalAlpha = Math.min(1, (m - 0.2) * 2);
      circle(ctx, -18, 2, 9 * m, d.fire ? P.FIRE_MID : P.CRIMSON, false);
      if (d.fire) circle(ctx, -18, 2, 5 * m, P.FIRE_CORE, false);
      ctx.globalAlpha = 1;
    }
    ctx.restore();
    ctx.restore();
  }

  // ------------------------------------------------------------------ level art

  var THEMES = {
    fort: { back: '#B88F64', back2: '#A57C52', mortar: '#8E6A45', stone: '#D2B48A', stoneDark: '#9F7C55', floor: '#DCC29A', floorFace: '#A88760', frieze: P.LAPIS, frieze2: P.TURQUOISE },
    cave: { back: '#3F4A45', back2: '#36403C', mortar: '#2A322F', stone: '#6E7A6C', stoneDark: '#4A5549', floor: '#8C9A82', floorFace: '#5C6858', frieze: '#2E5E58', frieze2: P.TURQUOISE_LIGHT },
    alborz: { back: null, stone: '#A893B5', stoneDark: '#7D6A8C', floor: '#C9B8C9', floorFace: '#8F7C9A', frieze: P.MEADOW, frieze2: P.LEAF },
    dungeon: { back: '#4A2C38', back2: '#3E2430', mortar: '#2B1820', stone: '#7A5260', stoneDark: '#57384A', floor: '#9A7280', floorFace: '#6A4858', frieze: P.CRIMSON, frieze2: P.GOLD },
  };

  // deterministic noise so the static room art is the same every time it is cached
  function hash(a, b) {
    var h = (a * 374761393 + b * 668265263) | 0;
    h = (h ^ (h >>> 13)) * 1274126177;
    return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
  }

  function goldCloud(ctx, x, y, s) {
    ctx.save(); ctx.translate(x, y); ctx.scale(s, s);
    ctx.fillStyle = P.GOLD_LIGHT; ctx.strokeStyle = P.GOLD_DARK; ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.moveTo(-40, 0);
    ctx.bezierCurveTo(-40, -14, -22, -16, -18, -6);
    ctx.bezierCurveTo(-16, -22, 6, -24, 8, -8);
    ctx.bezierCurveTo(14, -18, 34, -14, 30, -2);
    ctx.bezierCurveTo(44, -2, 44, 10, 30, 10);
    ctx.lineTo(-30, 10);
    ctx.bezierCurveTo(-46, 10, -48, 2, -40, 0);
    ctx.fill(); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(-26, 3); ctx.quadraticCurveTo(-8, -6, 4, 3); ctx.moveTo(8, 4); ctx.quadraticCurveTo(18, -2, 26, 4); ctx.stroke();
    ctx.restore();
  }

  function miniatureRock(ctx, x, y, w, h, c1, c2, seed) {
    // the stacked, rounded "sponge" rocks of Persian painting
    var n = 4;
    for (var i = 0; i < n; i++) {
      var rx = x + hash(seed, i) * w * 0.6, ry = y + h - (i + 1) * h / n;
      var rw = w * (0.45 + hash(i, seed) * 0.4), rh = h / n * 1.5;
      ctx.beginPath();
      ctx.moveTo(rx, ry + rh);
      ctx.bezierCurveTo(rx - 6, ry + rh * 0.3, rx + rw * 0.2, ry - rh * 0.2, rx + rw * 0.5, ry);
      ctx.bezierCurveTo(rx + rw * 0.8, ry - rh * 0.3, rx + rw + 6, ry + rh * 0.4, rx + rw, ry + rh);
      ctx.closePath();
      ctx.fillStyle = i % 2 ? c1 : c2; ctx.fill();
      ctx.strokeStyle = P.INK; ctx.lineWidth = 1.2; ctx.stroke();
    }
  }

  function roomBackground(ctx, theme, room, W, H, levelId) {
    var th = THEMES[theme];
    if (theme === 'alborz') {
      var g = ctx.createLinearGradient(0, 0, 0, H);
      g.addColorStop(0, '#7FB2CF'); g.addColorStop(0.6, '#CFE0D6'); g.addColorStop(1, P.PAPER);
      ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
      ctx.globalAlpha = 0.55;
      for (var m = 0; m < 4; m++) miniatureRock(ctx, m * 180 - 40 + hash(room, m) * 60, H * 0.35 + hash(m, room) * 80, 220, 260, '#B7A6C6', '#CDBFD8', room * 7 + m);
      ctx.globalAlpha = 1;
      for (var k = 0; k < 3; k++) goldCloud(ctx, 80 + k * 220 + hash(room, k + 9) * 60, 40 + hash(k, room + 3) * 160, 0.8 + hash(k, room) * 0.5);
      return;
    }
    ctx.fillStyle = th.back; ctx.fillRect(0, 0, W, H);
    // bricks
    ctx.strokeStyle = th.mortar; ctx.lineWidth = 1;
    for (var y = 0; y < H; y += 21) {
      var off = (y / 21) % 2 ? 0 : 24;
      ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke();
      for (var x = off; x < W; x += 48) {
        if (hash(x + room * 31, y) < 0.18) { ctx.fillStyle = th.back2; ctx.fillRect(x + 1, y + 1, 47, 20); }
        ctx.beginPath(); ctx.moveTo(x, y); ctx.lineTo(x, y + 21); ctx.stroke();
      }
    }
    if (theme === 'cave') {
      ctx.fillStyle = 'rgba(20,30,28,0.35)'; ctx.fillRect(0, 0, W, H);
      for (var s = 0; s < 6; s++) {
        var sx = hash(room, s) * W, sl = 30 + hash(s, room) * 50;
        poly(ctx, [sx - 14, 0, sx + 14, 0, sx, sl], '#2C3532', P.INK, 1);
      }
    }
    // tiled frieze near the top of each row — lapis and turquoise like a Safavid portal
    for (var r = 0; r < 3; r++) {
      var fy = r * 126 + 14;
      ctx.fillStyle = th.frieze; ctx.fillRect(0, fy, W, 12);
      ctx.fillStyle = th.frieze2;
      for (var d = 8; d < W; d += 22) {
        ctx.beginPath(); ctx.moveTo(d, fy + 6); ctx.lineTo(d + 5, fy + 1.5); ctx.lineTo(d + 10, fy + 6); ctx.lineTo(d + 5, fy + 10.5); ctx.fill();
      }
      ctx.strokeStyle = P.GOLD; ctx.lineWidth = 1; ctx.strokeRect(-1, fy, W + 2, 12);
    }
    if (theme === 'dungeon') {
      // hanging banners
      for (var b = 0; b < 2; b++) {
        var bx = 120 + b * 360 + hash(room, b) * 60;
        poly(ctx, [bx, 28, bx + 34, 28, bx + 34, 100, bx + 17, 88, bx, 100], P.CRIMSON, P.INK, 1.2);
        circle(ctx, bx + 17, 56, 7, P.HAIR, P.GOLD, 1.5);
      }
    }
  }

  function block(ctx, x, y, w, h, theme, r, c) {
    var th = THEMES[theme];
    if (theme === 'alborz') {
      ctx.fillStyle = th.stoneDark; ctx.fillRect(x, y, w, h);
      miniatureRock(ctx, x - 4, y - 6, w + 8, h + 6, th.stone, th.floor, r * 13 + c);
      return;
    }
    ctx.fillStyle = th.stone; ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1;
    for (var yy = 0; yy < h; yy += 21) {
      var off = (yy / 21) % 2 ? 0 : 16;
      ctx.beginPath(); ctx.moveTo(x, y + yy); ctx.lineTo(x + w, y + yy); ctx.stroke();
      for (var xx = off; xx < w; xx += 32) {
        if (hash(r * 97 + xx, c * 13 + yy) < 0.3) { ctx.fillStyle = th.stoneDark; ctx.fillRect(x + xx + 1, y + yy + 1, Math.min(31, w - xx - 1), 20); }
        ctx.beginPath(); ctx.moveTo(x + xx, y + yy); ctx.lineTo(x + xx, y + yy + 21); ctx.stroke();
      }
    }
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1.6; ctx.strokeRect(x + 0.5, y + 0.5, w - 1, h - 1);
  }

  function floorSlab(ctx, x, y, w, theme, cracked) {
    var th = THEMES[theme];
    ctx.fillStyle = th.floorFace; ctx.fillRect(x, y, w, 11);
    ctx.fillStyle = th.floor; ctx.fillRect(x, y - 4, w, 5);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1.2;
    ctx.strokeRect(x + 0.5, y - 4, w - 1, 15);
    ctx.beginPath(); ctx.moveTo(x, y + 1); ctx.lineTo(x + w, y + 1); ctx.stroke();
    if (theme === 'alborz') { ctx.fillStyle = P.MEADOW; ctx.fillRect(x, y - 5, w, 3); }
    if (cracked) {
      ctx.beginPath(); ctx.moveTo(x + w * 0.3, y - 4); ctx.lineTo(x + w * 0.4, y + 4); ctx.lineTo(x + w * 0.33, y + 11);
      ctx.moveTo(x + w * 0.7, y - 4); ctx.lineTo(x + w * 0.62, y + 5); ctx.stroke();
    }
  }

  function pillar(ctx, x, y, theme) {
    var th = THEMES[theme];
    ctx.fillStyle = th.stone; ctx.fillRect(x - 9, y - 110, 18, 106);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1.2; ctx.strokeRect(x - 9, y - 110, 18, 106);
    poly(ctx, [x - 14, y - 112, x + 14, y - 112, x + 10, y - 104, x - 10, y - 104], P.GOLD, P.INK, 1);
    ctx.strokeStyle = th.stoneDark; ctx.beginPath(); ctx.moveTo(x - 3, y - 104); ctx.lineTo(x - 3, y - 6); ctx.moveTo(x + 4, y - 104); ctx.lineTo(x + 4, y - 6); ctx.stroke();
  }

  function door(ctx, x, y, open) {
    // a pointed Persian arch
    poly(ctx, [x - 26, y, x - 26, y - 70, x - 16, y - 90, x, y - 102, x + 16, y - 90, x + 26, y - 70, x + 26, y], P.LAPIS, P.INK, 1.5);
    poly(ctx, [x - 18, y, x - 18, y - 66, x - 10, y - 82, x, y - 90, x + 10, y - 82, x + 18, y - 66, x + 18, y], open ? P.FIRE_CORE : P.HAIR, P.GOLD, 2);
    if (open) { ctx.globalAlpha = 0.35; poly(ctx, [x - 18, y, x + 18, y, x + 30, y + 4, x - 30, y + 4], P.FIRE_CORE, false); ctx.globalAlpha = 1; }
    for (var i = 0; i < 5; i++) circle(ctx, x - 22 + i * 11, y - 74 - (i % 2) * 4, 1.8, P.TURQUOISE_LIGHT, false);
  }

  function altar(ctx, x, y, lit, t) {
    poly(ctx, [x - 14, y, x + 14, y, x + 10, y - 10, x + 6, y - 26, x - 6, y - 26, x - 10, y - 10], P.STEEL_DARK, P.INK, 1.2);
    poly(ctx, [x - 16, y - 26, x + 16, y - 26, x + 12, y - 34, x - 12, y - 34], P.GOLD, P.INK, 1.2);
    if (lit) flame(ctx, x, y - 33, 1.1, t, x);
    else { ctx.fillStyle = P.SMOKE; ctx.fillRect(x - 8, y - 37, 16, 3); }
  }

  function gate(ctx, x, top, bottom, open) {
    var h = (bottom - top) * (1 - open);
    ctx.fillStyle = P.HAIR; ctx.fillRect(x - 12, top, 24, 6);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 4;
    for (var i = -1; i <= 1; i++) { ctx.beginPath(); ctx.moveTo(x + i * 8, top); ctx.lineTo(x + i * 8, top + h); ctx.stroke(); }
    ctx.strokeStyle = P.STEEL_DARK; ctx.lineWidth = 2.2;
    for (var j = -1; j <= 1; j++) { ctx.beginPath(); ctx.moveTo(x + j * 8, top); ctx.lineTo(x + j * 8, top + h); ctx.stroke(); }
    for (var y = top + 20; y < top + h; y += 22) { ctx.strokeStyle = P.STEEL_DARK; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(x - 11, y); ctx.lineTo(x + 11, y); ctx.stroke(); }
    if (h > 4) poly(ctx, [x - 11, top + h, x - 8, top + h + 6, x - 5, top + h, x + 5, top + h, x + 8, top + h + 6, x + 11, top + h], P.STEEL, P.INK, 1);
  }

  function spikes(ctx, x, y, w, up) {
    var n = 5, hh = 8 + up * 10;
    for (var i = 0; i < n; i++) {
      var sx = x + 6 + i * (w - 12) / (n - 1);
      poly(ctx, [sx - 4, y - 3, sx, y - 3 - hh, sx + 4, y - 3], P.STEEL, P.INK, 1);
    }
  }

  function slicer(ctx, x, y, closed) {
    // two serrated iron jaws that snap shut across the passage
    var gap = 44 * (1 - closed), mid = y - 58;
    ctx.fillStyle = P.HAIR; ctx.fillRect(x - 7, y - 124, 14, 10); ctx.fillRect(x - 7, y - 6, 14, 6);
    var top = [x - 7, y - 114, x + 7, y - 114, x + 7, mid - gap];
    for (var i = 0; i < 3; i++) top.push(x + 4 - i * 5, mid - gap + (i % 2 ? 0 : 6));
    top.push(x - 7, mid - gap);
    poly(ctx, top, P.STEEL, P.INK, 1.3);
    var bot = [x - 7, y - 6, x + 7, y - 6, x + 7, mid + gap + 4];
    for (var j = 0; j < 3; j++) bot.push(x + 4 - j * 5, mid + gap + 4 - (j % 2 ? 0 : 6));
    bot.push(x - 7, mid + gap + 4);
    poly(ctx, bot, P.STEEL, P.INK, 1.3);
    ctx.fillStyle = P.STEEL_DARK; ctx.fillRect(x - 2, y - 112, 4, 50 - gap); ctx.fillRect(x - 2, mid + gap + 10, 4, 44 - gap);
    if (closed > 0.8) { ctx.fillStyle = 'rgba(255,255,255,0.8)'; ctx.fillRect(x - 9, mid - 1, 18, 3); }
  }

  function plate(ctx, x, y, w, pressed, theme) {
    var th = THEMES[theme];
    poly(ctx, [x + 8, y - 4 + pressed * 3, x + w - 8, y - 4 + pressed * 3, x + w - 6, y, x + 6, y], th.stoneDark, P.INK, 1);
    ctx.fillStyle = P.GOLD; ctx.fillRect(x + w / 2 - 6, y - 3 + pressed * 3, 12, 1.6);
  }

  function torch(ctx, x, y, t) {
    poly(ctx, [x - 3, y, x + 3, y, x + 5, y - 14, x - 5, y - 14], P.EARTH_DARK, P.INK, 1);
    ctx.save(); ctx.globalAlpha = 0.18; circle(ctx, x, y - 22, 26 + Math.sin(t * 9 + x) * 3, P.FIRE_MID, false); ctx.restore();
    flame(ctx, x, y - 13, 0.7, t, x);
  }

  function target(ctx, x, y, hit) {
    seg(ctx, x, y, x, y - 48, 3, P.EARTH_DARK);
    ctx.save(); ctx.translate(x, y - 62);
    circle(ctx, 0, 0, 15, hit ? P.STEEL : P.WHITE, P.INK, 1.4);
    circle(ctx, 0, 0, 10, hit ? P.STEEL_DARK : P.VERMILION, P.INK, 1);
    circle(ctx, 0, 0, 4.5, P.GOLD, P.INK, 1);
    ctx.restore();
  }

  function water(ctx, x, y, w, h, t) {
    var g = ctx.createLinearGradient(0, y, 0, y + h);
    g.addColorStop(0, '#3D7FA0'); g.addColorStop(1, '#183A55');
    ctx.fillStyle = g; ctx.fillRect(x, y, w, h);
    ctx.strokeStyle = P.WHITE; ctx.lineWidth = 1.4;
    for (var row = 0; row < 3; row++) {
      ctx.beginPath();
      for (var i = 0; i <= w; i += 8) {
        var yy = y + 6 + row * 14 + Math.sin(i * 0.25 + t * 2.5 + row * 2 + x * 0.1) * 2.5;
        if (i === 0) ctx.moveTo(x + i, yy); else ctx.lineTo(x + i, yy);
      }
      ctx.globalAlpha = 0.6 - row * 0.15; ctx.stroke();
    }
    ctx.globalAlpha = 1;
  }

  function bridge(ctx, x, y, w, amount) {
    if (amount <= 0) return;
    var len = w * amount;
    ctx.fillStyle = P.EARTH; ctx.fillRect(x, y - 4, len, 10);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1;
    for (var i = 0; i < len; i += 12) ctx.strokeRect(x + i, y - 4, Math.min(12, len - i), 10);
  }

  function rubble(ctx, x, y, theme) {
    var th = THEMES[theme];
    for (var i = 0; i < 4; i++) poly(ctx, [x + 8 + i * 13, y, x + 12 + i * 13, y - 7 - (i % 2) * 3, x + 20 + i * 13, y], i % 2 ? th.stone : th.stoneDark, P.INK, 1);
  }

  function rock(ctx, x, y, rot) {
    ctx.save(); ctx.translate(x, y); ctx.rotate(rot);
    poly(ctx, [-10, -6, -2, -12, 9, -8, 11, 3, 3, 10, -9, 7], '#8A7A70', P.INK, 1.3);
    ctx.restore();
  }

  function fireball(ctx, x, y, t) {
    ctx.save(); ctx.globalAlpha = 0.25; circle(ctx, x, y - 14, 26, P.FIRE_MID, false); ctx.restore();
    flame(ctx, x, y, 1.5, t, x * 0.1);
    flame(ctx, x + 12, y, 1.0, t + 0.3, x * 0.2);
  }

  function arrow(ctx, x, y, ang) {
    ctx.save(); ctx.translate(x, y); ctx.rotate(ang);
    seg(ctx, -22, 0, 6, 0, 1.6, P.EARTH);
    poly(ctx, [6, -3, 14, 0, 6, 3], P.STEEL, P.INK, 0.8);
    poly(ctx, [-22, 0, -27, -4, -18, 0, -27, 4], P.VERMILION, P.INK, 0.6);
    ctx.restore();
  }

  // ------------------------------------------------------------------ cutscene paintings

  function frame(ctx, W, H) {
    // the gold rulings of a manuscript page around the painting
    ctx.strokeStyle = P.GOLD; ctx.lineWidth = 6; ctx.strokeRect(8, 8, W - 16, H - 16);
    ctx.strokeStyle = P.LAPIS; ctx.lineWidth = 2; ctx.strokeRect(14, 14, W - 28, H - 28);
    ctx.strokeStyle = P.INK; ctx.lineWidth = 1; ctx.strokeRect(5, 5, W - 10, H - 10);
  }

  function sky(ctx, W, H, top, bottom) {
    var g = ctx.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, top); g.addColorStop(1, bottom);
    ctx.fillStyle = g; ctx.fillRect(0, 0, W, H);
  }

  function ground(ctx, W, y, c1, c2) {
    ctx.fillStyle = c1; ctx.fillRect(0, y, W, 400 - y);
    ctx.fillStyle = c2;
    for (var i = 0; i < 30; i++) {
      var x = hash(i, 7) * W, yy = y + 10 + hash(7, i) * (380 - y);
      ctx.beginPath(); ctx.moveTo(x, yy); ctx.lineTo(x - 3, yy - 8); ctx.moveTo(x, yy); ctx.lineTo(x + 3, yy - 8);
      ctx.strokeStyle = c2; ctx.lineWidth = 1.4; ctx.stroke();
    }
  }

  function cypress(ctx, x, y, h) {
    poly(ctx, [x, y - h, x + 12, y - h * 0.5, x + 9, y, x - 9, y, x - 12, y - h * 0.5], P.LEAF_DARK, P.INK, 1.2);
    ctx.strokeStyle = P.LEAF; ctx.lineWidth = 1;
    for (var i = 1; i < 6; i++) { ctx.beginPath(); ctx.moveTo(x - 6, y - h * i / 6); ctx.lineTo(x, y - h * i / 6 - 8); ctx.lineTo(x + 6, y - h * i / 6); ctx.stroke(); }
  }

  function fortress(ctx, x, y, s, c) {
    ctx.save(); ctx.translate(x, y); ctx.scale(s, s);
    poly(ctx, [-90, 0, -90, -90, 90, -90, 90, 0], c, P.INK, 1.5);
    for (var i = -90; i < 90; i += 20) poly(ctx, [i, -90, i, -102, i + 12, -102, i + 12, -90], c, P.INK, 1.2);
    poly(ctx, [-120, 0, -120, -130, -86, -130, -86, 0], shade(c, 0.9), P.INK, 1.5);
    poly(ctx, [86, 0, 86, -130, 120, -130, 120, 0], shade(c, 0.9), P.INK, 1.5);
    poly(ctx, [-124, -130, -103, -160, -82, -130], P.LAPIS, P.INK, 1.2);
    poly(ctx, [82, -130, 103, -160, 124, -130], P.LAPIS, P.INK, 1.2);
    poly(ctx, [-20, 0, -20, -40, 0, -56, 20, -40, 20, 0], P.HAIR, P.GOLD, 2);
    ctx.restore();
  }

  function pardeh(ctx, x, y, w, h, t) {
    // the storyteller's painted curtain (pardeh), with scenes from Sam's tale
    poly(ctx, [x, y, x + w, y, x + w, y + h, x, y + h], '#E9D7AE', P.INK, 2);
    ctx.strokeStyle = P.CRIMSON; ctx.lineWidth = 6; ctx.strokeRect(x + 6, y + 6, w - 12, h - 12);
    ctx.save();
    ctx.beginPath(); ctx.rect(x + 10, y + 10, w - 20, h - 20); ctx.clip();
    ctx.fillStyle = '#F0E2BE'; ctx.fillRect(x + 10, y + 10, w - 20, h - 20);
    // the dragon coiling in the middle
    dragon(ctx, { ax: x + w * 0.42, ay: y + h * 0.82, hx: x + w * 0.28, hy: y + h * 0.42, mouth: 0.4, t: t, fire: true });
    simorgh(ctx, x + w * 0.78, y + h * 0.35, 0.32, t, -1);
    figure(ctx, LOOKS.sam, POSES.raise, x + w * 0.16, y + h * 0.9, 1, 0.9, { weapon: 'mace' });
    ctx.restore();
    seg(ctx, x - 10, y, x + w + 10, y, 5, P.EARTH_DARK);
  }

  var SCENES = {
    coffeehouse: function (ctx, W, H, t) {
      sky(ctx, W, H, '#3A1F14', '#1E120C');
      for (var i = 0; i < 3; i++) { // oil lamps
        var lx = 110 + i * 210;
        ctx.save(); ctx.globalAlpha = 0.2; circle(ctx, lx, 70, 70, P.FIRE_MID, false); ctx.restore();
        seg(ctx, lx, 0, lx, 52, 1.2, P.GOLD_DARK);
        flame(ctx, lx, 68, 0.8, t, i);
      }
      pardeh(ctx, 220, 70, 360, 220, t);
      ctx.fillStyle = '#4A2E1E'; ctx.fillRect(0, 330, W, 70);
      ctx.strokeStyle = P.GOLD_DARK; ctx.lineWidth = 2; ctx.beginPath(); ctx.moveTo(0, 330); ctx.lineTo(W, 330); ctx.stroke();
      // tea glasses on the takht
      for (var g = 0; g < 4; g++) { poly(ctx, [40 + g * 18, 344, 50 + g * 18, 344, 48 + g * 18, 330, 42 + g * 18, 330], 'rgba(200,80,40,0.8)', P.INK, 1); }
      var pointing = Math.sin(t * 0.8) > 0;
      figure(ctx, LOOKS.naqqal, pointing ? POSES.point : POSES.stand, 160, 360, 1, 2.3, { weapon: 'staff' });
    },
    nariman: function (ctx, W, H, t) {
      sky(ctx, W, H, '#E9A65A', '#F4E8CC');
      fortress(ctx, 470, 290, 1.1, '#D2B48A');
      ground(ctx, W, 290, '#C2A06E', '#9C7A4C');
      cypress(ctx, 90, 300, 140);
      figure(ctx, LOOKS.nariman, POSES.point, 210, 370, 1, 2.0, { weapon: 'staff' });
      figure(ctx, LOOKS.sam, POSES.stand, 350, 370, -1, 1.9, {});
    },
    mace: function (ctx, W, H, t) {
      sky(ctx, W, H, '#2B1B12', '#5A2E1E');
      ctx.save(); ctx.globalAlpha = 0.35 + Math.sin(t * 3) * 0.1; circle(ctx, 320, 120, 120, P.GOLD_LIGHT, false); ctx.restore();
      for (var i = 0; i < 12; i++) { var a = i / 12 * TAU + t * 0.2; seg(ctx, 320 + Math.cos(a) * 60, 120 + Math.sin(a) * 60, 320 + Math.cos(a) * 140, 120 + Math.sin(a) * 140, 2, P.GOLD); }
      ctx.fillStyle = '#6B4626'; ctx.fillRect(0, 330, W, 70);
      figure(ctx, LOOKS.sam, POSES.raise, 320, 370, 1, 2.4, { weapon: 'mace' });
    },
    court: function (ctx, W, H, t) {
      sky(ctx, W, H, P.PLUM, '#3B1D3D');
      // arcade of arches
      for (var i = 0; i < 5; i++) poly(ctx, [40 + i * 120, 330, 40 + i * 120, 120, 70 + i * 120, 80, 100 + i * 120, 120, 100 + i * 120, 330], P.LAPIS_DARK, P.GOLD, 2);
      ctx.fillStyle = '#E6D3A8'; ctx.fillRect(0, 320, W, 80);
      for (var k = 0; k < W; k += 40) { ctx.fillStyle = (k / 40) % 2 ? P.TURQUOISE : P.LAPIS; ctx.fillRect(k, 320, 40, 8); }
      poly(ctx, [380, 330, 380, 230, 560, 230, 560, 330], P.GOLD, P.INK, 1.5); // throne
      poly(ctx, [400, 230, 470, 170, 540, 230], P.CRIMSON, P.INK, 1.5);
      figure(ctx, LOOKS.manuchehr, POSES.point, 470, 345, -1, 2.1, {});
      figure(ctx, LOOKS.sam, POSES.bow, 230, 370, 1, 2.1, { weapon: 'mace' });
    },
    river: function (ctx, W, H, t) {
      sky(ctx, W, H, '#5C3A3A', '#B0705A');
      for (var m = 0; m < 3; m++) miniatureRock(ctx, m * 230 - 30, 120, 260, 180, '#8F7C9A', '#A893B5', m + 3);
      water(ctx, 0, 270, W, 130, t);
      dragon(ctx, { ax: 380, ay: 290, hx: 300 + Math.sin(t) * 20, hy: 150 + Math.cos(t * 1.3) * 10, mouth: 0.5 + Math.sin(t * 2) * 0.3, t: t, fire: true });
      poly(ctx, [0, 300, 170, 290, 210, 400, 0, 400], '#9C7A4C', P.INK, 1.5);
      figure(ctx, LOOKS.sam, POSES.shoot, 110, 330, 1, 1.6, { weapon: 'bow', weaponBack: null });
    },
    dragon_dead: function (ctx, W, H, t) {
      sky(ctx, W, H, '#F2C46B', '#F4E8CC');
      circle(ctx, 500, 110, 50, '#FBE3A0', P.GOLD, 2);
      water(ctx, 0, 270, W, 130, t);
      ctx.fillStyle = 'rgba(80,150,60,0.35)'; ctx.fillRect(0, 270, W, 130);
      dragon(ctx, { ax: 380, ay: 290, hx: 300, hy: 290, mouth: 0.15, t: t, stun: true, sink: 30 });
      poly(ctx, [0, 300, 170, 290, 210, 400, 0, 400], '#9C7A4C', P.INK, 1.5);
      figure(ctx, LOOKS.sam, POSES.raise, 110, 330, 1, 1.6, { weapon: 'mace' });
    },
    zal_birth: function (ctx, W, H, t) {
      sky(ctx, W, H, '#3B1D3D', '#5A2E5E');
      poly(ctx, [140, 330, 140, 110, 320, 60, 500, 110, 500, 330], P.LAPIS_DARK, P.GOLD, 3);
      ctx.fillStyle = '#E6D3A8'; ctx.fillRect(0, 330, W, 70);
      // cradle with the white-haired child
      poly(ctx, [270, 320, 370, 320, 380, 290, 260, 290], P.EARTH, P.INK, 1.5);
      poly(ctx, [285, 292, 355, 292, 350, 276, 290, 276], P.WHITE, P.INK, 1);
      circle(ctx, 300, 278, 10, P.SKIN, P.INK, 1.2);
      poly(ctx, [290, 274, 296, 264, 306, 266, 312, 274, 300, 270], P.BEARD_WHITE, P.INK, 1);
      ctx.save(); ctx.globalAlpha = 0.25 + Math.sin(t * 2) * 0.08; circle(ctx, 300, 278, 34, P.GOLD_LIGHT, false); ctx.restore();
      figure(ctx, LOOKS.sam, POSES.hurt, 480, 370, 1, 2.0, {});
    },
    alborz: function (ctx, W, H, t) {
      sky(ctx, W, H, '#7FB2CF', '#F4E8CC');
      miniatureRock(ctx, 300, 40, 300, 340, '#A893B5', '#C9B8C9', 5);
      miniatureRock(ctx, -40, 160, 280, 240, '#8F7C9A', '#B7A6C6', 9);
      goldCloud(ctx, 160, 70, 1.2); goldCloud(ctx, 480, 30, 0.9);
      poly(ctx, [410, 60, 500, 60, 490, 40, 420, 40], P.EARTH, P.INK, 1.4); // nest
      simorgh(ctx, 250 + Math.sin(t * 0.7) * 40, 150, 0.7, t, 1);
      figure(ctx, LOOKS.sam, POSES.point, 120, 380, 1, 1.4, {});
    },
    dream: function (ctx, W, H, t) {
      sky(ctx, W, H, P.LAPIS_NIGHT, P.LAPIS_DARK);
      for (var i = 0; i < 50; i++) circle(ctx, hash(i, 1) * W, hash(1, i) * 260, 1 + hash(i, i) * 1.6, P.GOLD_LIGHT, false);
      ctx.save(); ctx.globalAlpha = 0.5 + Math.sin(t * 1.5) * 0.2;
      figure(ctx, LOOKS.mobed, POSES.point, 420, 330, -1, 1.8, {});
      ctx.restore();
      ctx.fillStyle = '#2A2440'; ctx.fillRect(0, 330, W, 70);
      poly(ctx, [120, 340, 330, 340, 330, 360, 120, 360], P.CRIMSON, P.INK, 1.4); // bed
      figure(ctx, LOOKS.sam, POSES.dead, 270, 344, -1, 1.3, {});
    },
    simorgh: function (ctx, W, H, t) {
      sky(ctx, W, H, '#7FB2CF', '#F4E8CC');
      miniatureRock(ctx, 360, 120, 300, 280, '#A893B5', '#C9B8C9', 2);
      goldCloud(ctx, 140, 60, 1.1);
      ground(ctx, W, 330, '#B7C98A', P.LEAF);
      simorgh(ctx, 380, 120 + Math.sin(t) * 6, 0.85, t, -1);
      figure(ctx, LOOKS.zal, POSES.stand, 330, 370, -1, 1.9, { item: 'feather' });
      figure(ctx, LOOKS.sam, POSES.kneel, 180, 370, 1, 1.9, {});
    },
    mazandaran: function (ctx, W, H, t) {
      sky(ctx, W, H, '#2B1020', '#6A2030');
      fortress(ctx, 330, 300, 1.4, '#57384A');
      ground(ctx, W, 300, '#3E2430', '#2B1820');
      for (var i = 0; i < 4; i++) figure(ctx, LOOKS.div, POSES.fight, 380 + i * 60, 360 + (i % 2) * 14, -1, 1.2, { weapon: 'club' });
      figure(ctx, LOOKS.karkoy, POSES.point, 330, 330, -1, 1.5, { weapon: 'sword' });
      figure(ctx, LOOKS.sam, POSES.fight, 120, 380, 1, 1.8, { weapon: 'mace' });
    },
    karkoy_down: function (ctx, W, H, t) {
      sky(ctx, W, H, '#E9A65A', '#F4E8CC');
      fortress(ctx, 470, 300, 1.0, '#7A5260');
      ground(ctx, W, 300, '#9A7280', '#6A4858');
      figure(ctx, LOOKS.karkoy, POSES.dead, 380, 380, 1, 1.6, {});
      figure(ctx, LOOKS.sam, POSES.raise, 220, 380, 1, 1.9, { weapon: 'mace' });
    },
    rostam: function (ctx, W, H, t) {
      sky(ctx, W, H, '#F2C46B', '#F4E8CC');
      ground(ctx, W, 320, '#B7C98A', P.LEAF);
      cypress(ctx, 560, 330, 160); cypress(ctx, 70, 330, 130);
      goldCloud(ctx, 320, 60, 1.2);
      figure(ctx, LOOKS.zal, POSES.stand, 380, 370, -1, 1.8, {});
      figure(ctx, LOOKS.rudabeh, POSES.stand, 450, 370, -1, 1.75, {});
      // the infant Rostam in Rudabeh's arms
      circle(ctx, 432, 268, 8, P.SKIN, P.INK, 1.2);
      poly(ctx, [424, 272, 446, 272, 444, 290, 426, 290], P.WHITE, P.INK, 1);
      figure(ctx, LOOKS.sam, POSES.point, 200, 370, 1, 1.9, { weapon: 'mace' });
    },
  };

  function scene(ctx, name, W, H, t) {
    ctx.save();
    (SCENES[name] || SCENES.coffeehouse)(ctx, W, H, t);
    ctx.restore();
    frame(ctx, W, H);
  }

  return {
    LOOKS: LOOKS, POSES: POSES, THEMES: THEMES,
    pose: pose, lerpPose: lerpPose, run: run, figure: figure, shade: shade,
    poly: poly, circle: circle, seg: seg, flame: flame, potion: potion, leafScroll: leafScroll,
    maceOnGround: maceOnGround, simorgh: simorgh, dragon: dragon, drawFeather: drawFeather,
    roomBackground: roomBackground, block: block, floorSlab: floorSlab, pillar: pillar, door: door,
    altar: altar, gate: gate, spikes: spikes, slicer: slicer, plate: plate, torch: torch, target: target,
    water: water, bridge: bridge, rubble: rubble, rock: rock, fireball: fireball, arrow: arrow,
    goldCloud: goldCloud, scene: scene, frame: frame, hash: hash,
  };
})();
