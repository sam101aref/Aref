# Sam Nariman · سام نریمان

A Prince of Persia–style 2D adventure based on the Shahnameh tale of **Sam, son of Nariman**, Rostam's grandfather. Persian and English.
یک بازی ماجرایی دوبعدی به سبک پرنس آو پرشیا، بر اساس داستان **سام نریمان** در شاهنامه؛ فارسی و انگلیسی.

This is a separate game from *Arash The Archer* (the Unity project at the repository root) and *Siavosh the Prince*.
It follows their patterns: the storyteller-and-curtain frame and miniature art of Siavosh, and the bow, synthesized Shur music, bilingual UI, feature IDs and automated tests of Arash.

| | |
|---|---|
| Game design document · سند طراحی | [../docs/sam/GDD.md](../docs/sam/GDD.md) |
| Features & roadmap · فهرست ویژگی‌ها | [../docs/sam/FEATURES.md](../docs/sam/FEATURES.md) |

## Play · بازی کردن

Open `Sam/game/index.html` in any browser — on a phone, hold it sideways. No install, no build step.
فایل `Sam/game/index.html` را در هر مرورگری باز کنید؛ روی گوشی، افقی نگه دارید. نصب و ساخت لازم ندارد.

Keyboard: ← → move · ↑ jump up / climb · ↓ crouch / hang · Space jump · Shift careful step · X strike · C block · V bow · Esc pause.

## Status · وضعیت

**Playable · قابل‌بازی** — 4 chapters, 2 bosses, cutscenes, Persian/English, touch and keyboard. Every chapter is finished by the automated playtest.
۴ فصل، ۲ باس، میان‌پرده‌ها، فارسی/انگلیسی، لمسی و کیبورد. هر چهار فصل را تست خودکار تا آخر بازی می‌کند.

## Layout · ساختار

```
game/
  index.html        page, fonts, touch buttons
  js/i18n.js        Persian/English strings and hints
  js/story.js       the storyteller's cutscenes (📜 Shahnameh / ✏️ added for the game)
  js/levels.js      the four chapter maps (legend at the top of the file)
  js/art.js         everything drawn in code: cast rig, poses, tiles, dragon, Simorgh, paintings
  js/audio.js       Web Audio music in dastgah-e Shur and sound effects
  js/world.js       cells, traps, falling things, checkpoints, room drawing
  js/player.js      Sam: movement, ledges, falls, combat
  js/enemies.js     guards, Karkoy, the dragon
  js/ui.js          input, touch buttons, save data, UI drawing helpers
  js/main.js        screens, play loop, story flow
tools/
  check-levels.js   proves every map can be finished (shape + reachability search)
  playtest.js       plays all four chapters in headless Chromium
```

## Tests · تست‌ها

```sh
node Sam/tools/check-levels.js
cd Sam && npm install && npx playwright install chromium && npm run playtest
```

Both run in GitHub Actions on every change under `Sam/` ([`.github/workflows/sam.yml`](../.github/workflows/sam.yml)).
