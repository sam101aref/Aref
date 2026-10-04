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

## Cloud build (no Unity install needed) · ساخت ابری (بدون نصب Unity)

Every push builds an installable **APK** on GitHub Actions ([`.github/workflows/android.yml`](.github/workflows/android.yml)).
Download it from the repo's **Actions** tab → latest *Android Build* run → **Artifacts** → `ArashTheArcher-apk`.

با هر push، فایل نصبی **APK** به‌صورت خودکار روی GitHub ساخته می‌شود. از تب **Actions** ← آخرین اجرای *Android Build* ← بخش **Artifacts** دانلودش کنید.

**One-time setup · تنظیم یک‌باره** (only the account owner can do this · فقط صاحب حساب می‌تواند انجام دهد)

1. Install [Unity Hub](https://unity.com/download) (the editor is not needed), sign in, then **Preferences ▸ Licenses ▸ Add ▸ Get a free personal license**.
   Unity Hub را نصب کنید (خود ادیتور لازم نیست)، وارد حساب شوید و از **Preferences ▸ Licenses ▸ Add** لایسنس رایگان Personal بگیرید.
2. Open the license file in a text editor and copy all of its contents:
   فایل لایسنس را با یک ویرایشگر متن باز کنید و کل محتوایش را کپی کنید:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. On GitHub: **Settings ▸ Secrets and variables ▸ Actions ▸ New repository secret**, add:
   در GitHub این سه Secret را اضافه کنید:

   | Secret | Value |
   |---|---|
   | `UNITY_LICENSE` | contents of `Unity_lic.ulf` · محتوای فایل لایسنس |
   | `UNITY_EMAIL` | your Unity account email · ایمیل حساب Unity |
   | `UNITY_PASSWORD` | your Unity account password · رمز حساب Unity |

4. Re-run the workflow from the **Actions** tab (or push any change).
   از تب **Actions** دوباره اجرا کنید.

The first run also commits the files Unity generates (`ProjectSettings/`, `*.meta`, scenes) back to the branch.
اولین اجرا فایل‌هایی را که Unity می‌سازد به برنچ کامیت می‌کند.

## Local development (optional) · توسعه روی سیستم خودتان (اختیاری)

- [Unity Hub](https://unity.com/download) + **Unity 6 LTS** (`6000.0.58f2` or a newer 6000.0 patch) with **Android Build Support**
- `git lfs install` (art and audio are stored with Git LFS)

1. In Unity Hub: **Add ▸ Add project from disk** → select the repo folder.
2. If the packages dialog appears → **Install**; after recompiling the project configures itself.
3. If asked to restart for the new Input System → **Restart now**.

Setup can be re-run any time from **Arash ▸ Setup**. · از منوی **Arash ▸ Setup** می‌توانید تنظیمات را دوباره اجرا کنید.

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
