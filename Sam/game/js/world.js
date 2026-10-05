// The level world: cells, traps, falling things, checkpoints, and drawing the current room.
var SAM = window.SAM || (window.SAM = {});

SAM.TW = 64;          // cell width
SAM.TH = 126;         // cell height (one storey)
SAM.ROOM_W = 640;     // 10 cells
SAM.ROOM_H = 378;     // 3 cells
SAM.VIEW_H = 390;     // the room plus the edge of its bottom floor
SAM.HANG_DY = 102;    // feet below the ledge while hanging

SAM.World = (function () {
  var TW = SAM.TW, TH = SAM.TH, A = SAM.Art;

  var FEATURE = {
    '_': '', '|': 'pillar', 't': 'torch', '^': 'spikes', '~': 'loose', 'x': 'slicer', 'r': 'rocks',
    'h': 'potion', 'H': 'bigpotion', '*': 'leaf', 'F': 'fire', 'S': 'start', 'E': 'exit', 'M': 'mace',
    'g': 'guard', 'K': 'boss', 'D': 'dragon', 'o': 'target',
  };

  function World(index, snap) {
    var def = SAM.LEVELS[index];
    this.index = index;
    this.def = def;
    this.theme = def.theme;
    this.rows = def.map.length;
    this.cols = def.map[0].length;
    this.roomsWide = this.cols / 10;
    this.cells = [];
    this.opened = {};
    this.entities = [];
    this.enemies = [];
    this.time = 0;
    this.cache = {};
    this.leavesTaken = 0;
    this.start = null;
    this.events = []; // messages for the game: toasts, sounds, shakes

    for (var r = 0; r < this.rows; r++) {
      var row = [];
      for (var c = 0; c < this.cols; c++) {
        var k = def.map[r][c];
        var cell = { k: k, r: r, c: c, type: 'floor', f: '' };
        if (k === '.') cell.type = 'air';
        else if (k === '#') cell.type = 'block';
        else if (k === 'w') { cell.type = 'air'; cell.f = 'water'; }
        else if (k === 'O') { cell.type = 'air'; cell.f = 'bridge'; cell.amount = 0; }
        else if (/[a-c]/.test(k)) { cell.f = 'plate'; cell.letter = k.toUpperCase(); cell.pressed = 0; }
        else if (/[A-C]/.test(k)) { cell.f = 'gate'; cell.letter = k; cell.open = 0; }
        else if (/[1-9]/.test(k)) { cell.f = 'hint'; cell.n = +k; }
        else cell.f = FEATURE[k] || '';
        if (cell.f === 'rocks') cell.timer = 0.6 + (c % 4) * 0.55;
        if (cell.f === 'loose') { cell.shake = -1; }
        if (k === 'S') this.start = { r: r, c: c };
        row.push(cell);
      }
      this.cells.push(row);
    }

    var self = this;
    this.forCells(function (cell) {
      var x = cell.c * TW + TW / 2, y = (cell.r + 1) * TH;
      if (cell.f === 'guard') self.enemies.push(new SAM.Guard(self, x, y, def.enemy));
      if (cell.f === 'boss') self.enemies.push(new SAM.Guard(self, x, y, 'karkoy'));
      if (cell.f === 'dragon') self.enemies.push(new SAM.Dragon(self, x, y));
    });
    this.enemies.forEach(function (e, i) { e.id = i; });
    if (snap) this.applySnapshot(snap);
  }

  var W = World.prototype;

  W.forCells = function (fn) {
    for (var r = 0; r < this.rows; r++) for (var c = 0; c < this.cols; c++) fn(this.cells[r][c]);
  };

  W.cell = function (r, c) {
    if (r < 0 || r >= this.rows || c < 0 || c >= this.cols) return null;
    return this.cells[r][c];
  };

  W.solid = function (r, c) {
    if (c < 0 || c >= this.cols) return true;
    if (r < 0 || r >= this.rows) return false;
    return this.cells[r][c].type === 'block';
  };

  W.hasFloor = function (r, c) {
    var cell = this.cell(r, c);
    if (!cell) return false;
    if (cell.f === 'bridge') return cell.amount >= 1;
    return cell.type === 'floor';
  };

  W.standable = function (r, c) {
    if (r < 0 || r >= this.rows) return false;
    return !this.solid(r, c) && (this.hasFloor(r, c) || this.solid(r + 1, c));
  };

  // open air: a cell you can fall through
  W.open = function (r, c) {
    return !this.solid(r, c) && !this.standable(r, c) && !this.gateBlocks(r, c);
  };

  W.gateBlocks = function (r, c) {
    var cell = this.cell(r, c);
    return !!(cell && cell.f === 'gate' && cell.open < 0.8);
  };

  W.colOf = function (x) { return Math.floor(x / TW); };
  W.rowOfFeet = function (y) { return Math.floor((y - 0.5) / TH); };

  // Moves a body {x, y, h, hw} horizontally, stopping at blocks and closed gates.
  W.moveX = function (b, dx) {
    if (!dx) return false;
    var nx = b.x + dx;
    var top = this.rowOfFeet(b.y - b.h + 4), bot = this.rowOfFeet(b.y - 2);
    var dir = dx > 0 ? 1 : -1;
    var lead = nx + dir * b.hw;
    var c = this.colOf(lead);
    for (var r = top; r <= bot; r++) {
      if (this.solid(r, c)) {
        b.x = dir > 0 ? c * TW - b.hw - 0.01 : (c + 1) * TW + b.hw + 0.01;
        return true;
      }
    }
    // gate barriers stand in the middle of their cell
    var c0 = this.colOf(Math.min(b.x, nx) - b.hw), c1 = this.colOf(Math.max(b.x, nx) + b.hw);
    for (var cc = c0; cc <= c1; cc++) {
      for (var rr = top; rr <= bot; rr++) {
        if (!this.gateBlocks(rr, cc)) continue;
        var gx = cc * TW + TW / 2;
        if (dir > 0 && b.x + b.hw <= gx - 5 + 0.5 && nx + b.hw > gx - 5) { b.x = gx - 5 - b.hw; return true; }
        if (dir < 0 && b.x - b.hw >= gx + 5 - 0.5 && nx - b.hw < gx + 5) { b.x = gx + 5 + b.hw; return true; }
      }
    }
    b.x = nx;
    return false;
  };

  // Finds a ledge the hands can catch. face: direction the body faces.
  W.findLedge = function (x, handY, face, tolX, up, down) {
    var best = null;
    var c0 = this.colOf(x) - 2, c1 = this.colOf(x) + 2;
    for (var r = Math.floor((handY - up) / TH) - 1; r <= Math.floor((handY + down) / TH); r++) {
      var L = (r + 1) * TH;
      if (L < handY - up || L > handY + down) continue;
      for (var c = c0; c <= c1; c++) {
        if (!this.standable(r, c)) continue;
        var n = c - face; // the open side faces the body
        if (!this.open(r, n)) continue;
        if (this.solid(r + 1, n)) continue;
        var edge = face > 0 ? c * TW : (c + 1) * TW;
        var reach = x + face * 10;
        var d = Math.abs(edge - reach);
        if (d <= tolX && (!best || d < best.d)) best = { r: r, c: c, x: edge, y: L, d: d };
      }
    }
    return best;
  };

  W.emit = function (type, data) { this.events.push({ type: type, data: data }); };

  // ---------------------------------------------------------------- updates

  W.update = function (dt, player) {
    this.time += dt;
    var self = this;
    this.forCells(function (cell) {
      switch (cell.f) {
        case 'gate':
          if (self.opened[cell.letter] && cell.open < 1) cell.open = Math.min(1, cell.open + dt * 0.9);
          break;
        case 'bridge':
          if (self.opened.O && cell.amount < 1) cell.amount = Math.min(1, cell.amount + dt * 1.6);
          break;
        case 'plate':
          cell.pressed = Math.max(0, cell.pressed - dt * 4);
          break;
        case 'loose':
          if (cell.shake >= 0) {
            cell.shake += dt;
            if (cell.shake > 0.45) self.dropLoose(cell);
          }
          break;
        case 'rocks':
          cell.timer -= dt;
          if (cell.timer <= 0) { cell.timer = 2.4; self.spawnRock(cell); }
          break;
      }
    });
    for (var i = this.entities.length - 1; i >= 0; i--) {
      if (this.entities[i].update(dt, this, player) === false) this.entities.splice(i, 1);
    }
  };

  W.slicerClosed = function (cell) {
    var ph = (this.time + cell.c * 0.37) % 1.8;
    if (ph < 0.12) return ph / 0.12;
    if (ph < 0.32) return 1;
    if (ph < 0.5) return 1 - (ph - 0.32) / 0.18;
    return 0;
  };

  W.press = function (cell) {
    if (cell.f !== 'plate') return;
    if (cell.pressed < 0.5 && !this.opened[cell.letter]) {
      this.opened[cell.letter] = true;
      this.emit('sound', 'gate');
      this.emit('toast', 'gate');
    }
    cell.pressed = 1;
  };

  W.stepOn = function (r, c) {
    var cell = this.cell(r, c);
    if (!cell) return;
    if (cell.f === 'loose' && cell.shake < 0) { cell.shake = 0; this.emit('sound', 'shake'); }
    if (cell.f === 'plate') this.press(cell);
  };

  W.dropLoose = function (cell) {
    cell.type = 'air';
    cell.f = 'fallen';
    this.emit('sound', 'crumble');
    this.entities.push(new Debris(cell.c * TW + TW / 2, (cell.r + 1) * TH));
  };

  W.spawnRock = function (cell) {
    var rr = cell.r;
    while (rr - 1 >= 0 && !this.solid(rr - 1, cell.c) && !this.hasFloor(rr - 1, cell.c)) rr--;
    this.entities.push(new Rock(cell.c * TW + TW / 2 + (Math.random() - 0.5) * 16, rr * TH + 12, (cell.r + 1) * TH));
  };

  W.hitTarget = function (cell) {
    if (cell.hit) return;
    cell.hit = true;
    this.opened.O = true;
    this.emit('sound', 'gate');
    this.emit('toast', 'bridge');
  };

  W.particles = function (x, y, color, n, spread) {
    for (var i = 0; i < n; i++) this.entities.push(new Particle(x, y, color, spread || 120));
  };

  // ---------------------------------------------------------------- checkpoints

  W.snapshot = function (player) {
    var snap = { opened: JSON.parse(JSON.stringify(this.opened)), gone: [], dead: [], player: player.saveState() };
    this.forCells(function (cell) {
      if (cell.taken || cell.f === 'fallen' || cell.lit || cell.hit) snap.gone.push([cell.r, cell.c, cell.f, !!cell.taken, !!cell.lit, !!cell.hit]);
    });
    this.enemies.forEach(function (e) { if (e.dead) snap.dead.push(e.id); });
    return snap;
  };

  W.applySnapshot = function (snap) {
    this.opened = JSON.parse(JSON.stringify(snap.opened));
    var self = this;
    this.forCells(function (cell) {
      if (cell.f === 'gate' && self.opened[cell.letter]) cell.open = 1;
      if (cell.f === 'bridge' && self.opened.O) cell.amount = 1;
    });
    snap.gone.forEach(function (g) {
      var cell = self.cells[g[0]][g[1]];
      if (g[2] === 'fallen') { cell.type = 'air'; cell.f = 'fallen'; }
      if (g[3]) cell.taken = true;
      if (g[4]) cell.lit = true;
      if (g[5]) cell.hit = true;
    });
    snap.dead.forEach(function (id) { var e = self.enemies[id]; e.dead = true; e.removed = true; });
  };

  // ---------------------------------------------------------------- drawing

  W.roomOf = function (x, y) {
    var rx = Math.max(0, Math.min(this.roomsWide - 1, Math.floor(x / SAM.ROOM_W)));
    var ry = Math.max(0, Math.min(this.rows / 3 - 1, Math.floor(y / SAM.ROOM_H)));
    return { rx: rx, ry: ry, key: ry * this.roomsWide + rx };
  };

  var STATIC = /^(|pillar|torch|spikes|slicer|rocks|potion|bigpotion|leaf|fire|start|exit|mace|guard|boss|dragon|hint|target|gate)$/;

  W.drawStatic = function (ctx, room) {
    var ox = room.rx * 10, oy = room.ry * 3;
    A.roomBackground(ctx, this.theme, room.key + this.index * 31, SAM.ROOM_W, SAM.VIEW_H, this.index);
    for (var r = oy; r < oy + 3; r++) {
      for (var c = ox; c < ox + 10; c++) {
        var cell = this.cells[r][c], x = (c - ox) * TW, y = (r - oy) * TH;
        if (cell.type === 'block') A.block(ctx, x, y, TW, TH + (r === oy + 2 ? 12 : 0), this.theme, r, c);
      }
    }
    for (r = oy; r < oy + 3; r++) {
      for (c = ox; c < ox + 10; c++) {
        cell = this.cells[r][c]; x = (c - ox) * TW; y = (r - oy) * TH;
        if (cell.type !== 'floor' || cell.f === 'loose' || cell.f === 'plate' || !STATIC.test(cell.f)) continue;
        if (cell.f === 'pillar') A.pillar(ctx, x + TW / 2, y + TH, this.theme);
        if (cell.f === 'exit') A.door(ctx, x + TW / 2, y + TH, true);
        if (cell.f === 'start') A.door(ctx, x + TW / 2, y + TH, false);
        A.floorSlab(ctx, x, y + TH, TW, this.theme, false);
        if (cell.f === 'spikes') A.spikes(ctx, x, y + TH, TW, 0.5);
      }
    }
  };

  W.drawRoom = function (ctx, room, k) {
    var key = room.key;
    var cache = this.cache[key];
    if (!cache || cache.k !== k) {
      var cv = document.createElement('canvas');
      cv.width = Math.ceil(SAM.ROOM_W * k); cv.height = Math.ceil(SAM.VIEW_H * k);
      var cx = cv.getContext('2d');
      cx.setTransform(k, 0, 0, k, 0, 0);
      this.drawStatic(cx, room);
      cache = this.cache[key] = { cv: cv, k: k };
    }
    ctx.drawImage(cache.cv, 0, 0, SAM.ROOM_W, SAM.VIEW_H);
  };

  W.drawDynamic = function (ctx, room, player) {
    var t = this.time, ox = room.rx * 10, oy = room.ry * 3;
    for (var r = oy; r < oy + 3; r++) {
      for (var c = ox; c < ox + 10; c++) {
        var cell = this.cells[r][c], x = (c - ox) * TW, y = (r - oy) * TH, cx = x + TW / 2, fy = y + TH;
        switch (cell.f) {
          case 'torch': A.torch(ctx, cx, y + 70, t); break;
          case 'loose':
            var sh = cell.shake >= 0 ? Math.sin(t * 80) * 1.5 : 0;
            A.floorSlab(ctx, x, fy + sh, TW, this.theme, true);
            break;
          case 'plate': A.floorSlab(ctx, x, fy, TW, this.theme); A.plate(ctx, x, fy, TW, cell.pressed > 0 || this.opened[cell.letter] ? 1 : 0, this.theme); break;
          case 'gate': A.gate(ctx, cx, y + 4, fy - 4, cell.open); break;
          case 'slicer': A.slicer(ctx, cx, fy, this.slicerClosed(cell)); break;
          case 'potion': case 'bigpotion': if (!cell.taken) A.potion(ctx, cx + 10, fy - 2, cell.f === 'bigpotion', t + c); break;
          case 'leaf': if (!cell.taken) A.leafScroll(ctx, cx, fy, t + c); break;
          case 'mace': if (!cell.taken) A.maceOnGround(ctx, cx, fy, t); break;
          case 'fire': A.altar(ctx, cx, fy, cell.lit, t); break;
          case 'target': A.target(ctx, cx, fy, cell.hit); break;
          case 'water': A.water(ctx, x, fy - 24, TW, 24, t); break;
          case 'bridge':
            A.water(ctx, x, fy - 24, TW, 24, t);
            A.bridge(ctx, x, fy, TW, cell.amount);
            break;
          case 'fallen': break;
        }
        if (cell.rubble) A.rubble(ctx, x, fy, this.theme);
      }
    }
  };

  W.drawEntities = function (ctx, room) {
    ctx.save();
    ctx.translate(-room.rx * SAM.ROOM_W, -room.ry * SAM.ROOM_H);
    for (var i = 0; i < this.entities.length; i++) this.entities[i].draw(ctx, this.time);
    ctx.restore();
  };

  // ---------------------------------------------------------------- moving things

  function overlapsPlayer(p, x, y, rad) {
    if (!p || p.dead) return false;
    var b = p.box();
    return x + rad > b.x0 && x - rad < b.x1 && y + rad > b.y0 && y - rad < b.y1;
  }

  function Rock(x, y, floorY) { this.x = x; this.y = y; this.vy = 0; this.floorY = floorY; this.rot = 0; }
  Rock.prototype.update = function (dt, w, p) {
    this.vy += 900 * dt; this.y += this.vy * dt; this.rot += dt * 6;
    if (overlapsPlayer(p, this.x, this.y, 9)) { p.damage(1, this.x < p.x ? 1 : -1); w.particles(this.x, this.y, '#8A7A70', 8); return false; }
    if (this.y >= this.floorY - 8) {
      w.particles(this.x, this.floorY - 6, '#8A7A70', 10);
      if (p && w.roomOf(this.x, this.floorY - 40).key === w.roomOf(p.x, p.y - 40).key) w.emit('sound', 'crumble');
      return false;
    }
  };
  Rock.prototype.draw = function (ctx) { A.rock(ctx, this.x, this.y, this.rot); };

  function Debris(x, y) { this.x = x; this.y = y; this.vy = 0; this.top = y; }
  Debris.prototype.update = function (dt, w, p) {
    var prev = this.y;
    this.vy += 1400 * dt; this.y += this.vy * dt;
    if (overlapsPlayer(p, this.x, this.y - 6, 10) && this.y - this.top > 30 && p.y > prev + 10) { p.damage(1, 0); }
    var c = w.colOf(this.x);
    var r = w.rowOfFeet(prev);
    if (r >= w.rows) return false;
    var cell = w.cell(r, c);
    if (cell && cell.f === 'water' && this.y > (r + 1) * SAM.TH - 10) { w.particles(this.x, (r + 1) * SAM.TH - 24, '#9fd0e8', 10); return false; }
    var L = (r + 1) * SAM.TH;
    if (prev <= L && this.y >= L && w.standable(r, c) && L > this.top + 1) {
      this.y = L;
      w.particles(this.x, L - 4, A.THEMES[w.theme].floor, 12);
      w.emit('sound', 'thud');
      if (cell && cell.f === 'plate') w.press(cell);
      else if (cell && cell.f !== 'gate') cell.rubble = true;
      return false;
    }
  };
  Debris.prototype.draw = function (ctx) { A.floorSlab(ctx, this.x - SAM.TW / 2, this.y, SAM.TW, SAM.currentTheme, true); };

  function Particle(x, y, color, spread) {
    this.x = x; this.y = y; this.color = color;
    this.vx = (Math.random() - 0.5) * spread; this.vy = -Math.random() * spread;
    this.life = 0.5 + Math.random() * 0.4; this.size = 1.5 + Math.random() * 2.5;
  }
  Particle.prototype.update = function (dt) {
    this.vy += 600 * dt; this.x += this.vx * dt; this.y += this.vy * dt; this.life -= dt;
    return this.life > 0;
  };
  Particle.prototype.draw = function (ctx) {
    ctx.globalAlpha = Math.min(1, this.life * 2);
    ctx.fillStyle = this.color; ctx.fillRect(this.x - this.size / 2, this.y - this.size / 2, this.size, this.size);
    ctx.globalAlpha = 1;
  };

  function Arrow(x, y, face) { this.x = x; this.y = y; this.vx = face * 620; this.vy = -20; this.life = 1.4; this.face = face; }
  Arrow.prototype.update = function (dt, w) {
    var steps = 3;
    for (var s = 0; s < steps; s++) {
      this.vy += 220 * dt / steps;
      this.x += this.vx * dt / steps; this.y += this.vy * dt / steps;
      var r = Math.floor(this.y / SAM.TH), c = w.colOf(this.x);
      if (w.solid(r, c) || w.gateBlocks(r, c)) { w.particles(this.x, this.y, SAM.PAL.STEEL, 4, 60); return false; }
      var cell = w.cell(r, c);
      if (cell && cell.f === 'target' && !cell.hit && Math.abs(this.x - (c * SAM.TW + SAM.TW / 2)) < 12 && this.y > r * SAM.TH + 40 && this.y < r * SAM.TH + 90) {
        w.hitTarget(cell); return false;
      }
      for (var i = 0; i < w.enemies.length; i++) {
        var e = w.enemies[i];
        if (!e.dead && e.arrowHit && e.arrowHit(this, w)) return false;
      }
    }
    this.life -= dt;
    return this.life > 0;
  };
  Arrow.prototype.draw = function (ctx) { A.arrow(ctx, this.x, this.y, Math.atan2(this.vy, this.vx)); };

  function Fireball(x, y, vx) { this.x = x; this.y = y; this.vx = vx; this.life = 3.2; }
  Fireball.prototype.update = function (dt, w, p) {
    this.x += this.vx * dt; this.life -= dt;
    if (overlapsPlayer(p, this.x, this.y - 12, 10)) { p.damage(1, this.vx > 0 ? 1 : -1); w.particles(this.x, this.y - 10, SAM.PAL.FIRE_MID, 10); return false; }
    if (w.solid(Math.floor((this.y - 5) / SAM.TH), w.colOf(this.x))) return false;
    if (Math.random() < 0.3) w.entities.push(new Particle(this.x, this.y - 10, SAM.PAL.FIRE_MID, 60));
    return this.life > 0;
  };
  Fireball.prototype.draw = function (ctx, t) { A.fireball(ctx, this.x, this.y, t); };

  function FloatText(x, y, text, color) { this.x = x; this.y = y; this.text = text; this.color = color || SAM.PAL.GOLD_LIGHT; this.life = 1.2; }
  FloatText.prototype.update = function (dt) { this.y -= 30 * dt; this.life -= dt; return this.life > 0; };
  FloatText.prototype.draw = function (ctx) {
    ctx.globalAlpha = Math.min(1, this.life * 2);
    ctx.font = 'bold 15px Vazirmatn, Tahoma, sans-serif'; ctx.textAlign = 'center';
    ctx.lineWidth = 3; ctx.strokeStyle = SAM.PAL.INK; ctx.strokeText(this.text, this.x, this.y);
    ctx.fillStyle = this.color; ctx.fillText(this.text, this.x, this.y);
    ctx.globalAlpha = 1;
  };

  World.Arrow = Arrow;
  World.Fireball = Fireball;
  World.Particle = Particle;
  World.FloatText = FloatText;
  return World;
})();
