// Foes. Guards fight like the guards of Prince of Persia: they close in, wind up, strike,
// and can block. Blocking their strike at the right moment staggers them.
var SAM = window.SAM || (window.SAM = {});

SAM.Guard = (function () {
  var A = SAM.Art;
  var KINDS = {
    bandit: { hp: 3, skill: 0.3, look: 'bandit', weapon: 'sword', wind: 0.45, cd: [1.0, 1.9], speed: 75 },
    div: { hp: 4, skill: 0.45, look: 'div', weapon: 'club', wind: 0.5, cd: [1.0, 1.8], speed: 65 },
    karkoy: { hp: 8, skill: 0.75, look: 'karkoy', weapon: 'sword', wind: 0.38, cd: [0.8, 1.4], speed: 85, boss: true },
  };

  function Guard(world, x, y, kind) {
    var k = KINDS[kind] || KINDS.bandit;
    this.w = world; this.x = x; this.y = y; this.k = k; this.kind = kind;
    this.isGuard = true; this.boss = !!k.boss;
    this.hp = this.maxHp = k.hp;
    this.face = -1; this.state = 'idle'; this.t = 0; this.cd = 1.2;
    this.alert = false; this.hw = 11; this.flash = 0; this.anim = 0; this.vx = 0;
  }

  var G = Guard.prototype;

  G.set = function (s) { this.state = s; this.t = 0; };

  G.phase2 = function () { return this.boss && this.hp <= this.maxHp / 2; };

  G.clear = function (p) {
    var w = this.w, r = w.rowOfFeet(this.y);
    var a = w.colOf(Math.min(p.x, this.x)), b = w.colOf(Math.max(p.x, this.x));
    for (var c = a + 1; c < b; c++) if (w.solid(r, c) || w.gateBlocks(r, c)) return false;
    return true;
  };

  // guards stay on their floor and never walk into traps
  G.safeStep = function (dx) {
    var w = this.w, r = w.rowOfFeet(this.y), nx = this.x + dx;
    var c = w.colOf(nx + Math.sign(dx) * 14);
    var cell = w.cell(r, c);
    if (!w.standable(r, c) || w.gateBlocks(r, c)) return false;
    if (cell && (cell.f === 'spikes' || cell.f === 'slicer' || cell.f === 'loose')) return false;
    w.moveX(this, dx);
    return true;
  };

  G.update = function (dt, p) {
    this.t += dt;
    this.flash = Math.max(0, this.flash - dt);
    if (this.dead) {
      if (this.boss && this.t > 1.4 && !this.reported) { this.reported = true; this.w.emit('complete'); }
      return;
    }
    var k = this.k, dist = p.x - this.x, ad = Math.abs(dist);
    var same = !p.dead && Math.abs(p.y - this.y) < 12;
    if (!this.alert && same && ad < (this.boss ? 380 : 300) && this.clear(p)) {
      this.alert = true;
      if (this.boss) this.w.emit('boss', this);
    }
    var wind = this.phase2() ? 0.27 : k.wind;
    this.vx = 0;
    switch (this.state) {
      case 'idle':
        if (!this.alert || !same || p.dead) break;
        this.face = dist > 0 ? 1 : -1;
        var want = 60;
        if (ad > want + 8) { if (this.safeStep(this.face * k.speed * dt)) this.vx = this.face * k.speed; }
        else if (ad < want - 18) { if (this.safeStep(-this.face * 55 * dt)) this.vx = -this.face * 55; }
        this.anim += Math.abs(this.vx) * dt * 0.06;
        this.cd -= dt * (p.state === 'hang' || p.state === 'air' ? 0.3 : 1);
        if (this.cd <= 0 && ad < want + 26) this.set('wind');
        break;
      case 'wind':
        if (this.t >= wind) {
          this.set('strike');
          this.w.emit('sound', 'swing');
          if (same && ad < 80 && p.onGround()) {
            if (p.parryT > 0 && p.face === -this.face) {
              this.w.emit('sound', 'clang');
              this.w.particles(this.x + this.face * 34, this.y - 66, SAM.PAL.GOLD_LIGHT, 10, 160);
              this.set('recoil');
            } else {
              p.damage(1, this.face);
            }
          }
        }
        break;
      case 'strike':
        if (this.t >= 0.3) this.rest();
        break;
      case 'recoil':
        this.safeStep(-this.face * 40 * dt);
        if (this.t >= 0.65) this.rest();
        break;
      case 'parry':
        if (this.t >= 0.3) this.rest();
        break;
      case 'hurt':
        if (this.t >= 0.3) this.rest();
        break;
    }
  };

  G.rest = function () {
    var cd = this.k.cd;
    this.cd = (cd[0] + Math.random() * (cd[1] - cd[0])) * (this.phase2() ? 0.6 : 1);
    if (this.phase2() && Math.random() < 0.35) this.cd = 0.15; // Karkoy's double strike
    this.set('idle');
  };

  G.hurt = function (dir) {
    this.hp--;
    this.flash = 0.15;
    this.w.emit('sound', 'hit');
    this.w.particles(this.x, this.y - 55, SAM.PAL.WHITE, 8);
    this.safeStep(dir * 14);
    if (this.hp <= 0) {
      this.dead = true; this.set('dead');
      this.w.emit('sound', 'thud');
      this.w.emit('enemyDown', this);
    } else this.set('hurt');
  };

  G.takeStrike = function (p) {
    var d = (this.x - p.x) * p.face;
    if (d < -8 || d > 82 || Math.abs(this.y - p.y) > 30) return;
    this.alert = true;
    var canBlock = this.state === 'idle' && p.face === -this.face && Math.random() < this.k.skill * 0.6;
    if (canBlock) {
      this.set('parry');
      this.w.emit('sound', 'clang');
      this.w.particles(this.x - this.face * 20, this.y - 66, SAM.PAL.GOLD_LIGHT, 8, 140);
      p.recoil();
      return;
    }
    this.hurt(p.face);
  };

  G.arrowHit = function (a) {
    if (Math.abs(a.x - this.x) > 12 || a.y < this.y - 92 || a.y > this.y) return false;
    this.alert = true;
    if (Math.sign(a.vx) === -this.face && this.state === 'idle' && Math.random() < 0.55 + this.k.skill * 0.4) {
      this.set('parry');
      this.w.emit('sound', 'clang');
      this.w.particles(a.x, a.y, SAM.PAL.GOLD_LIGHT, 6, 120);
      return true;
    }
    this.face = a.vx > 0 ? -1 : 1;
    this.hurt(Math.sign(a.vx));
    return true;
  };

  G.draw = function (ctx, ox, oy) {
    if (this.removed) return;
    var POS = A.POSES, p, t = this.t;
    switch (this.state) {
      case 'idle': p = !this.alert ? POS.stand : Math.abs(this.vx) > 1 ? A.lerpPose(POS.fight, A.run(this.anim, 0.35), 0.4) : POS.fight; break;
      case 'wind': p = A.lerpPose(POS.fight, POS.strikeUp, Math.min(1, t / 0.2)); break;
      case 'strike': p = A.lerpPose(POS.strikeUp, POS.strikeDown, Math.min(1, t / 0.07)); break;
      case 'recoil': p = POS.hurt; break;
      case 'parry': p = POS.parry; break;
      case 'hurt': p = POS.hurt; break;
      case 'dead': p = POS.dead; break;
    }
    var x = this.x - ox, y = this.y - oy;
    // the telegraph: a glint on the weapon before the strike lands
    if (this.state === 'wind' && t > 0.1) {
      ctx.save(); ctx.globalAlpha = 0.5; A.circle(ctx, x + this.face * 6, y - 112, 6 + t * 10, SAM.PAL.FIRE_CORE, false); ctx.restore();
    }
    A.figure(ctx, A.LOOKS[this.k.look], p, x, y, this.face, 1, { weapon: this.k.weapon, flash: this.flash > 0 });
    if (this.alert && !this.dead && !this.boss) {
      for (var i = 0; i < this.maxHp; i++) {
        var hx = x - (this.maxHp - 1) * 5 + i * 10, hy = y - 118;
        A.poly(ctx, [hx - 4, hy, hx + 4, hy, hx, hy + 7], i < this.hp ? SAM.PAL.VERMILION : 'rgba(0,0,0,0.3)', SAM.PAL.INK, 1);
      }
    }
  };

  return Guard;
})();

SAM.Dragon = (function () {
  var A = SAM.Art;

  function Dragon(world, x, y) {
    this.w = world; this.floorY = y; this.x = x; this.y = y;
    this.ax = x + 96; this.ay = y - 24;
    this.boss = true; this.isDragon = true;
    this.hp = this.maxHp = 5;
    this.state = 'sleep'; this.t = 0; this.tt = 0;
    this.hx = this.ax + 10; this.hy = this.ay + 60;
    this.mouth = 0; this.flash = 0; this.sink = 0; this.hits = 0; this.sinceRoar = 0;
    this.alert = false;
  }

  var D = Dragon.prototype;

  D.set = function (s) { this.state = s; this.t = 0; };

  D.update = function (dt, p) {
    this.t += dt; this.tt += dt;
    this.flash = Math.max(0, this.flash - dt);
    var fy = this.floorY, tx = this.ax - 60 + Math.sin(this.tt * 0.9) * 16, ty = fy - 200 + Math.cos(this.tt * 1.3) * 10, mouth = 0.1, speed = 4;
    this.fire = false;
    if (this.dead) {
      this.sink += dt * 45;
      this.hy += dt * 30;
      if (this.t > 2.6 && !this.reported) { this.reported = true; this.w.emit('complete'); }
      return;
    }
    if (!this.alert) {
      if (p.x > this.ax - 470) { this.alert = true; this.set('rise'); this.w.emit('sound', 'roar'); this.w.emit('shake', 10); this.w.emit('boss', this); }
      else { this.hy = this.ay + 80; return; }
    }
    switch (this.state) {
      case 'rise':
        speed = 2;
        if (this.t > 1.6) this.set('sway');
        break;
      case 'sway':
        if (this.t > 1.2 + Math.random() * 0.02) {
          if (p.x > this.ax - 250 && !p.dead) this.set('bite');
          else if (this.sinceRoar >= 1 || Math.random() < 0.5) { this.sinceRoar = 0; this.set('roar'); this.w.emit('sound', 'roar'); }
          else { this.sinceRoar++; this.set('breath'); }
        }
        break;
      case 'roar':
        tx = this.ax - 140; ty = fy - 72; mouth = this.t > 0.35 ? 1 : this.t / 0.35;
        if (this.t > 1.9) this.set('sway');
        break;
      case 'breath':
        tx = this.ax - 150; ty = fy - 80; mouth = Math.min(1, this.t / 0.5); this.fire = this.t > 0.5;
        var shots = [0.6, 1.15, 1.7];
        for (var i = 0; i < shots.length; i++) {
          if (this.t >= shots[i] && this.t - dt < shots[i]) {
            this.w.entities.push(new SAM.World.Fireball(this.hx - 50, fy, -215));
            this.w.emit('sound', 'fire');
          }
        }
        if (this.t > 2.2) this.set('sway');
        break;
      case 'bite':
        if (this.t < 0.6) { tx = this.ax - 40; ty = fy - 230; mouth = 0.7; }
        else if (this.t < 0.9) {
          tx = Math.max(this.ax - 280, p.x + 40); ty = fy - 58; mouth = 0.9; speed = 14;
          if (!this.bit && Math.hypot(this.hx - 40 - p.x, this.hy - (p.y - 45)) < 44) { this.bit = true; p.damage(1, -1); }
        } else { this.bit = false; mouth = 0.2; }
        if (this.t > 1.4) this.set('sway');
        break;
      case 'stun':
        tx = this.ax - 150; ty = fy - 22; mouth = 0.25; speed = 6;
        if (this.t > 3.2 || this.hits >= 2) { this.hits = 0; this.set('sway'); }
        break;
    }
    var k = Math.min(1, dt * speed);
    this.hx += (tx - this.hx) * k; this.hy += (ty - this.hy) * k;
    this.mouth += (mouth - this.mouth) * Math.min(1, dt * 10);
  };

  D.arrowHit = function (a) {
    if (!this.alert || this.dead) return false;
    var mx = this.hx - 26, my = this.hy + 2;
    if (this.state === 'roar' && this.t > 0.3 && Math.hypot(a.x - mx, a.y - my) < 26) {
      this.set('stun');
      this.hits = 0;
      this.flash = 0.2;
      this.w.emit('sound', 'roar');
      this.w.emit('toast', 'dragonHurt');
      this.w.emit('shake', 8);
      this.w.particles(a.x, a.y, SAM.PAL.CRIMSON, 12);
      return true;
    }
    if (Math.hypot(a.x - (this.hx - 10), a.y - (this.hy - 6)) < 40) {
      this.w.emit('sound', 'clang');
      this.w.particles(a.x, a.y, SAM.PAL.STEEL, 6, 100);
      return true;
    }
    return false;
  };

  D.takeStrike = function (p) {
    if (!this.alert || this.dead) return;
    var reach = p.x + p.face * 50;
    if (Math.abs(reach - (this.hx - 20)) > 60 || Math.abs(this.hy - (p.y - 30)) > 70) return;
    if (this.state !== 'stun') { this.w.emit('sound', 'clang'); return; }
    this.hp--; this.hits++; this.flash = 0.2;
    this.w.emit('sound', 'hit'); this.w.emit('shake', 6);
    this.w.particles(this.hx - 20, this.hy - 10, '#37896A', 14);
    if (this.hp <= 0) { this.dead = true; this.set('dead'); this.w.emit('sound', 'roar'); this.w.emit('shake', 14); }
  };

  D.draw = function (ctx, ox, oy) {
    A.dragon(ctx, { ax: this.ax - ox, ay: this.ay - oy, hx: this.hx - ox, hy: this.hy - oy, mouth: this.mouth, t: this.tt,
      stun: this.state === 'stun' || this.dead, flash: this.flash > 0, fire: this.fire, sink: this.sink });
  };

  return Dragon;
})();
