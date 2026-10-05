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

**Design stage · مرحلهٔ طراحی** — art direction, cast, screens, cutscenes and gameplay mock-ups are up for review. No game code yet.
سبک هنری، شخصیت‌ها، صفحه‌ها، میان‌پرده‌ها و نمونهٔ گیم‌پلی آمادهٔ نظر دادن است؛ هنوز کد بازی نوشته نشده.

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
python3 tools/figures.py art && python3 tools/horse.py art && python3 tools/scenes.py art
```
