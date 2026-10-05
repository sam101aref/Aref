// Music and sound effects synthesized with Web Audio, the same idea as Arash's code-built audio:
// a plucked tar line in dastgah-e Shur (with a koron note) over a daf rhythm.
var SAM = window.SAM || (window.SAM = {});

SAM.Audio = (function () {
  var ac = null, master = null, musicGain = null, sfxGain = null, noiseBuf = null;
  var enabled = true, mode = null, nextNote = 0, step = 0, timer = null;

  // Shur on D: D, E-koron (a quarter tone flat), F, G, A, Bb, C, D
  var SHUR = [0, 1.5, 3, 5, 7, 8, 10, 12];
  var BASE = 146.83; // D3

  function freq(deg, oct) {
    var n = SHUR[((deg % 7) + 7) % 7] + 12 * (Math.floor(deg / 7) + (oct || 0));
    return BASE * Math.pow(2, n / 12);
  }

  function init() {
    if (ac) return;
    var AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return;
    ac = new AC();
    master = ac.createGain(); master.gain.value = enabled ? 0.8 : 0; master.connect(ac.destination);
    musicGain = ac.createGain(); musicGain.gain.value = 0.32; musicGain.connect(master);
    sfxGain = ac.createGain(); sfxGain.gain.value = 0.7; sfxGain.connect(master);
    noiseBuf = ac.createBuffer(1, ac.sampleRate, ac.sampleRate);
    var d = noiseBuf.getChannelData(0);
    for (var i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
  }

  function unlock() {
    init();
    if (ac && ac.state === 'suspended') ac.resume();
  }

  function pluck(f, t, dur, vol, dest) {
    var o = ac.createOscillator(), o2 = ac.createOscillator(), g = ac.createGain(), lp = ac.createBiquadFilter();
    o.type = 'sawtooth'; o.frequency.value = f;
    o2.type = 'triangle'; o2.frequency.value = f * 2.003;
    lp.type = 'lowpass'; lp.frequency.setValueAtTime(f * 8, t); lp.frequency.exponentialRampToValueAtTime(f * 1.5, t + dur);
    g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.005); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(lp); o2.connect(lp); lp.connect(g); g.connect(dest || musicGain);
    o.start(t); o2.start(t); o.stop(t + dur + 0.05); o2.stop(t + dur + 0.05);
  }

  function noise(t, dur, vol, type, f, q, dest) {
    var s = ac.createBufferSource(), g = ac.createGain(), bp = ac.createBiquadFilter();
    s.buffer = noiseBuf;
    bp.type = type || 'bandpass'; bp.frequency.value = f || 1000; bp.Q.value = q || 1;
    g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    s.connect(bp); bp.connect(g); g.connect(dest || sfxGain);
    s.start(t, Math.random() * 0.5); s.stop(t + dur + 0.05);
  }

  function tone(type, f1, f2, t, dur, vol, dest) {
    var o = ac.createOscillator(), g = ac.createGain();
    o.type = type; o.frequency.setValueAtTime(f1, t); o.frequency.exponentialRampToValueAtTime(Math.max(20, f2), t + dur);
    g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
    o.connect(g); g.connect(dest || sfxGain);
    o.start(t); o.stop(t + dur + 0.05);
  }

  function daf(t, accent, dest) {
    tone('sine', accent ? 110 : 160, 50, t, 0.18, accent ? 0.5 : 0.25, dest || musicGain);
    noise(t, 0.08, accent ? 0.16 : 0.1, 'highpass', 3000, 0.7, dest || musicGain);
  }

  // a slow melody that wanders around the Shur degrees, resolving to the tonic
  var CALM = [0, 1, 2, 1, 0, -1, 0, null, 2, 3, 4, 3, 2, 1, 2, null, 4, 3, 2, 1, 2, 1, 0, null, -1, 0, 1, 0, -1, -3, 0, null];
  var BATTLE = [0, 0, 2, 1, 3, 2, 1, 0, 4, 4, 3, 2, 1, 2, 0, null];

  function schedule() {
    if (!ac || !mode) return;
    var beat = mode === 'battle' ? 0.17 : 0.34;
    while (nextNote < ac.currentTime + 0.25) {
      var seq = mode === 'battle' ? BATTLE : CALM;
      var deg = seq[step % seq.length];
      if (deg !== null) {
        pluck(freq(deg, 1), nextNote, mode === 'battle' ? 0.35 : 0.9, 0.22);
        if (step % 2 === 0) pluck(freq(deg, 1), nextNote + beat * 0.5, 0.25, 0.08); // tremolo of the tar
      }
      if (step % 8 === 0) pluck(freq(0, 0), nextNote, 2.2, 0.16); // drone
      if (mode === 'battle') daf(nextNote, step % 4 === 0);
      else if (step % 4 === 0) daf(nextNote, step % 8 === 0);
      nextNote += beat;
      step++;
    }
  }

  function music(m) {
    if (!ac || mode === m) return;
    mode = m;
    step = 0;
    nextNote = ac.currentTime + 0.1;
    if (!timer) timer = setInterval(schedule, 60);
  }

  var SFX = {
    jump: function (t) { noise(t, 0.12, 0.12, 'bandpass', 900, 1.2); },
    land: function (t) { tone('sine', 140, 60, t, 0.1, 0.35); },
    thud: function (t) { tone('sine', 120, 40, t, 0.25, 0.6); noise(t, 0.2, 0.25, 'lowpass', 400); },
    swing: function (t) { noise(t, 0.16, 0.22, 'bandpass', 1800, 2); },
    hit: function (t) { tone('square', 220, 70, t, 0.12, 0.25); noise(t, 0.1, 0.3, 'lowpass', 900); },
    clang: function (t) { tone('triangle', 1650, 1500, t, 0.35, 0.22); tone('triangle', 2470, 2300, t, 0.25, 0.12); noise(t, 0.05, 0.25, 'highpass', 4000); },
    hurt: function (t) { tone('sawtooth', 300, 120, t, 0.2, 0.18); },
    die: function (t) { tone('sawtooth', 220, 55, t, 1.0, 0.25); pluck(freq(-3), t + 0.2, 1.6, 0.25, sfxGain); },
    potion: function (t) { for (var i = 0; i < 3; i++) tone('sine', 500 + i * 120, 300, t + i * 0.09, 0.08, 0.2); },
    pickup: function (t) { pluck(freq(4, 1), t, 0.4, 0.3, sfxGain); pluck(freq(7, 1), t + 0.08, 0.6, 0.3, sfxGain); },
    leaf: function (t) { pluck(freq(2, 2), t, 0.5, 0.25, sfxGain); pluck(freq(4, 2), t + 0.07, 0.5, 0.2, sfxGain); pluck(freq(7, 2), t + 0.14, 0.8, 0.2, sfxGain); },
    gate: function (t) { for (var i = 0; i < 8; i++) noise(t + i * 0.09, 0.06, 0.12, 'bandpass', 600 + i * 30, 4); },
    crumble: function (t) { noise(t, 0.4, 0.3, 'lowpass', 600); },
    shake: function (t) { noise(t, 0.3, 0.08, 'bandpass', 300, 3); },
    slice: function (t) { noise(t, 0.12, 0.2, 'highpass', 2500); },
    bow: function (t) { tone('triangle', 180, 90, t, 0.15, 0.2); noise(t, 0.12, 0.12, 'bandpass', 2400, 2); },
    roar: function (t) { tone('sawtooth', 90, 45, t, 1.2, 0.35); noise(t, 1.0, 0.25, 'lowpass', 500); },
    fire: function (t) { noise(t, 0.6, 0.25, 'lowpass', 1200); },
    fanfare: function (t) { [0, 2, 4, 7].forEach(function (d, i) { pluck(freq(d, 1), t + i * 0.14, 0.9, 0.3, sfxGain); }); },
    feather: function (t) { [7, 4, 2, 4, 7, 9].forEach(function (d, i) { pluck(freq(d, 2), t + i * 0.07, 0.6, 0.2, sfxGain); }); },
    step: function (t) { noise(t, 0.04, 0.05, 'lowpass', 500); },
    select: function (t) { pluck(freq(4, 1), t, 0.25, 0.2, sfxGain); },
  };

  function play(name) {
    if (!ac || !enabled || !SFX[name]) return;
    SFX[name](ac.currentTime);
  }

  function setEnabled(on) {
    enabled = on;
    if (master) master.gain.setTargetAtTime(on ? 0.8 : 0, ac.currentTime, 0.05);
  }

  return { unlock: unlock, play: play, music: music, setEnabled: setEnabled, isEnabled: function () { return enabled; } };
})();
