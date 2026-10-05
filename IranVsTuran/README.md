# Iran vs Turan · ایران و توران

A Kingdom Rush–style tower defense game based on Ferdowsi's **Shahnameh**, built with **Unity 6** for Android.
Fully bilingual (فارسی / English).

یک بازی دفاع از برج به سبک «کینگدام راش» بر پایهٔ **شاهنامهٔ فردوسی**، ساخته‌شده با **یونیتی ۶** برای اندروید. کاملاً دوزبانه.

This is a separate Unity project from *Arash The Archer* (repo root); open the `IranVsTuran/` folder in Unity Hub.
این پروژه جدا از «آرش کمانگیر» است؛ در Unity Hub پوشهٔ `IranVsTuran/` را باز کنید.

## What's in the game · محتوای بازی

| | |
|---|---|
| Campaign · کارزار | 13 battles in 5 chapters: Afrasiab's invasion, Rostam's Seven Labours, Rostam & Sohrab, Vengeance for Siavash, bonus: Damavand · ۱۳ نبرد در ۵ فصل |
| Story · داستان | Storybook cutscenes before and after each chapter, boss battle cries · میان‌پرده‌ها و گفتار باس‌ها |
| Enemies · دشمنان | 14 Turanian and demon enemies + 8 bosses (Garsivaz, the Dragon, the White Div, Houman, Ashkbus, Akvan Div, Afrasiab, Zahhak) with abilities: summon, heal, stomp, fire breath, teleport, silence towers… |
| Towers · برج‌ها | 4 families × 3 tiers + 2 specialisations each: Tower of Arash / Kayanian Volley, Zabuli Champions / Kaviani Legion, Adur Gushnasp / Light of Sorush, Mountain Hurler / Naphtha Thrower |
| Heroes · پهلوانان | Rostam, Gordafarid, Zal, Esfandiar — each with an ability, 10 training levels and skins |
| Spells & items | Arash's Rain of arrows, Kaveh's smiths; Nushdaru, Simorgh feather, naphtha jar, Kay Khosrow's treasure |
| Economy · اقتصاد | Gold (زر) and gems (گوهر); Armory upgrades; hero training |
| Shop · بازار | Gem packs, starter pack, gold for gems, items, free gold/gems for ads |
| Ads · آگهی | Rewarded only (never forced): free gold/gems, double victory gold, revive after defeat, double daily gift — capped per day |
| Battle pass · نبردنامه | 8-week seasons, 30 tiers, free + royal track (royal pass sold in the store), tier skip with gems |
| Also | Daily login calendar, Book of Foes (Shahnameh lore), 3 difficulties, 3 stars per level, synthesized Persian music |

Art and sound are placeholders generated in code (no image or audio files), so the game runs end to end today and real art can replace it piece by piece.
گرافیک و صدا فعلاً با کد ساخته می‌شوند تا بازی از همین حالا کامل اجرا شود؛ بعداً می‌توان آن‌ها را با آثار هنری واقعی جایگزین کرد.

## Store and ads · فروشگاه و تبلیغات

Purchases and ads go through two small interfaces. Until real SDKs are connected, **test** versions are used: a fake 5-second ad and a "test purchase" dialog (no money is charged), clearly marked TEST. See [docs/MONETIZATION.md](docs/MONETIZATION.md).
خرید و تبلیغات فعلاً «آزمایشی» هستند (پولی برداشته نمی‌شود). برای وصل کردن کافه‌بازار/مایکت/گوگل‌پلی و تپسل/ادموب، [docs/MONETIZATION.md](docs/MONETIZATION.md) را ببینید.

## Cloud build · ساخت ابری

Every push that changes `IranVsTuran/` builds an APK with GitHub Actions ([`.github/workflows/iran-vs-turan.yml`](../.github/workflows/iran-vs-turan.yml)), using the same Unity secrets as Arash.
Download: **Actions** tab → *Iran vs Turan Android Build* → **Artifacts** → `IranVsTuran-apk`.
دانلود APK: تب **Actions** ← *Iran vs Turan Android Build* ← **Artifacts**.

EditMode tests (`Assets/Game/Tests/EditMode`) run before each build; a failing test stops the build.

## Project layout · ساختار

```
IranVsTuran/Assets/Game/
  Scripts/        assembly IVT.Runtime
    Defs/         game data: enemies, towers, heroes, levels & waves, story, shop, battle pass
    Core/         GameRoot (boot + navigation), save, economy, battle pass, monetization, progress
    Battle/       battlefield, enemies, allies, towers, projectiles, effects
    Art/          procedural painter for units, towers, icons, maps, backdrops
    Audio/        synthesized music and sound effects
    UI/           every screen and popup, built in code
    Localization/ Persian shaping and all strings (StringsUI/Content/Story.cs)
  Resources/Fonts Vazirmatn (SIL OFL)
  Editor/         project setup and CI build
  Tests/EditMode/ unit tests
```

The single `Boot` scene only holds a camera; `GameRoot` builds everything else at runtime.
