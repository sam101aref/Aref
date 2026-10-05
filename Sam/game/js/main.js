// The game: screens, the play loop, checkpoints, saving, and the story flow between chapters.
var SAM = window.SAM || (window.SAM = {});

SAM.Game = (function () {
  var VW = 640, VH = 412, A = SAM.Art, UI = SAM.UI, P = SAM.PAL, TW = SAM.TW, TH = SAM.TH;
  var save = SAM.Save.data;

  function Game(canvas) {
    this.cv = canvas;
    this.ctx = canvas.getContext('2d');
    this.k = 1;
    this.mode = 'boot';
    this.t = 0;
    this.buttons = [];
    this.focus = 0;
    this.toasts = [];
    SAM.lang = save.lang || 'fa';
    SAM.Audio.setEnabled(save.sound !== false);
    this.resize();
    var self = this;
    window.addEventListener('resize', function () { self.resize(); });
    canvas.addEventListener('pointerdown', function (e) { self.pointer(e); });
    document.addEventListener('visibilitychange', function () { if (document.hidden && self.mode === 'play' && !self.overlay) self.pause(); });
    if (save.lang) this.toTitle(); else this.toLang();
  }

  var G = Game.prototype;

  G.resize = function () {
    var s = Math.min(window.innerWidth / VW, window.innerHeight / VH);
    var dpr = Math.min(window.devicePixelRatio || 1, 2);
    this.cv.style.width = Math.floor(VW * s) + 'px';
    this.cv.style.height = Math.floor(VH * s) + 'px';
    this.cv.width = Math.floor(VW * s * dpr);
    this.cv.height = Math.floor(VH * s * dpr);
    this.k = (VW * s * dpr) / VW;
    if (this.world) this.world.cache = {};
  };

  G.pointer = function (e) {
    SAM.Audio.unlock();
    var r = this.cv.getBoundingClientRect();
    var x = (e.clientX - r.left) / r.width * VW, y = (e.clientY - r.top) / r.height * VH;
    if (e.pointerType === 'touch' && SAM.inputMode !== 'touch') SAM.inputMode = 'touch';
    for (var i = 0; i < this.buttons.length; i++) {
      var b = this.buttons[i];
      if (!b.disabled && x >= b.x && x <= b.x + b.w && y >= b.y && y <= b.y + b.h) { SAM.Audio.play('select'); b.action(); return; }
    }
    if (this.mode === 'story') this.advanceStory();
  };

  // ---------------------------------------------------------------- screens

  G.setButtons = function (list) { this.buttons = list; this.focus = 0; };

  G.toLang = function () {
    this.mode = 'lang';
    SAM.Touch.show(false);
    var self = this;
    this.setButtons([
      { label: 'فارسی', x: 120, y: 200, w: 180, h: 60, size: 24, action: function () { self.setLang('fa'); self.toTitle(); } },
      { label: 'English', x: 340, y: 200, w: 180, h: 60, size: 22, action: function () { self.setLang('en'); self.toTitle(); } },
    ]);
    if (navigator.language && !/^fa/i.test(navigator.language)) this.focus = 1;
  };

  G.setLang = function (l) {
    SAM.lang = l; save.lang = l; SAM.Save.write();
    SAM.Touch.relabel();
  };

  G.toTitle = function () {
    this.mode = 'title';
    this.overlay = null;
    this.world = null; this.player = null;
    SAM.Touch.show(false);
    SAM.Audio.music('calm');
    var self = this, y = 344, list = [];
    var canContinue = save.resume > 0 || save.unlocked > 1;
    list.push({ label: SAM.t(canContinue ? 'cont' : 'start'), action: function () { self.beginChapter(canContinue ? Math.min(save.unlocked, SAM.LEVELS.length) - 1 : 0); } });
    list.push({ label: SAM.t('chapters'), action: function () { self.toChapters(); } });
    list.push({ label: SAM.t('controls'), action: function () { self.toControls(); } });
    list.push({ label: SAM.t('language'), action: function () { self.setLang(SAM.lang === 'fa' ? 'en' : 'fa'); self.toTitle(); self.focus = 3; } });
    list.push({ label: SAM.t(SAM.Audio.isEnabled() ? 'soundOn' : 'soundOff'), action: function () { self.toggleSound(); self.toTitle(); self.focus = 4; } });
    var w = 112, gap = 8, x0 = (VW - (w * list.length + gap * (list.length - 1))) / 2;
    list.forEach(function (b, i) { b.x = x0 + i * (w + gap); b.y = y; b.w = w; b.h = 36; b.size = 13; });
    if (SAM.lang === 'fa') list.forEach(function (b) { b.x = VW - b.x - b.w; });
    this.setButtons(list);
  };

  G.toggleSound = function () {
    SAM.Audio.setEnabled(!SAM.Audio.isEnabled());
    save.sound = SAM.Audio.isEnabled(); SAM.Save.write();
  };

  G.toChapters = function () {
    this.mode = 'chapters';
    var self = this, list = [];
    SAM.LEVELS.forEach(function (lv, i) {
      var x = 32 + i * 148;
      if (SAM.lang === 'fa') x = VW - x - 132;
      list.push({ label: '', x: x, y: 74, w: 132, h: 250, level: i, disabled: i + 1 > save.unlocked, card: true, action: function () { self.beginChapter(i); } });
    });
    list.push({ label: SAM.t('back'), x: VW / 2 - 70, y: 340, w: 140, h: 36, action: function () { self.toTitle(); } });
    this.setButtons(list);
  };

  G.toControls = function () {
    this.mode = 'controls';
    var self = this;
    this.setButtons([{ label: SAM.t('back'), x: VW / 2 - 70, y: 348, w: 140, h: 34, action: function () { self.toTitle(); } }]);
  };

  // ---------------------------------------------------------------- story

  G.story = function (key, then) {
    this.mode = 'story';
    SAM.Touch.show(false);
    this.st = { beats: SAM.STORY[key], i: 0, t: 0, then: then };
    var self = this;
    this.setButtons([{ label: SAM.t('skip'), x: SAM.lang === 'fa' ? 18 : VW - 108, y: 14, w: 90, h: 26, size: 12, action: function () { self.endStory(); } }]);
    SAM.Audio.music('calm');
  };

  G.beatText = function () {
    var b = this.st.beats[this.st.i];
    return SAM.lang === 'fa' ? b[2] : b[3];
  };

  G.advanceStory = function () {
    var st = this.st, full = this.beatText().length;
    if (st.t * 45 < full) { st.t = full / 45 + 0.01; return; }
    st.i++; st.t = 0;
    if (st.i >= st.beats.length) this.endStory();
  };

  G.endStory = function () {
    var then = this.st.then;
    this.st = null;
    then();
  };

  // ---------------------------------------------------------------- chapters

  G.beginChapter = function (i) {
    var self = this, lv = SAM.LEVELS[i];
    this.story(lv.intro, function () { self.startLevel(i); });
  };

  G.startLevel = function (i) {
    this.level = i;
    this.checkpoint = null;
    this.deaths = 0;
    this.time = 0;
    save.resume = i; SAM.Save.write();
    this.spawn(null);
    this.mode = 'play';
    SAM.Input.clear();
    SAM.Touch.show(SAM.inputMode === 'touch');
  };

  G.spawn = function (cp) {
    var lv = SAM.LEVELS[this.level];
    this.world = new SAM.World(this.level, cp ? cp.snap : null);
    SAM.currentTheme = lv.theme;
    var w = this.world, keep = cp ? cp.snap.player : { maxHp: 3 + this.level, hasMace: !lv.needMace };
    var x = cp ? cp.x : w.start.c * TW + TW / 2, y = cp ? cp.y : (w.start.r + 1) * TH;
    this.player = new SAM.Player(w, x, y, keep);
    w.leavesTaken = 0;
    w.forCells(function (c) { if (c.f === 'leaf' && c.taken) w.leavesTaken++; });
    this.boss = null; this.overlay = null; this.deadT = 0; this.hint = null; this.toasts = [];
    this.shake = 0; this.flash = 0;
    this.setButtons([]);
    SAM.Audio.music(lv.music);
    SAM.Touch.relabel();
  };

  G.pause = function () {
    var self = this;
    this.overlay = 'pause';
    this.overlayButtons([
      [SAM.t('resume'), function () { self.overlay = null; self.setButtons([]); }],
      [SAM.t('restart'), function () { self.startLevel(self.level); }],
      [SAM.t(SAM.Audio.isEnabled() ? 'soundOn' : 'soundOff'), function () { self.toggleSound(); self.pause(); self.focus = 2; }],
      [SAM.t('toMenu'), function () { self.toTitle(); }],
    ]);
  };

  G.overlayButtons = function (items) {
    var list = items.map(function (it, i) { return { label: it[0], action: it[1], x: VW / 2 - 90, y: 160 + i * 46, w: 180, h: 38 }; });
    this.setButtons(list);
  };

  G.retry = function () {
    this.deaths++;
    this.spawn(this.checkpoint);
    SAM.Input.clear();
  };

  G.finishLevel = function () {
    if (this.overlay === 'complete') return;
    var lv = SAM.LEVELS[this.level], w = this.world, self = this;
    save.unlocked = Math.max(save.unlocked, Math.min(SAM.LEVELS.length, this.level + 2));
    save.leaves[lv.id] = Math.max(save.leaves[lv.id] || 0, w.leavesTaken);
    if (!save.best[lv.id] || this.time < save.best[lv.id]) save.best[lv.id] = Math.round(this.time);
    save.resume = Math.min(SAM.LEVELS.length - 1, this.level + 1);
    SAM.Save.write();
    SAM.Audio.play('fanfare');
    this.overlay = 'complete';
    this.overlayT = 0;
    this.overlayButtons([[SAM.t('tapToGo'), function () { self.afterLevel(); }]]);
    this.buttons[0].y = 300;
  };

  G.afterLevel = function () {
    var self = this, i = this.level, lv = SAM.LEVELS[i];
    this.story(lv.outro, function () {
      if (i + 1 < SAM.LEVELS.length) self.beginChapter(i + 1);
      else self.story('ending', function () { self.mode = 'theend'; self.endT = 0; self.setButtons([{ label: SAM.t('toMenu'), x: VW / 2 - 80, y: 320, w: 160, h: 38, action: function () { self.toTitle(); } }]); });
    });
  };

  // ---------------------------------------------------------------- update

  G.update = function (dt) {
    this.t += dt;
    var inp = SAM.Input.read();
    var pr = inp.pressed;

    if (this.mode !== 'play' || this.overlay) {
      // menu navigation with the keyboard
      var n = this.buttons.length;
      if (n) {
        var prev = (SAM.lang === 'fa' && this.mode === 'title') || (SAM.lang === 'fa' && this.mode === 'chapters') ? pr.right : pr.left;
        var next = (SAM.lang === 'fa' && this.mode === 'title') || (SAM.lang === 'fa' && this.mode === 'chapters') ? pr.left : pr.right;
        if (pr.up || prev) { do { this.focus = (this.focus - 1 + n) % n; } while (this.buttons[this.focus].disabled); }
        if (pr.down || next) { do { this.focus = (this.focus + 1) % n; } while (this.buttons[this.focus].disabled); }
      }
      if (this.mode === 'story') {
        this.st.t += dt;
        if (pr.enter || pr.jump || pr.attack) this.advanceStory();
        if (pr.pause) this.endStory();
        return;
      }
      if ((pr.enter || pr.jump) && n && !this.buttons[this.focus].disabled) { SAM.Audio.play('select'); this.buttons[this.focus].action(); return; }
      if (pr.pause && this.overlay === 'pause') { this.overlay = null; this.setButtons([]); }
      else if (pr.pause && (this.mode === 'chapters' || this.mode === 'controls')) this.toTitle();
      if (this.overlay === 'complete') this.overlayT += dt;
      return;
    }

    if (pr.pause) return this.pause();
    this.time += dt;
    var w = this.world, pl = this.player;
    var steps = 2, sdt = dt / steps;
    for (var s = 0; s < steps; s++) {
      var frameInp = s === 0 ? inp : { pressed: {}, left: inp.left, right: inp.right, up: inp.up, down: inp.down, walk: inp.walk };
      pl.update(sdt, frameInp);
      for (var i = 0; i < w.enemies.length; i++) w.enemies[i].update(sdt, pl);
      w.update(sdt, pl);
    }
    this.events();

    // hint scrolls on the floor
    if (pl.onGround()) {
      var cell = w.cell(w.rowOfFeet(pl.y), w.colOf(pl.x));
      if (cell && cell.f === 'hint') this.hint = { text: SAM.hint(w.def.id, cell.n), life: 3.5 };
    }
    if (this.hint) { this.hint.life -= dt; if (this.hint.life <= 0) this.hint = null; }
    this.toasts.forEach(function (t) { t.life -= dt; });
    this.toasts = this.toasts.filter(function (t) { return t.life > 0; });
    this.shake = Math.max(0, this.shake - dt * 30);
    this.flash = Math.max(0, this.flash - dt);

    if (pl.dead) {
      this.deadT += dt;
      if (this.deadT > 1.1 && pl.feather) {
        pl.feather = false;
        var at = /fall|drown|spikes|slicer/.test(pl.reason) ? pl.safe : { x: pl.x, y: pl.y };
        pl.revive(at.x, at.y);
        w.particles(at.x, at.y - 50, P.GOLD_LIGHT, 30, 200);
        SAM.Audio.play('feather');
        this.toast('feather');
        this.deadT = 0;
      } else if (this.deadT > 1.4 && !this.overlay) {
        var self = this;
        this.overlay = 'dead';
        this.overlayButtons([[SAM.t('retry'), function () { self.retry(); }], [SAM.t('toMenu'), function () { self.toTitle(); }]]);
        this.buttons.forEach(function (b) { b.y += 60; });
      }
    }
  };

  G.toast = function (key) {
    var text = SAM.t(key);
    if (this.toasts.some(function (t) { return t.text === text; })) return;
    this.toasts.push({ text: text, life: 2.4 });
  };

  G.events = function () {
    var w = this.world, ev = w.events;
    w.events = [];
    for (var i = 0; i < ev.length; i++) {
      var e = ev[i];
      switch (e.type) {
        case 'sound': SAM.Audio.play(e.data); break;
        case 'toast': this.toast(e.data); break;
        case 'shake': this.shake = Math.max(this.shake, e.data); break;
        case 'flash': this.flash = 0.25; break;
        case 'checkpoint': this.checkpoint = { snap: w.snapshot(this.player), x: e.data.x, y: e.data.y }; break;
        case 'boss': this.boss = e.data; SAM.Audio.music('battle'); break;
        case 'complete': this.finishLevel(); break;
      }
    }
  };

  // ---------------------------------------------------------------- draw

  G.draw = function () {
    var ctx = this.ctx;
    ctx.setTransform(this.k, 0, 0, this.k, 0, 0);
    ctx.fillStyle = '#000'; ctx.fillRect(0, 0, VW, VH);
    switch (this.mode) {
      case 'lang': this.drawLang(ctx); break;
      case 'title': this.drawTitle(ctx); break;
      case 'chapters': this.drawChapters(ctx); break;
      case 'controls': this.drawControls(ctx); break;
      case 'story': this.drawStory(ctx); break;
      case 'play': this.drawPlay(ctx); break;
      case 'theend': this.drawEnd(ctx); break;
    }
    if (this.mode !== 'story' && this.mode !== 'play' && this.mode !== 'chapters') this.drawButtons(ctx);
    if (SAM.inputMode === 'touch' && window.innerHeight > window.innerWidth) {
      ctx.fillStyle = 'rgba(0,0,0,0.75)'; ctx.fillRect(0, 0, VW, VH);
      UI.text(ctx, '⟲  ' + SAM.t('rotate'), VW / 2, VH / 2, { size: 26, color: P.GOLD_LIGHT });
    }
  };

  G.drawButtons = function (ctx) {
    for (var i = 0; i < this.buttons.length; i++) if (!this.buttons[i].card) UI.button(ctx, this.buttons[i], i === this.focus);
  };

  G.drawLang = function (ctx) {
    UI.paper(ctx, VW, VH);
    A.goldCloud(ctx, 150, 90, 1); A.goldCloud(ctx, 490, 90, 1);
    UI.text(ctx, 'سام نریمان', VW / 2, 88, { size: 44, display: true, color: P.LAPIS, stroke: P.PAPER });
    UI.text(ctx, 'SAM, SON OF NARIMAN', VW / 2, 135, { size: 16, color: P.CRIMSON });
    UI.text(ctx, 'زبان را انتخاب کنید · Choose your language', VW / 2, 310, { size: 15 });
  };

  G.drawTitle = function (ctx) {
    ctx.save(); ctx.scale(1, VH / 400); A.scene(ctx, 'coffeehouse', VW, 400, this.t); ctx.restore();
    var g = ctx.createLinearGradient(0, 300, 0, VH);
    g.addColorStop(0, 'rgba(20,10,5,0)'); g.addColorStop(1, 'rgba(20,10,5,0.9)');
    ctx.fillStyle = g; ctx.fillRect(0, 290, VW, 110);
    UI.text(ctx, SAM.t('title'), VW / 2, 46, { size: SAM.lang === 'fa' ? 46 : 34, display: true, color: P.GOLD_LIGHT, stroke: P.INK, strokeW: 6 });
    UI.text(ctx, SAM.t('subtitle'), VW / 2, 84, { size: 15, color: P.PAPER, stroke: P.INK, strokeW: 4 });
  };

  G.drawChapters = function (ctx) {
    UI.paper(ctx, VW, VH);
    UI.text(ctx, SAM.t('chapters'), VW / 2, 44, { size: 28, display: true, color: P.LAPIS });
    var self = this;
    this.buttons.forEach(function (b, i) {
      if (!b.card) return UI.button(ctx, b, i === self.focus);
      var lv = SAM.LEVELS[b.level], focused = i === self.focus;
      A.poly(ctx, [b.x, b.y, b.x + b.w, b.y, b.x + b.w, b.y + b.h, b.x, b.y + b.h], focused ? P.GOLD_LIGHT : P.PAPER_DARK, focused ? P.CRIMSON : P.INK, focused ? 2.5 : 1.4);
      ctx.save();
      ctx.beginPath(); ctx.rect(b.x + 8, b.y + 8, b.w - 16, 110); ctx.clip();
      ctx.translate(b.x + 8, b.y + 8); ctx.scale((b.w - 16) / VW, 110 / 400);
      var sc = SAM.STORY[lv.intro][0][0];
      A.scene(ctx, sc, VW, 400, self.t);
      ctx.restore();
      if (b.disabled) { ctx.fillStyle = 'rgba(43,27,18,0.55)'; ctx.fillRect(b.x + 8, b.y + 8, b.w - 16, 110); UI.text(ctx, '🔒', b.x + b.w / 2, b.y + 63, { size: 26 }); }
      UI.text(ctx, SAM.t('chapter') + ' ' + SAM.num(lv.id), b.x + b.w / 2, b.y + 140, { size: 13, color: P.CRIMSON });
      var lines = UI.wrap(ctx, SAM.t(lv.name), b.w - 14, UI.font(15, true));
      lines.forEach(function (l, j) { UI.text(ctx, l, b.x + b.w / 2, b.y + 166 + j * 22, { size: 15, display: true, color: P.LAPIS_DARK }); });
      var got = save.leaves[lv.id] || 0;
      for (var k = 0; k < 3; k++) {
        ctx.save(); ctx.globalAlpha = k < got ? 1 : 0.25;
        A.poly(ctx, [b.x + b.w / 2 - 30 + k * 30 - 7, b.y + 222, b.x + b.w / 2 - 30 + k * 30 + 7, b.y + 222, b.x + b.w / 2 - 30 + k * 30 + 7, b.y + 238, b.x + b.w / 2 - 30 + k * 30 - 7, b.y + 238], P.PAPER, P.GOLD, 1.5);
        ctx.restore();
      }
    });
  };

  G.drawControls = function (ctx) {
    UI.paper(ctx, VW, VH);
    UI.text(ctx, SAM.t('ctl_title'), VW / 2, 40, { size: 26, display: true, color: P.LAPIS });
    var K = SAM.KEYNAMES.keys, T = SAM.KEYNAMES.touch;
    var rows = [
      [K.left + ' ' + K.right + '  (A D)', T.left + ' ' + T.right, 'ctl_move'],
      [K.up + '  (W)', T.up, 'ctl_up'],
      [K.down + '  (S)', T.down, 'ctl_down'],
      [K.jump + '  (Z)', SAM.pick(T.jump), 'ctl_jump'],
      [K.walk, SAM.pick(T.walk), 'ctl_walk'],
      [K.attack + '  (K)', SAM.pick(T.attack), 'ctl_attack'],
      [K.parry + '  (L)', SAM.pick(T.parry), 'ctl_parry'],
      [K.shoot + '  (I)', SAM.pick(T.shoot), 'ctl_shoot'],
      ['Esc  (P)', '❚❚', 'ctl_pause'],
    ];
    var fa = SAM.lang === 'fa';
    rows.forEach(function (r, i) {
      var y = 80 + i * 29;
      UI.text(ctx, r[0], fa ? 560 : 80, y, { size: 13, color: P.CRIMSON });
      UI.text(ctx, r[1], fa ? 470 : 170, y, { size: 13, color: P.LAPIS });
      UI.text(ctx, SAM.t(r[2]), fa ? 420 : 220, y, { size: 13, align: fa ? 'right' : 'left' });
    });
  };

  G.drawStory = function (ctx) {
    var st = this.st, beat = st.beats[st.i];
    UI.paper(ctx, VW, VH);
    // the painting sits on the page like an illustration in a Shahnameh manuscript
    var s = 0.68, pw = VW * s, ph = 400 * s, px = (VW - pw) / 2, py = 22;
    ctx.save();
    ctx.beginPath(); ctx.rect(px, py, pw, ph); ctx.clip();
    ctx.translate(px, py); ctx.scale(s, s);
    A.scene(ctx, beat[0], VW, 400, this.t);
    ctx.restore();
    var who = beat[1] ? SAM.t('who_' + beat[1]) : '';
    var text = SAM.lang === 'fa' ? beat[2] : beat[3];
    var shown = text.slice(0, Math.floor(st.t * 45));
    var fa = SAM.lang === 'fa';
    var tx = fa ? VW - 44 : 44;
    if (who) UI.text(ctx, who, tx, py + ph + 18, { size: 14, color: P.CRIMSON, align: fa ? 'right' : 'left', display: true });
    var f = UI.font(15);
    var lines = UI.wrap(ctx, text, VW - 88, f);
    var used = 0;
    lines.forEach(function (l, i) {
      var part = shown.length >= used + l.length ? l : shown.slice(used, Math.max(used, shown.length));
      used += l.length + 1;
      if (part) UI.text(ctx, part, tx, py + ph + 44 + i * 24, { size: 15, align: fa ? 'right' : 'left' });
    });
    if (shown.length >= text.length && Math.sin(this.t * 5) > 0) UI.text(ctx, fa ? '◂' : '▸', fa ? 30 : VW - 30, VH - 26, { size: 18, color: P.CRIMSON });
    this.drawButtons(ctx);
  };

  G.drawPlay = function (ctx) {
    var w = this.world, pl = this.player;
    var room = w.roomOf(pl.x, pl.y - 50);
    var ox = room.rx * SAM.ROOM_W, oy = room.ry * SAM.ROOM_H;
    ctx.save();
    if (this.shake > 0) ctx.translate((Math.random() - 0.5) * this.shake, (Math.random() - 0.5) * this.shake);
    ctx.save(); ctx.beginPath(); ctx.rect(0, 0, SAM.ROOM_W, SAM.VIEW_H); ctx.clip();
    w.drawRoom(ctx, room, this.k);
    w.drawDynamic(ctx, room, pl);
    for (var i = 0; i < w.enemies.length; i++) {
      var e = w.enemies[i];
      if (e.isDragon || (e.x > ox - 60 && e.x < ox + SAM.ROOM_W + 60 && e.y > oy && e.y <= oy + SAM.ROOM_H + 2)) e.draw(ctx, ox, oy);
    }
    pl.draw(ctx, ox, oy);
    w.drawEntities(ctx, room);
    ctx.restore();
    ctx.restore();
    if (this.flash > 0) { ctx.fillStyle = 'rgba(194,65,45,' + this.flash * 1.2 + ')'; ctx.fillRect(0, 0, VW, SAM.VIEW_H); }
    this.drawHud(ctx);

    if (this.hint) {
      ctx.save(); ctx.globalAlpha = Math.min(1, this.hint.life * 2);
      var lines = UI.wrap(ctx, this.hint.text, 520, UI.font(14));
      var hh = 18 + lines.length * 21;
      A.poly(ctx, [50, 10, VW - 50, 10, VW - 40, 10 + hh / 2, VW - 50, 10 + hh, 50, 10 + hh, 40, 10 + hh / 2], 'rgba(244,232,204,0.95)', P.GOLD_DARK, 2);
      lines.forEach(function (l, j) { UI.text(ctx, l, VW / 2, 20 + 10 + j * 21, { size: 14 }); });
      ctx.restore();
    }
    var ty = this.hint ? 96 : 30;
    this.toasts.forEach(function (t, j) {
      ctx.save(); ctx.globalAlpha = Math.min(1, t.life * 2);
      UI.text(ctx, t.text, VW / 2, ty + j * 26, { size: 17, display: true, color: P.GOLD_LIGHT, stroke: P.INK, strokeW: 4 });
      ctx.restore();
    });
    if (this.overlay) {
      ctx.fillStyle = 'rgba(14,8,4,0.72)'; ctx.fillRect(0, 0, VW, VH);
      if (this.overlay === 'pause') UI.text(ctx, SAM.t('paused'), VW / 2, 110, { size: 32, display: true, color: P.GOLD_LIGHT });
      if (this.overlay === 'dead') {
        UI.text(ctx, SAM.t('die_' + pl.reason), VW / 2, 150, { size: 28, display: true, color: P.VERMILION, stroke: P.INK, strokeW: 4 });
      }
      if (this.overlay === 'complete') {
        var lv = w.def;
        UI.text(ctx, SAM.t('complete'), VW / 2, 90, { size: 32, display: true, color: P.GOLD_LIGHT });
        UI.text(ctx, SAM.t(lv.name), VW / 2, 130, { size: 18, color: P.PAPER });
        UI.text(ctx, SAM.t('time') + ': ' + SAM.clock(this.time), VW / 2, 180, { size: 16, color: P.PAPER });
        UI.text(ctx, SAM.t('leaves') + ': ' + SAM.num(w.leavesTaken) + ' / ' + SAM.num(3), VW / 2, 210, { size: 16, color: P.PAPER });
        UI.text(ctx, SAM.t('deaths') + ': ' + SAM.num(this.deaths), VW / 2, 240, { size: 16, color: P.PAPER });
      }
      this.drawButtons(ctx);
    }
  };

  G.drawHud = function (ctx) {
    var pl = this.player, w = this.world, fa = SAM.lang === 'fa';
    var y = SAM.VIEW_H;
    ctx.fillStyle = P.INK; ctx.fillRect(0, y, VW, VH - y);
    ctx.fillStyle = P.GOLD_DARK; ctx.fillRect(0, y, VW, 1.5);
    var hx = fa ? VW - 16 : 16;
    UI.hearts(ctx, hx, y + 11, pl.hp, pl.maxHp, fa ? -1 : 1);
    if (this.boss && !this.boss.dead) {
      // the foe's strength replaces the chapter name while a boss fight lasts
      var bw = 150, bx = VW / 2 - bw / 2 + (fa ? -50 : 50);
      ctx.fillStyle = P.HAIR; ctx.fillRect(bx, y + 7, bw, 9);
      ctx.fillStyle = P.VERMILION; ctx.fillRect(bx, y + 7, bw * this.boss.hp / this.boss.maxHp, 9);
      ctx.strokeStyle = P.GOLD; ctx.lineWidth = 1; ctx.strokeRect(bx, y + 7, bw, 9);
      UI.text(ctx, SAM.t(this.boss.isDragon ? 'dragon' : 'who_karkoy'), fa ? bx + bw + 12 : bx - 12, y + 12, { size: 12, color: P.GOLD_LIGHT, align: fa ? 'left' : 'right' });
    } else UI.text(ctx, SAM.t(w.def.name), VW / 2, y + 12, { size: 12, color: P.PAPER_DARK });
    var lx = fa ? 22 : VW - 22;
    for (var i = 0; i < 3; i++) {
      var x = lx + (fa ? 1 : -1) * i * 16;
      ctx.save(); ctx.globalAlpha = i < w.leavesTaken ? 1 : 0.25;
      A.poly(ctx, [x - 5, y + 4, x + 5, y + 4, x + 5, y + 18, x - 5, y + 18], P.PAPER, P.GOLD, 1.2);
      ctx.restore();
    }
    if (pl.feather) A.drawFeather(ctx, fa ? 80 : VW - 80, y + 11, 0.3, 0.6);
    if (this.time > 0) UI.text(ctx, SAM.clock(this.time), fa ? 140 : VW - 140, y + 12, { size: 11, color: P.STEEL });
  };

  G.drawEnd = function (ctx) {
    this.endT += 1 / 60;
    UI.paper(ctx, VW, VH);
    A.goldCloud(ctx, 150, 100, 1.1); A.goldCloud(ctx, 490, 100, 1.1);
    UI.text(ctx, SAM.t('ending'), VW / 2, 120, { size: 44, display: true, color: P.LAPIS });
    UI.text(ctx, SAM.t('title'), VW / 2, 190, { size: 22, display: true, color: P.CRIMSON });
    UI.text(ctx, SAM.t('credits'), VW / 2, 236, { size: 15 });
    var total = 0; for (var k in save.leaves) total += save.leaves[k];
    UI.text(ctx, SAM.t('leaves') + ': ' + SAM.num(total) + ' / ' + SAM.num(SAM.LEVELS.length * 3), VW / 2, 270, { size: 14, color: P.EARTH_DARK });
  };

  return Game;
})();

// The Android app's back button: pause or step back a screen; returns false on the title screen so the app closes.
SAM.onBack = function () {
  var g = SAM.game;
  if (!g || g.mode === 'title' || g.mode === 'lang') return false;
  if (g.mode === 'story') g.endStory();
  else if (g.mode === 'play' && !g.overlay) g.pause();
  else if (g.mode === 'play' && g.overlay === 'pause') { g.overlay = null; g.setButtons([]); }
  else g.toTitle();
  return true;
};

(function () {
  function boot() {
    SAM.Touch.build();
    var game = SAM.game = new SAM.Game(document.getElementById('game'));
    var last = performance.now();
    function frame(now) {
      var dt = Math.min(1 / 30, (now - last) / 1000);
      last = now;
      if (!SAM.manual) game.update(dt); // tests set SAM.manual and step the game themselves
      game.draw();
      requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', boot);
  else boot();
})();
