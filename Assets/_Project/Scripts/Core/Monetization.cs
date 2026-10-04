using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arash.Core
{
    public enum MonetizationMode
    {
        /// <summary>Everything free, no ads (current default until the decision in GDD §8 is made).</summary>
        Free,
        /// <summary>GDD option A: the prologue and chapter 1 are free; one purchase unlocks the rest.</summary>
        Premium,
        /// <summary>GDD option B: free, with optional rewarded ads that double a level's coins.</summary>
        RewardedAds,
    }

    /// <summary>A store such as Google Play Billing (via Unity IAP).</summary>
    public interface IStoreBackend
    {
        bool IsAvailable { get; }
        string PriceText(string productId);
        void Purchase(string productId, Action<bool> done);
        void Restore(Action<IList<string>> done);
    }

    /// <summary>A rewarded-ads network (e.g. Unity LevelPlay or AdMob).</summary>
    public interface IAdsBackend
    {
        bool IsRewardedReady { get; }
        void ShowRewarded(Action<bool> rewarded);
    }

    /// <summary>
    /// Monetization (F-38). The game never sells power: purchases unlock chapters, ads only double
    /// coins and are always optional. Real store and ad networks plug in through
    /// <see cref="IStoreBackend"/> / <see cref="IAdsBackend"/>; until then development builds use a
    /// test store that grants purchases, and release builds offer nothing.
    /// </summary>
    public static class Monetization
    {
        public const string FullGameProduct = "full_game";
        /// <summary>Chapters before this index are free in Premium mode (prologue and chapter 1).</summary>
        public const int FreeChapters = 2;

        public static MonetizationMode Mode = MonetizationMode.Free;
        public static IStoreBackend Store = new DevelopmentStore();
        public static IAdsBackend Ads = new DevelopmentAds();

        public static bool OwnsFullGame(SaveData save)
        {
            return save.entitlements.Contains(FullGameProduct);
        }

        public static bool IsChapterAccessible(int chapterIndex, SaveData save)
        {
            return Mode != MonetizationMode.Premium || chapterIndex < FreeChapters || OwnsFullGame(save);
        }

        public static bool CanBuyFullGame(SaveData save)
        {
            return Mode == MonetizationMode.Premium && !OwnsFullGame(save) && Store != null && Store.IsAvailable;
        }

        public static void BuyFullGame(SaveData save, Action<bool> done)
        {
            Store.Purchase(FullGameProduct, success =>
            {
                if (success && !OwnsFullGame(save))
                {
                    save.entitlements.Add(FullGameProduct);
                    SaveSystem.Save();
                    Telemetry.Event("purchase", "product", FullGameProduct);
                }
                if (done != null)
                    done(success);
            });
        }

        public static bool CanOfferDoubleCoins(int coins)
        {
            return Mode == MonetizationMode.RewardedAds && coins > 0 && Ads != null && Ads.IsRewardedReady;
        }

        class DevelopmentStore : IStoreBackend
        {
            public bool IsAvailable { get { return Debug.isDebugBuild; } }

            public string PriceText(string productId)
            {
                return "TEST";
            }

            public void Purchase(string productId, Action<bool> done)
            {
                done(Debug.isDebugBuild);
            }

            public void Restore(Action<IList<string>> done)
            {
                done(new List<string>());
            }
        }

        class DevelopmentAds : IAdsBackend
        {
            public bool IsRewardedReady { get { return Debug.isDebugBuild; } }

            public void ShowRewarded(Action<bool> rewarded)
            {
                rewarded(Debug.isDebugBuild);
            }
        }
    }
}
