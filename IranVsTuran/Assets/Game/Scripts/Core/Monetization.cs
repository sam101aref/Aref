using System;
using IranVsTuran.Defs;
using UnityEngine;

namespace IranVsTuran.Core
{
    /// <summary>
    /// A store for real-money purchases (Google Play Billing via Unity IAP, Cafe Bazaar, Myket…).
    /// The game only talks to this interface; see docs/MONETIZATION.md for plugging one in.
    /// </summary>
    public interface IStoreBackend
    {
        bool IsAvailable { get; }
        /// <summary>The localized price, or null to show the product's fallback price.</summary>
        string PriceText(string productId);
        void Purchase(string productId, Action<bool> done);
    }

    /// <summary>A rewarded-video ad network (Unity LevelPlay, AdMob, Tapsell…).</summary>
    public interface IAdsBackend
    {
        bool IsRewardedReady { get; }
        /// <summary>Shows an ad; <paramref name="done"/> receives true only if it was watched to the end.</summary>
        void ShowRewarded(string placement, Action<bool> done);
    }

    /// <summary>
    /// Purchases and rewarded ads. Until real SDKs are added, <see cref="GameRoot"/> installs test
    /// backends that simulate an ad and a purchase (clearly marked TEST) so the whole flow can be
    /// played in any build. Ads are always optional and capped per day.
    /// </summary>
    public static class Monetization
    {
        public static IStoreBackend Store;
        public static IAdsBackend Ads;

        /// <summary>Overridable clock for tests (UTC day number).</summary>
        public static Func<int> Today = () => (int)(DateTime.UtcNow - new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;

        public static string Price(ProductDef product)
        {
            var text = Store != null && Store.IsAvailable ? Store.PriceText(product.id) : null;
            return string.IsNullOrEmpty(text) ? product.fallbackPrice : text;
        }

        public static bool Owns(ProductDef product)
        {
            switch (product.type)
            {
                case ProductType.NonConsumable:
                    return SaveSystem.Data.entitlements.Contains(product.id);
                case ProductType.Season:
                    return product.id == ShopDefs.BattlePassProduct && BattlePass.Premium;
                default:
                    return false;
            }
        }

        public static bool CanBuy(ProductDef product)
        {
            return Store != null && Store.IsAvailable && !Owns(product);
        }

        public static void Buy(ProductDef product, Action<bool> done)
        {
            if (!CanBuy(product))
            {
                if (done != null)
                    done(false);
                return;
            }

            Store.Purchase(product.id, success =>
            {
                if (success)
                    Deliver(product);
                if (done != null)
                    done(success);
            });
        }

        /// <summary>Gives what a product contains. Also used to restore purchases.</summary>
        public static void Deliver(ProductDef product)
        {
            Economy.Grant(product.rewards);
            if (product.type == ProductType.NonConsumable && !SaveSystem.Data.entitlements.Contains(product.id))
                SaveSystem.Data.entitlements.Add(product.id);
            if (product.id == ShopDefs.BattlePassProduct)
                BattlePass.UnlockPremium();
            Economy.Commit();
            Debug.Log("[Shop] Delivered " + product.id);
        }

        // ---- rewarded ads ----

        public static int AdsWatchedToday(string placement)
        {
            var state = AdState;
            return SaveData.Count(state.watched, placement);
        }

        public static int AdsLeftToday(string placement)
        {
            var def = ShopDefs.Ad(placement);
            return def == null ? 0 : Math.Max(0, def.dailyLimit - AdsWatchedToday(placement));
        }

        public static bool CanWatchAd(string placement)
        {
            return Ads != null && Ads.IsRewardedReady && AdsLeftToday(placement) > 0;
        }

        /// <summary>
        /// Shows a rewarded ad. If it is watched to the end, the placement's own reward (if any)
        /// is granted and <paramref name="done"/> receives true so the caller can add its own.
        /// </summary>
        public static void WatchAd(string placement, Action<bool> done)
        {
            if (!CanWatchAd(placement))
            {
                if (done != null)
                    done(false);
                return;
            }

            Ads.ShowRewarded(placement, watched =>
            {
                if (watched)
                {
                    var state = AdState;
                    SaveData.SetCount(state.watched, placement, SaveData.Count(state.watched, placement) + 1);
                    var def = ShopDefs.Ad(placement);
                    if (def != null && def.reward.amount > 0)
                        Economy.Grant(def.reward);
                    else
                        Economy.Commit();
                }
                if (done != null)
                    done(watched);
            });
        }

        static AdState AdState
        {
            get
            {
                var state = SaveSystem.Data.ads;
                var today = Today();
                if (state.day != today)
                {
                    state.day = today;
                    state.watched.Clear();
                }
                return state;
            }
        }
    }

    /// <summary>The seven-day login calendar.</summary>
    public static class DailyReward
    {
        public static bool CanClaim
        {
            get { return Monetization.Today() > SaveSystem.Data.daily.lastDay; }
        }

        /// <summary>Index (0–6) of the reward that the next claim gives.</summary>
        public static int NextIndex
        {
            get { return SaveSystem.Data.daily.index % ShopDefs.Daily.Length; }
        }

        public static Reward NextReward
        {
            get { return ShopDefs.Daily[NextIndex]; }
        }

        /// <summary>Claims today's reward (granted <paramref name="multiplier"/> times). Returns false if already claimed.</summary>
        public static bool Claim(int multiplier = 1)
        {
            if (!CanClaim)
                return false;
            var reward = NextReward;
            reward.amount *= Math.Max(1, multiplier);
            var daily = SaveSystem.Data.daily;
            daily.lastDay = Monetization.Today();
            daily.index = (daily.index + 1) % ShopDefs.Daily.Length;
            Economy.Grant(reward);
            BattlePass.AddXp(30);
            return true;
        }
    }
}
