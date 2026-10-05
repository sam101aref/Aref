// Sam. Movement follows Prince of Persia: run, careful steps, jumps that clear one or two cells,
// grabbing ledges, hanging, climbing, and falls that hurt or kill. Combat adds the mace, blocking and the bow.
var SAM = window.SAM || (window.SAM = {});

SAM.Player = (function () {
  var TW = SAM.TW, TH = SAM.TH, A = SAM.Art;
  var G = 1600, MAX_FALL = 950;
  var RUN = 190, WALK = 70, FIGHT = 80;

  function Player(world, x, y, keep) {
    this.w = world;
    this.x = x; this.y = y; this.vx = 0; this.vy = 0; this.face = 1;
    this.h = 84; this.hw = 10;
    this.state = 'stand'; this.t = 0; this.anim = 0;
    this.maxHp = keep ? keep.maxHp : 3;
    this.hp = this.maxHp;
    this.hasMace = keep ? keep.hasMace : !world.def.needMace;
    this.hasBow = !!world.def.bow;
    this.feather = keep && keep.feather !== undefined ? keep.feather : !!world.def.feather;
    this.cool = 0; this.inv = 0; this.parryT = 0;
    this.minY = y; this.airKind = null; this.ledge = null;
    this.safe = { x: x, y: y };
    this.dead = false; this.reason = '';
    this.hitDone = false; this.foe = null;
  }

  var P = Player.prototype;

  P.saveState = function () { return { maxHp: this.maxHp, hasMace: this.hasMace, feather: this.feather }; };

  P.box = function () {
    var h = this.state === 'crouch' ? 50 : this.h;
    return { x0: this.x - this.hw, x1: this.x + this.hw, y0: this.y - h, y1: this.y };
  };

  P.set = function (s) { this.state = s; this.t = 0; this.hitDone = false; this.runup = false; this.h = 84; };

  P.onGround = function () {
    return /^(stand|crouch|strike|parry|shoot|hurt|land|drink|pickup|exit)$/.test(this.state);
  };

  P.engagedFoe = function () {
    if (!this.hasMace) return null;
    var best = null;
    for (var i = 0; i < this.w.enemies.length; i++) {
      var e = this.w.enemies[i];
      if (e.dead || !e.isGuard || !e.alert) continue;
      var d = Math.abs(e.x - this.x);
      if (Math.abs(e.y - this.y) < 10 && d < 170 && (!best || d < Math.abs(best.x - this.x))) best = e;
    }
    return best;
  };

  // ---------------------------------------------------------------- update

  P.update = function (dt, inp) {
    this.t += dt;
    this.cool = Math.max(0, this.cool - dt);
    this.inv = Math.max(0, this.inv - dt);
    if (this.dead) { this.vx *= 0.9; return; }
    this.foe = this.onGround() ? this.engagedFoe() : null;
    if (this.foe && this.state === 'stand') this.face = this.foe.x > this.x ? 1 : -1;

    switch (this.state) {
      case 'stand': this.ground(dt, inp); break;
      case 'crouch':
        if (!this.supported()) return this.fall();
        if (!inp.down) this.set('stand');
        break;
      case 'air': this.air(dt, inp); break;
      case 'hang': this.hang(dt, inp); break;
      case 'climb':
        if (this.t >= 0.5) { this.x = this.ledge.x + this.face * 16; this.y = this.ledge.y; this.vx = 0; this.set('stand'); }
        break;
      case 'climbdown':
        if (this.t >= 0.36) { this.x = this.ledge.x - this.face * 10; this.y = this.ledge.y + SAM.HANG_DY; this.set('hang'); }
        break;
      case 'strike':
        this.vx *= 0.85;
        if (this.t >= 0.14 && !this.hitDone) { this.hitDone = true; this.strike(); }
        if (this.t >= 0.36) this.set('stand');
        this.groundPhysics(dt);
        break;
      case 'parry':
        this.vx *= 0.8;
        this.parryT = this.t < 0.26 ? 1 : 0;
        if (this.t >= 0.34) { this.parryT = 0; this.set('stand'); }
        this.groundPhysics(dt);
        break;
      case 'shoot':
        this.vx *= 0.8;
        if (this.t >= 0.22 && !this.hitDone) {
          this.hitDone = true;
          this.w.entities.push(new SAM.World.Arrow(this.x + this.face * 22, this.y - 63, this.face));
          this.w.emit('sound', 'bow');
        }
        if (this.t >= 0.42) this.set('stand');
        this.groundPhysics(dt);
        break;
      case 'hurt':
        this.vx *= 0.88;
        if (this.t >= 0.32) this.set('stand');
        this.groundPhysics(dt);
        break;
      case 'land':
        this.vx *= 0.7;
        if (this.t >= this.landT) this.set('stand');
        this.groundPhysics(dt);
        break;
      case 'drink':
        if (this.t >= 0.6) this.set('stand');
        break;
      case 'pickup':
        if (this.t >= 1.1) this.set('stand');
        break;
      case 'exit':
        if (this.t >= 0.9 && !this.hitDone) { this.hitDone = true; this.w.emit('complete'); }
        break;
    }
    this.hazards();
  };

  P.supported = function () {
    return this.w.standable(this.w.rowOfFeet(this.y), this.w.colOf(this.x));
  };

  P.fall = function () {
    this.set('air'); this.airKind = 'fall'; this.vy = 0; this.minY = this.y;
  };

  P.groundPhysics = function (dt) {
    if (!this.supported()) return this.fall();
    if (this.vx) this.w.moveX(this, this.vx * dt);
  };

  P.ground = function (dt, inp) {
    var w = this.w;
    if (!this.supported()) return this.fall();
    var r = w.rowOfFeet(this.y), c = w.colOf(this.x);
    var cell = w.cell(r, c);
    var dir = (inp.right ? 1 : 0) - (inp.left ? 1 : 0);
    var pr = inp.pressed;
    var fight = !!this.foe;

    if (pr.parry && this.hasMace) return this.set('parry');
    if (pr.attack && this.hasMace) { this.w.emit('sound', 'swing'); return this.set('strike'); }
    if (pr.shoot && this.hasBow && this.cool <= 0 && !fight) { this.cool = 0.55; return this.set('shoot'); }
    if (pr.up) {
      if (cell && cell.f === 'exit') {
        if (w.def.needMace && !this.hasMace) w.emit('toast', 'needMace');
        else { this.vx = 0; return this.set('exit'); }
      } else if (fight) return this.set('parry');
      else return this.jumpUp();
    }
    if (this.runup) {
      // a running jump pressed early carries Sam on to the edge of the gap, then he leaps
      var toEdge = ((this.face > 0 ? (c + 1) * TW : c * TW) - this.x) * this.face;
      if (toEdge <= 12 || w.standable(r, c + this.face)) { this.runup = false; return this.jumpFwd(true); }
      this.vx = this.face * RUN;
      if (w.moveX(this, this.vx * dt)) { this.runup = false; this.vx = 0; }
      this.anim += RUN * dt * 0.058;
      return;
    }
    if (pr.jump && !fight) {
      if (Math.abs(this.vx) > 150) {
        var edgeDist = ((this.face > 0 ? (c + 1) * TW : c * TW) - this.x) * this.face;
        if (!w.standable(r, c + this.face) && edgeDist > 12) { this.runup = true; return; }
      }
      if (dir || Math.abs(this.vx) > 60) {
        if (dir) this.face = dir;
        return this.jumpFwd(Math.abs(this.vx) > 150);
      }
      return this.jumpUp();
    }
    if (pr.down && !fight) {
      if (this.climbDown()) return;
      this.vx = 0;
      return this.set('crouch');
    }

    // walking and running
    var careful = inp.walk || fight;
    var speed = fight ? FIGHT : inp.walk ? WALK : RUN;
    if (dir) {
      if (!fight && dir !== this.face) {
        if (Math.abs(this.vx) > 90) this.vx -= this.face * 1500 * dt; // skid before turning
        else { this.face = dir; this.vx = 0; }
      } else {
        var target = dir * speed;
        var acc = careful ? 900 : 1000;
        if (Math.abs(this.vx) > speed) this.vx = target;
        else this.vx += Math.sign(target - this.vx) * Math.min(Math.abs(target - this.vx), acc * dt);
      }
    } else {
      this.vx -= Math.sign(this.vx) * Math.min(Math.abs(this.vx), 1500 * dt);
    }
    if (careful && this.vx) {
      // a careful step stops at the edge instead of stepping off
      var d = Math.sign(this.vx), nx = this.x + this.vx * dt;
      if (!w.standable(r, w.colOf(nx + d * 3))) {
        var edge = d > 0 ? (c + 1) * TW : c * TW;
        this.x = d > 0 ? Math.min(nx, edge - 4) : Math.max(nx, edge + 4);
        this.vx = 0;
      }
    }
    if (this.vx) {
      if (w.moveX(this, this.vx * dt)) this.vx = 0;
      this.anim += Math.abs(this.vx) * dt * 0.058;
      if (Math.abs(this.vx) > 120 && Math.floor(this.anim / Math.PI) !== Math.floor((this.anim - Math.abs(this.vx) * dt * 0.058) / Math.PI)) w.emit('sound', 'step');
    }
  };

  P.jumpUp = function () {
    this.set('air'); this.airKind = 'up';
    this.vy = -430; this.vx = 0; this.minY = this.y;
    // line up under a ledge in front, like the Prince does
    var L = this.w.findLedge(this.x, this.y - TH - 0, this.face, 40, 30, 30);
    this.alignX = L ? L.x - this.face * 10 : null;
    this.w.emit('sound', 'jump');
  };

  P.jumpFwd = function (running) {
    this.set('air'); this.airKind = 'fwd';
    this.vx = this.face * (running ? 300 : 195);
    this.vy = running ? -415 : -360;
    this.h = 64; // legs tucked: a long jump fits under a low ceiling
    this.minY = this.y;
    this.w.emit('sound', 'jump');
  };

  P.climbDown = function () {
    var w = this.w, r = w.rowOfFeet(this.y), c = w.colOf(this.x);
    var order = [this.face, -this.face];
    for (var i = 0; i < 2; i++) {
      var d = order[i];
      var edge = d > 0 ? (c + 1) * TW : c * TW;
      if (Math.abs(this.x - edge) > 30) continue;
      if (!w.open(r, c + d) || w.solid(r + 1, c + d)) continue;
      this.face = -d;
      this.ledge = { r: r, c: c, x: edge, y: (r + 1) * TH };
      this.vx = 0;
      this.set('climbdown');
      return true;
    }
    return false;
  };

  P.air = function (dt, inp) {
    var w = this.w;
    if (this.airKind === 'up' && this.alignX !== null) {
      this.x += (this.alignX - this.x) * Math.min(1, dt * 12);
    }
    this.vy = Math.min(MAX_FALL, this.vy + G * dt);
    if (this.vx && w.moveX(this, this.vx * dt)) this.vx = -this.vx * 0.15;
    var prev = this.y;
    this.y += this.vy * dt;
    this.minY = Math.min(this.minY, this.y);
    var c = w.colOf(this.x);

    if (this.vy < 0) {
      // bump the head on a floor or block above
      var hp = prev - this.h, hn = this.y - this.h;
      var L = Math.floor(hp / TH) * TH;
      if (hn < L && (w.solid(L / TH - 1, c) || w.hasFloor(L / TH - 1, c))) { this.y = L + this.h; this.vy = 0; }
      if (this.y - this.h < 0) { this.y = this.h; this.vy = 0; }
    }

    // catch a ledge
    if (this.airKind !== 'drop' && (this.airKind === 'up' ? this.vy > -140 : this.vy > 0)) {
      var hand = this.y - (this.airKind === 'up' ? 104 : 96);
      var led = w.findLedge(this.x, hand, this.face, this.airKind === 'up' ? 30 : 24, 8, 40);
      if (led) return this.grab(led);
    }

    if (this.vy > 0) {
      var Lf = (w.rowOfFeet(prev) + 1) * TH;
      if (prev <= Lf && this.y >= Lf) {
        var rr = Lf / TH - 1;
        if (w.standable(rr, c)) return this.land(Lf, rr, c);
        // a forgiving toe-hold: landing just short of a floor still counts
        var d = Math.sign(this.vx), cc = w.colOf(this.x + d * 16);
        if (d && cc !== c && w.standable(rr, cc) && !w.solid(rr, cc)) {
          this.x = d > 0 ? cc * TW + 3 : (cc + 1) * TW - 3;
          return this.land(Lf, rr, cc);
        }
      }
      var wr = w.rowOfFeet(this.y - 30), cell = w.cell(wr, c);
      if (cell && cell.f === 'water' && this.y > (wr + 1) * TH - 6) { w.particles(this.x, (wr + 1) * TH - 24, '#9fd0e8', 16); return this.die('drown'); }
      if (this.y > w.rows * TH + 60) return this.die('fall');
    }
  };

  P.grab = function (led) {
    this.ledge = led;
    this.x = led.x - this.face * 10;
    this.y = led.y + SAM.HANG_DY;
    this.vx = 0; this.vy = 0;
    this.set('hang');
    this.w.emit('sound', 'land');
  };

  P.land = function (L, r, c) {
    var w = this.w;
    this.y = L; this.vy = 0;
    var dist = this.y - this.minY;
    w.stepOn(r, c);
    var cell = w.cell(r, c);
    if (cell && cell.f === 'spikes') return this.die('spikes');
    if (dist > 320) { w.emit('sound', 'thud'); return this.die('fall'); }
    if (dist > 190) {
      this.vx = 0; this.landT = 0.6; this.set('land');
      w.emit('sound', 'thud'); w.emit('shake', 6);
      this.damage(1, 0, true);
      return;
    }
    w.emit('sound', 'land');
    if (this.airKind === 'fwd' && Math.abs(this.vx) > 200) { this.set('stand'); return; }
    this.vx *= 0.3;
    this.landT = 0.12; this.set('land');
  };

  P.hang = function (dt, inp) {
    var w = this.w, L = this.ledge;
    if (!w.standable(L.r, L.c)) { this.airKind = 'drop'; this.set('air'); this.minY = this.y; return; }
    w.stepOn(L.r, L.c);
    if (inp.pressed.up || inp.pressed.jump) { this.set('climb'); this.w.emit('sound', 'jump'); return; }
    if (inp.pressed.down) {
      this.x -= this.face * 3;
      this.set('air'); this.airKind = 'drop'; this.vy = 0; this.minY = this.y;
    }
  };

  // ---------------------------------------------------------------- combat

  P.strike = function () {
    for (var i = 0; i < this.w.enemies.length; i++) {
      var e = this.w.enemies[i];
      if (!e.dead && e.takeStrike) e.takeStrike(this);
    }
  };

  P.recoil = function () { this.set('hurt'); this.t = 0.12; this.vx = -this.face * 60; };

  P.damage = function (n, dir, silent) {
    if (this.dead || (this.inv > 0 && !silent)) return;
    this.hp -= n;
    this.inv = 0.7;
    if (!silent) this.w.emit('sound', 'hurt');
    this.w.emit('shake', 4);
    this.w.emit('flash');
    if (this.hp <= 0) { this.hp = 0; return this.die('hp'); }
    if (this.state === 'hang' || this.state === 'climb') { this.set('air'); this.airKind = 'drop'; this.vy = 0; this.minY = this.y; return; }
    if (silent) return;
    if (this.onGround()) { this.set('hurt'); this.vx = (dir || -this.face) * 130; }
  };

  P.die = function (reason) {
    if (this.dead) return;
    this.dead = true; this.reason = reason; this.hp = 0;
    this.set('dead');
    this.vx = 0;
    this.w.emit('sound', reason === 'spikes' || reason === 'slicer' ? 'slice' : 'die');
    this.w.emit('shake', 8);
    this.w.emit('dead', reason);
  };

  P.revive = function (x, y) {
    this.dead = false; this.reason = '';
    this.x = x; this.y = y; this.vx = 0; this.vy = 0;
    this.hp = this.maxHp; this.inv = 1.5;
    this.set('stand');
  };

  // traps and pickups under Sam's feet
  P.hazards = function () {
    if (this.dead) return;
    var w = this.w, r = w.rowOfFeet(this.y), c = w.colOf(this.x), cell = w.cell(r, c);
    if (!cell) return;
    var grounded = this.onGround();
    if (cell.f === 'slicer' && w.slicerClosed(cell) > 0.75 && Math.abs(this.x - (c * TW + TW / 2)) < 16 && this.state !== 'hang') return this.die('slicer');
    if (!grounded) return;
    w.stepOn(r, c);
    if (cell.f === 'spikes' && Math.abs(this.vx) > 100) return this.die('spikes');
    if (cell.f === 'potion' && !cell.taken) {
      cell.taken = true; this.hp = Math.min(this.maxHp, this.hp + 1); this.vx = 0; this.set('drink');
      w.emit('sound', 'potion'); w.emit('toast', 'potion');
    } else if (cell.f === 'bigpotion' && !cell.taken) {
      cell.taken = true; this.maxHp = Math.min(10, this.maxHp + 1); this.hp = this.maxHp; this.vx = 0; this.set('drink');
      w.emit('sound', 'potion'); w.emit('toast', 'potionBig');
    } else if (cell.f === 'leaf' && !cell.taken) {
      cell.taken = true; w.leavesTaken++;
      w.particles(c * TW + TW / 2, this.y - 30, SAM.PAL.GOLD_LIGHT, 14);
      w.emit('sound', 'leaf'); w.emit('toast', 'leaf');
    } else if (cell.f === 'mace' && !cell.taken) {
      cell.taken = true; this.hasMace = true; this.vx = 0; this.set('pickup');
      w.particles(c * TW + TW / 2, this.y - 20, SAM.PAL.GOLD_LIGHT, 24);
      w.emit('sound', 'fanfare'); w.emit('toast', 'maceGot');
    } else if (cell.f === 'fire' && !cell.lit) {
      cell.lit = true;
      w.emit('sound', 'pickup'); w.emit('toast', 'checkpoint'); w.emit('checkpoint', { x: c * TW + TW / 2, y: (r + 1) * TH });
    }
    if (cell.f !== 'loose' && cell.f !== 'spikes' && cell.f !== 'slicer' && !this.foe) this.safe = { x: this.x, y: this.y };
  };

  // ---------------------------------------------------------------- drawing

  P.draw = function (ctx, ox, oy) {
    var look = A.LOOKS.sam, POS = A.POSES, x = this.x - ox, y = this.y - oy, face = this.face;
    var p, weapon = this.hasMace ? 'mace' : null, opts = {};
    var t = this.t;
    switch (this.state) {
      case 'stand':
        if (this.foe) p = POS.fight;
        else if (Math.abs(this.vx) > 5) p = A.run(this.anim, Math.min(1, Math.abs(this.vx) / RUN) * (Math.abs(this.vx) <= WALK + 5 ? 0.55 : 1));
        else p = this.hasMace ? POS.standMace : POS.stand;
        if (this.foe && Math.abs(this.vx) > 5) p = A.lerpPose(POS.fight, A.run(this.anim, 0.35), 0.4);
        break;
      case 'crouch': p = POS.crouch; break;
      case 'air':
        p = this.airKind === 'up' ? POS.jumpUp : this.airKind === 'fwd' && this.vy < 120 ? POS.jumpFwd : POS.fall;
        break;
      case 'hang': p = A.lerpPose(POS.hang, POS.hang, 0); p.hf = 0.08 + Math.sin(this.t * 2) * 0.05; break;
      case 'climb':
        var k = Math.min(1, t / 0.5), L = this.ledge;
        if (k < 0.55) {
          var u = k / 0.55;
          y = L.y - oy + SAM.HANG_DY - u * 70;
          x = L.x - ox - face * 10;
          p = A.lerpPose(POS.hang, A.pose({ shF: 0.5, elF: 2.4, shB: 0.4, elB: 2.4, hf: 0.9, kf: 1.6, hb: 0.2, kb: 0.6 }), u);
        } else {
          var v = (k - 0.55) / 0.45;
          y = L.y - oy;
          x = L.x - ox - face * 10 + face * 26 * v;
          p = A.lerpPose(POS.crouch, POS.stand, v);
        }
        break;
      case 'climbdown':
        var q = Math.min(1, t / 0.36), Ld = this.ledge;
        x = Ld.x - ox - face * (10 * q - 16 * (1 - q));
        y = Ld.y - oy + SAM.HANG_DY * q * q;
        p = A.lerpPose(POS.crouch, POS.hang, q);
        break;
      case 'strike':
        p = t < 0.12 ? A.lerpPose(POS.fight, POS.strikeUp, t / 0.12) : A.lerpPose(POS.strikeUp, POS.strikeDown, Math.min(1, (t - 0.12) / 0.07));
        break;
      case 'parry': p = POS.parry; break;
      case 'shoot': p = POS.shoot; weapon = 'bow'; break;
      case 'hurt': p = POS.hurt; break;
      case 'land': p = this.landT > 0.3 ? POS.crouch : A.lerpPose(POS.crouch, POS.stand, Math.min(1, t / this.landT)); break;
      case 'drink': p = POS.drink; weapon = null; break;
      case 'pickup': p = POS.raise; break;
      case 'exit': p = A.run(t * 6, 0.5); opts.alpha = Math.max(0, 1 - t / 0.9); break;
      case 'dead': p = POS.dead; break;
    }
    if (this.inv > 0 && !this.dead && Math.floor(this.inv * 14) % 2) opts.alpha = 0.45;
    opts.weapon = weapon;
    A.figure(ctx, look, p, x, y, face, 1, opts);
  };

  return Player;
})();
