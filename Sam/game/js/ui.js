// Input (keyboard and touch), saved progress, and the screens drawn on the canvas.
var SAM = window.SAM || (window.SAM = {});

// ------------------------------------------------------------------ save

SAM.Save = (function () {
  var KEY = 'sam-nariman-v1';
  var data = { lang: null, sound: true, unlocked: 1, leaves: {}, best: {}, resume: 0 };
  try {
    var raw = window.localStorage.getItem(KEY);
    if (raw) { var d = JSON.parse(raw); for (var k in d) data[k] = d[k]; }
  } catch (e) { /* storage can be unavailable; the game still runs */ }
  return {
    data: data,
    write: function () { try { window.localStorage.setItem(KEY, JSON.stringify(data)); } catch (e) { /* ignore */ } },
  };
})();

// ------------------------------------------------------------------ input

SAM.Input = (function () {
  var held = {}, pressed = {};
  var KEYS = {
    ArrowLeft: 'left', KeyA: 'left', ArrowRight: 'right', KeyD: 'right',
    ArrowUp: 'up', KeyW: 'up', ArrowDown: 'down', KeyS: 'down',
    Space: 'jump', KeyZ: 'jump', KeyJ: 'jump',
    KeyX: 'attack', KeyK: 'attack', KeyC: 'parry', KeyL: 'parry', KeyV: 'shoot', KeyI: 'shoot',
    ShiftLeft: 'walk', ShiftRight: 'walk', Escape: 'pause', KeyP: 'pause', Enter: 'enter',
  };

  function down(a) { if (!held[a]) pressed[a] = true; held[a] = true; }
  function up(a) { held[a] = false; }

  window.addEventListener('keydown', function (e) {
    var a = KEYS[e.code];
    if (!a) return;
    e.preventDefault();
    SAM.Audio.unlock();
    if (SAM.inputMode !== 'keys') { SAM.inputMode = 'keys'; SAM.Touch && SAM.Touch.show(false); }
    down(a);
  });
  window.addEventListener('keyup', function (e) { var a = KEYS[e.code]; if (a) up(a); });
  window.addEventListener('blur', function () { held = {}; });

  return {
    down: down, up: up,
    // a snapshot for one frame; "pressed" fires once per key press
    read: function () {
      var s = { pressed: pressed };
      for (var k in held) s[k] = held[k];
      pressed = {};
      return s;
    },
    clear: function () { held = {}; pressed = {}; },
  };
})();

SAM.Touch = (function () {
  var root = null, visible = false;
  var BUTTONS = [
    // id, label, action, css position — laid out in the side margins a phone leaves around the 16:10 view
    ['tu', '▲', 'up', 'left:32px;bottom:196px'],
    ['tl', '◀', 'left', 'left:4px;bottom:120px'],
    ['tr', '▶', 'right', 'left:62px;bottom:120px'],
    ['td', '▼', 'down', 'left:32px;bottom:44px'],
    ['tj', ['پرش', 'JUMP'], 'jump', 'right:6px;bottom:14px;width:68px;height:68px'],
    ['ta', ['گرز', 'STRIKE'], 'attack', 'right:12px;bottom:92px'],
    ['tp', ['دفاع', 'BLOCK'], 'parry', 'right:12px;bottom:158px'],
    ['tb', ['کمان', 'BOW'], 'shoot', 'right:12px;bottom:224px'],
    ['tw', ['آهسته', 'SLOW'], 'walk', 'right:76px;bottom:20px;width:46px;height:46px;font-size:11px'],
    ['tx', '❚❚', 'pause', 'right:10px;top:10px;width:42px;height:42px'],
  ];

  function build() {
    root = document.getElementById('touch');
    BUTTONS.forEach(function (b) {
      var el = document.createElement('div');
      el.className = 'tbtn';
      el.id = b[0];
      el.setAttribute('style', b[3]);
      el.dataset.label = JSON.stringify(b[1]);
      var action = b[2];
      el.addEventListener('pointerdown', function (e) {
        e.preventDefault(); el.setPointerCapture(e.pointerId); el.classList.add('on');
        SAM.Audio.unlock(); SAM.Input.down(action);
      });
      var release = function (e) { e.preventDefault(); el.classList.remove('on'); SAM.Input.up(action); };
      el.addEventListener('pointerup', release);
      el.addEventListener('pointercancel', release);
      el.addEventListener('lostpointercapture', release);
      root.appendChild(el);
    });
    relabel();
  }

  function relabel() {
    if (!root) return;
    Array.prototype.forEach.call(root.children, function (el) {
      var l = JSON.parse(el.dataset.label);
      el.textContent = Array.isArray(l) ? SAM.pick(l) : l;
    });
    var bow = document.getElementById('tb');
    if (bow) bow.style.display = SAM.game && SAM.game.player && SAM.game.player.hasBow ? '' : 'none';
  }

  function show(on) {
    visible = on;
    if (root) root.style.display = on ? 'block' : 'none';
    relabel();
  }

  window.addEventListener('touchstart', function () {
    if (SAM.inputMode !== 'touch') { SAM.inputMode = 'touch'; if (SAM.game && SAM.game.mode === 'play') show(true); }
  }, { passive: true });

  return { build: build, show: show, relabel: relabel, isVisible: function () { return visible; } };
})();

// ------------------------------------------------------------------ drawing helpers

SAM.UI = (function () {
  var P = SAM.PAL, A = SAM.Art;

  function font(size, display) {
    if (display) return size + 'px Lalezar, Vazirmatn, Tahoma, sans-serif';
    return size + 'px Vazirmatn, Tahoma, sans-serif';
  }

  function text(ctx, s, x, y, o) {
    o = o || {};
    ctx.save();
    ctx.font = o.font || font(o.size || 16, o.display);
    ctx.direction = SAM.lang === 'fa' ? 'rtl' : 'ltr';
    ctx.textAlign = o.align || 'center';
    ctx.textBaseline = o.base || 'middle';
    if (o.stroke) { ctx.lineJoin = 'round'; ctx.lineWidth = o.strokeW || 4; ctx.strokeStyle = o.stroke; ctx.strokeText(s, x, y); }
    ctx.fillStyle = o.color || P.INK;
    ctx.fillText(s, x, y);
    ctx.restore();
  }

  function wrap(ctx, s, maxW, f) {
    ctx.save();
    ctx.font = f;
    ctx.direction = SAM.lang === 'fa' ? 'rtl' : 'ltr';
    var words = s.split(' '), lines = [], line = '';
    for (var i = 0; i < words.length; i++) {
      var test = line ? line + ' ' + words[i] : words[i];
      if (ctx.measureText(test).width > maxW && line) { lines.push(line); line = words[i]; }
      else line = test;
    }
    if (line) lines.push(line);
    ctx.restore();
    return lines;
  }

  function paper(ctx, W, H) {
    ctx.fillStyle = P.PAPER; ctx.fillRect(0, 0, W, H);
    // margin with gold rulings and a small floral border
    ctx.strokeStyle = P.GOLD; ctx.lineWidth = 3; ctx.strokeRect(10, 10, W - 20, H - 20);
    ctx.strokeStyle = P.LAPIS; ctx.lineWidth = 1; ctx.strokeRect(16, 16, W - 32, H - 32);
    for (var x = 30; x < W - 20; x += 40) { A.circle(ctx, x, 13, 2, P.VERMILION, false); A.circle(ctx, x, H - 13, 2, P.VERMILION, false); }
  }

  // buttons are plain records; the game keeps the current list for hit tests and keyboard focus
  function button(ctx, b, focused) {
    ctx.save();
    var fill = b.disabled ? 'rgba(80,60,40,0.35)' : focused ? P.GOLD_LIGHT : P.PAPER;
    A.poly(ctx, [b.x + 8, b.y, b.x + b.w - 8, b.y, b.x + b.w, b.y + b.h / 2, b.x + b.w - 8, b.y + b.h, b.x + 8, b.y + b.h, b.x, b.y + b.h / 2], fill, focused ? P.CRIMSON : P.INK, focused ? 2.2 : 1.4);
    text(ctx, b.label, b.x + b.w / 2, b.y + b.h / 2 + 1, { size: b.size || 15, color: b.disabled ? P.EARTH_DARK : P.INK });
    ctx.restore();
  }

  function hearts(ctx, x, y, hp, max, dir) {
    for (var i = 0; i < max; i++) {
      var hx = x + dir * i * 14;
      A.poly(ctx, [hx - 6, y - 5, hx + 6, y - 5, hx, y + 6], i < hp ? P.VERMILION : 'rgba(255,255,255,0.12)', i < hp ? P.GOLD_LIGHT : P.STEEL_DARK, 1);
    }
  }

  return { font: font, text: text, wrap: wrap, paper: paper, button: button, hearts: hearts };
})();
