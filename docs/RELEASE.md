# Release checklist · چک‌لیست انتشار (F-39)

## 1. Upload key (one time) · کلید امضا (یک بار)

Google Play signs the final app itself (Play App Signing); we only sign uploads with an **upload key**.

```bash
keytool -genkeypair -v -keystore upload.keystore -alias arash-upload \
  -keyalg RSA -keysize 2048 -validity 10000
base64 -w0 upload.keystore > upload.keystore.b64   # macOS: base64 -i upload.keystore -o upload.keystore.b64
```

Keep `upload.keystore` and its passwords somewhere safe and **never commit them** (`.gitignore` already excludes `*.keystore`).

Add these repository secrets (Settings ▸ Secrets and variables ▸ Actions):

| Secret | Value |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | contents of `upload.keystore.b64` |
| `ANDROID_KEYSTORE_PASS` | keystore password |
| `ANDROID_KEYALIAS_NAME` | `arash-upload` |
| `ANDROID_KEYALIAS_PASS` | key password |

## 2. Build · ساخت

Push a version tag; the **Release Build** workflow builds a signed `.aab` and attaches it to the run:

```bash
git tag v1.0.0 && git push origin v1.0.0
```

The tag becomes the version name; the version code always increases with the workflow run number.

## 3. Before uploading · پیش از آپلود

- [ ] Application ID is final: `com.sam101aref.arashthearcher` (cannot change after the first upload; see `ProjectSetup.cs`)
- [ ] Monetization decided (GDD §8) and `Monetization.Mode` set; store/ads backends connected if used
- [ ] All levels played through on at least one low-end and one mid-range phone, 60 fps (development builds show an FPS counter)
- [ ] Persian and English texts proof-read (`Assets/_Project/Resources/Localization/Strings.json`)
- [ ] Placeholder art and synthesized audio replaced, or accepted for the first release
- [ ] Privacy policy published at a public URL ([privacy-policy.md](privacy-policy.md)) and still accurate: check the merged AndroidManifest permissions (only VIBRATE is expected) and any SDKs added since

## 4. Play Console · کنسول گوگل‌پلی

| Item | What to enter |
|---|---|
| App name | Arash The Archer / آرش کمانگیر (see [store/listing.md](store/listing.md)) |
| Category | Game ▸ Action (or Adventure) |
| Store listing | English + Persian (fa-IR) texts from [store/listing.md](store/listing.md); screenshots in both languages (landscape, 1920×1080) |
| Content rating (IARC) | Violence: fantasy/cartoon violence, no blood; no gambling, no user interaction → expected PEGI 7–12 |
| Target audience | 13+ (avoid "designed for children" requirements) |
| Data safety | No data collected or shared (true while no analytics/ads backend is connected; update if Firebase or ads are added) |
| Ads | "No ads" unless `MonetizationMode.RewardedAds` is enabled |
| Privacy policy URL | public URL of the privacy policy |

Start with an **internal testing** track, then closed testing, then production.

**New personal developer accounts** must run a **closed test with at least 12 testers for 14 days in a row** before Google allows production access. Plan for this: gather 12+ testers (friends with Android phones and Google accounts) early. The developer account itself needs a one-time $25 fee and identity verification in the name of the account owner.

## 5. Other stores (P2, F-46)

Cafe Bazaar and Myket accept APKs; build an APK with the normal Android Build workflow and sign it with the same key.
