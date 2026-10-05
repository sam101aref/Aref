# Siavosh the Prince · شاهزاده سیاوش

A story-driven 2D action-adventure for Android based on the Shahnameh tale of Siavosh. Persian and English.
یک بازی اکشن‌ماجرایی دوبعدی و داستان‌محور برای اندروید، بر اساس داستان سیاوش در شاهنامه؛ فارسی و انگلیسی.

This folder is a separate game from *Arash The Archer* (the Unity project at the repository root). It will become its own Unity project here.
این پوشه بازی جداگانه‌ای است و پروژهٔ Unity خودش را همین‌جا خواهد داشت.

| | |
|---|---|
| Game design document · سند طراحی | [../docs/siavosh/GDD.md](../docs/siavosh/GDD.md) |
| Visual designs (canvas, private to the owner) · طراحی‌های تصویری | https://claude.ai/artifact/DxeBqmLBMxTjW73okXumLf |

## Status · وضعیت

**First playable build · نخستین نسخهٔ قابل‌بازی** — language picker, menu, the storyteller's prologue, chapter 1 (five stages and Rostam's trial), skill tree, smith, story book, saving.
انتخاب زبان، منو، پیش‌درآمد نقّال، فصل ۱ (پنج مرحله و آزمون رستم)، درخت مهارت، آهنگر، کتاب داستان و ذخیره.

## Cloud build · ساخت ابری

Every push that changes `Siavosh/` builds an APK with [`.github/workflows/siavosh-android.yml`](../.github/workflows/siavosh-android.yml), using the same Unity secrets as Arash.
Download it from **Actions** → latest *Siavosh Android Build* → **Artifacts** → `SiavoshThePrince-apk`.
با هر push در پوشهٔ `Siavosh/`، فایل APK ساخته می‌شود: **Actions** ← *Siavosh Android Build* ← **Artifacts**.

## How to play · راهنمای بازی

| | On foot · پیاده | On Shabrang · سواره |
|---|---|---|
| Left · چپ | stick: walk, aim the bow up/down · جوی‌استیک | duck · خم شدن |
| Right · راست | sword (tap again for combos, hold for heavy blow), jump, roll, shield (raise just in time to parry), bow (hold, aim, release), Rostam's Fist, help · شمشیر، پرش، غلت، سپر، کمان، فرّ، کمک | jump (twice with Double leap), bow in the hunt · پرش، کمان |

Keyboard in the editor: A/D move, W/S aim, Space jump, J sword, K shield, L roll, I bow, U Farr, E help, Shift duck.

## Project layout · ساختار پروژه

```
Assets/_Project/
  Scripts/Core          game flow, save, rules (levels, skills, gear), settings, art loading
  Scripts/Localization  Persian shaping, bilingual text (LocText), interface strings
  Scripts/Story         story data, the script (Script.cs), cutscene player
  Scripts/Play          stages (Chapter1.cs), physics, hero, fighters, Rostam, riding, controls
  Scripts/UI            screens and HUD, built in code
  Scripts/Audio         synthesized Persian music and sound effects
  Resources/Art         sprites exported from Design/ (python3 Design/tools/export_game.py)
  Editor/               project setup and cloud build
  Tests/EditMode        tests, run by the cloud build before the APK
```

## Design art · هنر طراحی

All art is drawn in code as SVG in a Persian-miniature style, so it can be tweaked and re-rendered into game sprites later.

```
Design/
  tools/palette.py   pigment colours shared by everything
  tools/figures.py   standing characters (one style, many costumes)
  tools/horse.py     Shabrang in the flying gallop, with or without a rider
  tools/scenes.py    backgrounds: Zabulistan, the fire trial, coffeehouse, palace, the story curtain map
  art/               generated SVGs
```

Regenerate · ساخت دوباره:

```sh
cd Siavosh/Design
python3 tools/figures.py art && python3 tools/horse.py art && python3 tools/scenes.py art   # design SVGs
python3 tools/export_game.py                                                              # game sprites (needs node + playwright)
```
