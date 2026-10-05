// Persian / English strings. Every string is a [fa, en] pair, like the tables in Arash and Siavosh.
var SAM = window.SAM || (window.SAM = {});

SAM.lang = 'fa';
SAM.inputMode = 'keys'; // 'keys' or 'touch' — decides how hints name the controls

SAM.STR = {
  title: ['سام نریمان', 'SAM, SON OF NARIMAN'],
  subtitle: ['پهلوانِ پیش از رستم', 'The hero before Rostam'],
  chooseLang: ['زبان را انتخاب کنید', 'Choose your language'],
  start: ['آغاز داستان', 'Begin the tale'],
  cont: ['ادامهٔ داستان', 'Continue'],
  chapters: ['فصل‌ها', 'Chapters'],
  language: ['زبان: فارسی', 'Language: English'],
  soundOn: ['صدا: روشن', 'Sound: on'],
  soundOff: ['صدا: خاموش', 'Sound: off'],
  controls: ['راهنمای کنترل', 'Controls'],
  back: ['بازگشت', 'Back'],
  paused: ['درنگ', 'Paused'],
  resume: ['ادامه', 'Resume'],
  restart: ['از نو', 'Restart chapter'],
  toMenu: ['منوی اصلی', 'Main menu'],
  retry: ['دوباره', 'Try again'],
  locked: ['بسته', 'Locked'],
  skip: ['رد شدن ›', 'Skip ›'],
  tapToGo: ['برای ادامه بزنید', 'Tap to continue'],
  rotate: ['گوشی را افقی بگیرید', 'Turn your phone sideways'],
  chapter: ['فصل', 'Chapter'],
  ending: ['پایان', 'The End'],
  credits: ['بر پایهٔ شاهنامهٔ فردوسی', "Based on Ferdowsi's Shahnameh"],

  die_fall: ['سام از بلندی افتاد', 'Sam fell to his death'],
  die_spikes: ['خارهای آهنین', 'Impaled on the spikes'],
  die_slicer: ['تیغه‌های دژ', 'Caught by the blades'],
  die_drown: ['آب کشف‌رود سام را برد', 'The river took Sam'],
  die_hp: ['سام از پا افتاد', 'Sam has fallen'],

  complete: ['فصل به پایان رسید', 'Chapter complete'],
  leaves: ['برگ‌های شاهنامه', 'Shahnameh leaves'],
  time: ['زمان', 'Time'],
  deaths: ['افتادن‌ها', 'Falls'],

  maceGot: ['گرز گاوسر از آنِ توست!', 'The ox-head mace is yours!'],
  bowGot: ['کمان و تیرهای الماس‌پیکان', 'Bow and diamond-tipped arrows'],
  checkpoint: ['آتشدان روشن شد', 'The fire altar is lit'],
  potion: ['جان گرفتی', 'Health restored'],
  potionBig: ['جانت فزون شد', 'Your strength grows'],
  leaf: ['برگی از شاهنامه', 'A leaf of the Shahnameh'],
  gate: ['دروازه‌ای گشوده شد', 'A gate opens'],
  bridge: ['پل فرود آمد', 'The bridge comes down'],
  feather: ['پر سیمرغ سوخت و سام برخاست!', "The Simorgh's feather burns — Sam rises again!"],
  needMace: ['بی گرز نیا بازنمی‌گردم.', 'I will not leave without the mace.'],
  dragonHurt: ['تیر در کام اژدها نشست!', 'The arrow struck its jaws!'],

  ctl_title: ['راهنمای کنترل', 'Controls'],
  ctl_move: ['راه رفتن / دویدن', 'Walk / run'],
  ctl_up: ['پریدن رو به بالا، گرفتن لبه، بالا رفتن، رفتن به در', 'Jump up, grab a ledge, climb, enter a door'],
  ctl_down: ['خم شدن، آویزان شدن از لبه، رها کردن', 'Crouch, hang from an edge, let go'],
  ctl_jump: ['پرش رو به جلو (با دویدن: پرش بلند)', 'Jump forward (while running: long jump)'],
  ctl_walk: ['گام آهسته — از روی خار امن می‌گذری و از لبه نمی‌افتی', 'Careful step — safe on spikes, stops at edges'],
  ctl_attack: ['ضربهٔ گرز', 'Strike with the mace'],
  ctl_parry: ['دفع ضربه', 'Block'],
  ctl_shoot: ['تیر و کمان (از فصل ۲)', 'Bow (from chapter 2)'],
  ctl_pause: ['درنگ', 'Pause'],

  lv1: ['دژ نیاکان', 'The Fortress of the Forefathers'],
  lv2: ['اژدهای کشف‌رود', 'The Dragon of Kashafrud'],
  lv3: ['البرز کوه', 'Mount Alborz'],
  lv4: ['دیوان مازندران', 'The Demons of Mazandaran'],

  who_naqqal: ['نقّال', 'Storyteller'],
  who_sam: ['سام', 'Sam'],
  who_nariman: ['نریمان', 'Nariman'],
  who_manuchehr: ['منوچهر شاه', 'King Manuchehr'],
  who_simorgh: ['سیمرغ', 'The Simorgh'],
  who_mobed: ['موبد', 'The Mobed'],
  who_karkoy: ['کرکوی', 'Karkoy'],
  dragon: ['اژدهای کشف‌رود', 'The Dragon of Kashafrud'],
};

// Level hints, keyed by the digit placed on the map.
SAM.HINTS = {
  1: {
    1: ['با {left} و {right} راه برو و بدو.', 'Walk and run with {left} and {right}.'],
    2: ['زیر لبه بایست و {up} را بزن تا بپری و لبه را بگیری. باز {up} تا بالا بروی.', 'Stand under a ledge and press {up} to jump and grab it. Press {up} again to climb.'],
    3: ['بدو و {jump} بزن تا از شکاف پهن بپری. ایستاده، فقط از شکاف باریک می‌پری.', 'Run and press {jump} to leap a wide gap. Standing, you only clear a narrow one.'],
    4: ['روی خار ندو! {walk} را نگه دار و آهسته بگذر، یا از رویش بپر.', 'Never run onto spikes! Hold {walk} to step carefully, or jump over them.'],
    5: ['لب پرتگاه {down} را بزن تا آویزان شوی. باز {down} تا رها شوی.', 'At an edge press {down} to hang. Press {down} again to let go.'],
    6: ['گرز گاوسرِ گرشاسب، نیای تو. رویش برو تا برش داری.', 'The ox-head mace of Garshasp, your forefather. Step on it to take it.'],
    7: ['با {attack} گرز بزن و با {parry} ضربه را دفع کن. دفعِ به‌موقع، دشمن را گیج می‌کند.', 'Strike with {attack}, block with {parry}. A well-timed block staggers your foe.'],
  },
  2: {
    1: ['کمان را با {shoot} بکش. نشانه را بزن تا پل پایین بیاید.', 'Shoot your bow with {shoot}. Hit the target to lower the bridge.'],
    2: ['از سقف غار سنگ می‌ریزد. صبر کن، سپس بگذر.', 'Rocks fall from the cave roof. Wait, then pass.'],
  },
  3: {
    1: ['البرز بلند است. زیر هر لبه بایست و {up} بزن.', 'Alborz is high. Stand under each ledge and press {up}.'],
    2: ['سنگ‌ها از قله فرو می‌ریزند…', 'Stones tumble from the peak…'],
  },
  4: {
    1: ['تیغه‌های دژ را بپا. وقتی باز شدند، بگذر.', 'Mind the fortress blades. Pass when they open.'],
    2: ['کرکوی، نوادهٔ سلم، این‌جاست. دفع کن، سپس بکوب.', 'Karkoy, grandson of Salm, waits here. Block, then strike.'],
  },
};

SAM.KEYNAMES = {
  keys: { left: '←', right: '→', up: '↑', down: '↓', jump: 'Space', attack: 'X', parry: 'C', shoot: 'V', walk: 'Shift' },
  touch: {
    left: '◀', right: '▶', up: '▲', down: '▼',
    jump: ['«پرش»', 'JUMP'], attack: ['«گرز»', 'STRIKE'], parry: ['«دفاع»', 'BLOCK'],
    shoot: ['«کمان»', 'BOW'], walk: ['«آهسته»', 'SLOW'],
  },
};

SAM.t = function (key) {
  var s = SAM.STR[key];
  if (!s) return key;
  return SAM.lang === 'fa' ? s[0] : s[1];
};

SAM.pick = function (pair) {
  return SAM.lang === 'fa' ? pair[0] : pair[1];
};

SAM.fillKeys = function (text) {
  var names = SAM.KEYNAMES[SAM.inputMode];
  return text.replace(/\{(\w+)\}/g, function (_, k) {
    var n = names[k];
    if (Array.isArray(n)) n = SAM.pick(n);
    return n || k;
  });
};

SAM.hint = function (level, n) {
  var h = SAM.HINTS[level] && SAM.HINTS[level][n];
  return h ? SAM.fillKeys(SAM.pick(h)) : '';
};

SAM.num = function (n) {
  var s = String(n);
  if (SAM.lang !== 'fa') return s;
  return s.replace(/[0-9]/g, function (d) { return '۰۱۲۳۴۵۶۷۸۹'[d]; });
};

SAM.clock = function (sec) {
  var m = Math.floor(sec / 60), s = Math.floor(sec % 60);
  return SAM.num(m + ':' + (s < 10 ? '0' : '') + s);
};
