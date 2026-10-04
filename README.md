# Arash The Archer · آرش کمانگیر

A story-driven, level-based 2D archery game for Android, based on the Persian legend of **Arash the Archer**.
Built with **Unity**. Fully bilingual: **فارسی** and **English**.

یک بازی داستانی و مرحله‌ای تیراندازی با کمان برای اندروید، بر اساس افسانهٔ **آرش کمانگیر**.
ساخته‌شده با موتور **یونیتی**، کاملاً دوزبانه (فارسی و انگلیسی).

## Documents · اسناد

| Document | Description |
|---|---|
| [docs/GDD.md](docs/GDD.md) | Game Design Document — سند طراحی بازی |
| [docs/FEATURES.md](docs/FEATURES.md) | Feature backlog & roadmap — فهرست ویژگی‌ها و نقشهٔ راه |

## Getting started · راه‌اندازی پروژه

**Requirements · پیش‌نیازها**
- [Unity Hub](https://unity.com/download) + **Unity 6.3 LTS** (or newer Unity 6 LTS) with the **Android Build Support** module (incl. OpenJDK and Android SDK & NDK)
- Git LFS: `git lfs install` (art and audio files are stored with LFS)

**First open · اولین باز کردن**
1. Clone the repo, then in Unity Hub choose **Add ▸ Add project from disk** and select the repo folder.
   ریپو را کلون کنید و در Unity Hub با **Add project from disk** پوشهٔ ریپو را اضافه کنید.
   If Hub asks for an editor version, pick your installed Unity 6.3.x.
   اگر Hub نسخهٔ ادیتور را پرسید، نسخهٔ 6.3 نصب‌شده را انتخاب کنید.
2. When the project opens, a dialog asks to install the required packages → **Install**.
   پس از باز شدن، پنجره‌ای برای نصب پکیج‌ها باز می‌شود ← **Install**.
3. After recompiling, the project configures itself automatically (URP 2D, Android settings, scenes, build target).
   بعد از کامپایل، پروژه خودش تنظیم می‌شود (URP 2D، تنظیمات اندروید، صحنه‌ها، پلتفرم Android).
4. When asked to restart for the new Input System → **Restart now**.
   وقتی برای Input System درخواست ری‌استارت داد ← **Restart now**.
5. Commit the files Unity generated (`ProjectSettings/`, `Packages/`, `*.meta`, `Assets/_Project/Settings`, `Assets/_Project/Scenes`).
   فایل‌هایی را که Unity ساخته کامیت کنید.

Setup can be re-run at any time from the menu **Arash ▸ Setup**.
هر وقت لازم شد، از منوی **Arash ▸ Setup** می‌توانید تنظیمات را دوباره اجرا کنید.

**Project settings applied · تنظیمات اعمال‌شده**

| Setting | Value |
|---|---|
| Render pipeline | URP with 2D Renderer (`Assets/_Project/Settings/URP-2D.asset`) |
| Input | Input System package only |
| Platform | Android, IL2CPP, ARM64, min API 24, target API auto |
| Orientation | Landscape (left/right) |
| Package name | `com.sam101aref.arashthearcher` — change in `ProjectSetup.cs` **before** the first Google Play release |
| Scenes | `Boot`, `MainMenu`, `WorldMap`, `Battle`, `Cutscene` |

## Project layout · ساختار پروژه

```
Assets/_Project/
  Art/  Audio/  Data/  Localization/  Prefabs/  Scenes/  Settings/
  Scripts/  (Core, Combat, AI, Levels, Story, UI, Progression)
  Editor/Setup/   project setup tools (F-01)
```
