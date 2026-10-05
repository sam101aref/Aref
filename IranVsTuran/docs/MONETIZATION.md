# Monetization · کسب درآمد

The game never forces ads and never sells power that can't also be earned.

## Currencies
- **Gold (زر)** — earned by winning battles, daily gifts, ads, the battle pass; spent on Armory upgrades and hero training.
- **Gems (گوهر)** — earned from new stars (5 per star), daily gifts, ads (3/day), the battle pass; bought with real money; spent on heroes, skins, items, gold, tier skips and revives.

## Real-money products (`Defs/ShopDefs.cs`)
| id | type | content |
|---|---|---|
| `gems_small/medium/large/huge` | consumable | 80 / 500 / 1100 / 2500 gems |
| `starter_pack` | one-time | 300 gems, 2000 gold, Gordafarid, 3× each item |
| `battle_pass` | per season | royal track of the current season |

Create products with these ids in your store console.

## Rewarded ad placements
`ad_gold` (150 gold, 5/day), `ad_gems` (5 gems, 3/day), `ad_double` (double victory gold), `ad_revive` (+5 lives after defeat), `ad_daily` (double daily gift).

## Plugging in real SDKs
`Core/Monetization.cs` defines two interfaces:

```csharp
public interface IStoreBackend { bool IsAvailable { get; } string PriceText(string productId); void Purchase(string productId, Action<bool> done); }
public interface IAdsBackend  { bool IsRewardedReady { get; } void ShowRewarded(string placement, Action<bool> done); }
```

Implement them with your chosen SDK and assign them before `GameRoot` starts (or in `GameRoot.Awake`, replacing `TestStore` / `TestAds`):

- **Iranian market**: Cafe Bazaar (Poolakey) or Myket billing; Tapsell or Adivery for ads.
- **Google Play**: Unity IAP (`com.unity.purchasing`) and Unity LevelPlay or AdMob.

Purchases must be verified and consumed by the store backend before calling `done(true)`; `Monetization.Deliver` then grants the content. Non-consumables (starter pack) should be restored on reinstall by calling `Monetization.Deliver` for each owned product.
