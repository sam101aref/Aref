using IranVsTuran.Art;
using IranVsTuran.Audio;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>
    /// The battle pass ("نبردنامه"): 30 tiers with a free track and a royal track. Winning
    /// battles and daily logins earn experience; the royal track is unlocked by buying the pass.
    /// </summary>
    public class BattlePassScreen : ScreenBase
    {
        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Mountain), new Color(0.45f, 0.48f, 0.58f));
            TopBar("pass.title", () => Root.ToMap());

            // header: season, progress, purchase
            var header = UIKit.Panel(Rect, new Color(0.05f, 0.07f, 0.13f, 0.85f), "Header");
            UIKit.Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1820f, 200f), false);

            var season = UIKit.Label(header.transform, Loc.T("pass.season", BattlePass.SeasonNumber), 46, UIKit.Gold, TextAnchor.MiddleLeft, true);
            UIKit.Place(season.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(600f, 70f));
            var days = UIKit.Label(header.transform, Loc.T("pass.days_left", BattlePass.DaysLeft), 32, UIKit.Cream, TextAnchor.MiddleLeft, false);
            UIKit.Place(days.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -90f), new Vector2(600f, 50f));

            var tierText = UIKit.Label(header.transform, Loc.T("pass.tier", BattlePass.Tier, PassDefs.Tiers), 34, UIKit.Cream, TextAnchor.MiddleLeft, true);
            UIKit.Place(tierText.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 18f), new Vector2(300f, 50f));
            var bar = UIKit.ProgressBar(header.transform, new Vector2(420f, 40f), UIKit.Turquoise);
            bar.fillAmount = BattlePass.XpIntoTier / (float)PassDefs.XpPerTier;
            UIKit.Place((RectTransform)bar.transform.parent, new Vector2(0f, 0f), new Vector2(340f, 22f), new Vector2(420f, 40f));

            if (BattlePass.Premium)
            {
                var royal = UIKit.Label(header.transform, Loc.T("pass.royal_owned"), 38, UIKit.Gold, TextAnchor.MiddleCenter, true);
                UIKit.Place(royal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120f, 0f), new Vector2(500f, 80f));
            }
            else
            {
                var product = ShopDefs.Product(ShopDefs.BattlePassProduct);
                var buy = UIKit.Button(header.transform, Loc.T("pass.buy", Monetization.Price(product)), () =>
                {
                    Monetization.Buy(product, success =>
                    {
                        if (success)
                        {
                            AudioService.Play(Sfx.Reward, 1f);
                            Rebuild();
                        }
                    });
                }, UIKit.Gold, 38, Icon.Crown);
                UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(120f, 0f), new Vector2(520f, 120f));
            }

            var skip = UIKit.Button(header.transform, Loc.T("pass.skip", PassDefs.TierSkipGems), () =>
            {
                if (BattlePass.SkipTier())
                    Rebuild();
                else
                    Root.Toast(Loc.T(BattlePass.Tier >= PassDefs.Tiers ? "pass.maxed" : "shop.not_enough_gems"));
            }, UIKit.Purple, 30, Icon.Gem);
            UIKit.Place(skip.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-330f, 0f), new Vector2(290f, 110f));

            var claimAll = UIKit.Button(header.transform, Loc.T("pass.claim_all"), () =>
            {
                if (BattlePass.ClaimAll() > 0)
                {
                    AudioService.Play(Sfx.Reward, 1f);
                    Rebuild();
                }
            }, BattlePass.UnclaimedCount > 0 ? UIKit.Green : UIKit.Muted, 32);
            UIKit.Place(claimAll.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(280f, 110f));

            // row labels
            Track(Loc.T("pass.free"), -480f);
            Track(Loc.T("pass.royal"), -720f);

            var area = UIKit.Rect(Rect, "Tiers");
            area.anchorMin = new Vector2(0f, 0f);
            area.anchorMax = new Vector2(1f, 1f);
            area.offsetMin = new Vector2(Loc.IsRtl ? 30f : 200f, 40f);
            area.offsetMax = new Vector2(Loc.IsRtl ? -200f : -30f, -360f);
            var content = UIKit.ScrollRow(area, 14f);
            for (var tier = 1; tier <= PassDefs.Tiers; tier++)
                Column(content, tier);
        }

        void Track(string text, float y)
        {
            var label = UIKit.Label(Rect, text, 36, UIKit.Gold, TextAnchor.MiddleCenter, true);
            UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(160f, 80f));
        }

        void Column(RectTransform parent, int tier)
        {
            var column = UIKit.Rect(parent, "Tier " + tier);
            UIKit.Size(column, 190f, 600f);
            var reached = tier <= BattlePass.Tier;

            var number = UIKit.Panel(column, reached ? UIKit.Turquoise : new Color(0f, 0f, 0f, 0.55f), "Number");
            UIKit.Place(number.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(110f, 64f), false);
            var label = UIKit.Label(number.transform, Loc.Number(tier), 36, Color.white, TextAnchor.MiddleCenter, true);
            UIKit.Stretch(label.rectTransform);

            Reward(column, tier, false, -80f);
            Reward(column, tier, true, -320f);
        }

        void Reward(RectTransform column, int tier, bool premium, float y)
        {
            var reward = premium ? PassDefs.Premium(tier) : PassDefs.Free(tier);
            var claimed = BattlePass.IsClaimed(tier, premium);
            var claimable = BattlePass.CanClaim(tier, premium);
            var color = premium ? Raster.Hex(0xF4D98A) : UIKit.Parchment;
            if (claimable)
                color = Raster.Hex(0xA8E6A0);
            var box = UIKit.Panel(column, color, "Reward");
            UIKit.Place(box.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(180f, 220f), false);
            var badge = UIKit.RewardBadge(box.transform, reward, 140f);
            UIKit.Place(badge, new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(140f, 140f), false);
            var name = UIKit.Label(box.transform, ShortName(reward), 22, UIKit.Ink, TextAnchor.MiddleCenter, true, false);
            UIKit.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(175f, 40f), false);

            if (claimed)
            {
                box.color = new Color(color.r, color.g, color.b, 0.5f);
                var check = UIKit.Icon(box.transform, Icon.Check, 90f);
                check.color = UIKit.Green;
                UIKit.Center(check.rectTransform, new Vector2(0f, 20f), new Vector2(90f, 90f), false);
            }
            else if (premium && !BattlePass.Premium)
            {
                var lockIcon = UIKit.Icon(box.transform, Icon.Lock, 60f);
                UIKit.Place(lockIcon.rectTransform, new Vector2(1f, 1f), new Vector2(10f, 10f), new Vector2(60f, 60f), false);
            }

            var button = box.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                if (BattlePass.Claim(tier, premium))
                {
                    AudioService.Play(Sfx.Reward, 1f);
                    Root.Toast(UIKit.RewardName(reward));
                    Rebuild();
                }
                else if (!claimed)
                    Root.Toast(Loc.T(premium && !BattlePass.Premium ? "pass.need_royal" : "pass.need_tier", tier));
            });
        }

        static string ShortName(Reward reward)
        {
            switch (reward.type)
            {
                case RewardType.Hero: return Loc.T("hero." + reward.id);
                case RewardType.Skin: return Loc.T("skin." + reward.id);
                case RewardType.Item: return Loc.T("item." + reward.id);
                default: return string.Empty;
            }
        }

        public override void OnBack()
        {
            Root.ToMap();
        }
    }
}
