using System;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using NUnit.Framework;

namespace IranVsTuran.Tests
{
    /// <summary>Gold, gems, items, the battle pass, ads, daily gifts and campaign progress.</summary>
    public class EconomyTests
    {
        class FakeAds : IAdsBackend
        {
            public bool Watch = true;
            public bool IsRewardedReady { get { return true; } }
            public void ShowRewarded(string placement, Action<bool> done) { done(Watch); }
        }

        class FakeStore : IStoreBackend
        {
            public bool IsAvailable { get { return true; } }
            public string PriceText(string productId) { return null; }
            public void Purchase(string productId, Action<bool> done) { done(true); }
        }

        DateTime now;
        int today;

        [SetUp]
        public void SetUp()
        {
            SaveSystem.UseInMemory(new SaveData());
            now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
            today = 100;
            BattlePass.Now = () => now;
            Monetization.Today = () => today;
            Monetization.Ads = new FakeAds();
            Monetization.Store = new FakeStore();
        }

        [Test]
        public void SpendingNeverGoesNegative()
        {
            Economy.AddGold(100);
            Assert.IsFalse(Economy.TrySpendGold(150));
            Assert.IsTrue(Economy.TrySpendGold(100));
            Assert.AreEqual(0, Economy.Gold);
            Assert.IsFalse(Economy.TrySpendGems(1));
        }

        [Test]
        public void DuplicateHeroRewardBecomesGems()
        {
            Economy.Grant(Reward.Hero(HeroIds.Zal));
            Assert.IsTrue(SaveSystem.Data.OwnsHero(HeroIds.Zal));
            Economy.Grant(Reward.Hero(HeroIds.Zal));
            Assert.AreEqual(Economy.DuplicateHeroGems, Economy.Gems);
        }

        [Test]
        public void SkinRewardAlsoGivesItsHero()
        {
            Economy.Grant(Reward.Skin("esfandiar_gold"));
            Assert.IsTrue(SaveSystem.Data.OwnsHero(HeroIds.Esfandiar));
            Assert.IsTrue(SaveSystem.Data.skins.Contains("esfandiar_gold"));
        }

        [Test]
        public void BattlePassTiersAndClaims()
        {
            BattlePass.AddXp(250);
            Assert.AreEqual(2, BattlePass.Tier);
            Assert.AreEqual(50, BattlePass.XpIntoTier);
            Assert.IsTrue(BattlePass.CanClaim(1, false));
            Assert.IsFalse(BattlePass.CanClaim(3, false));
            Assert.IsFalse(BattlePass.CanClaim(1, true), "royal track needs the pass");
            Assert.IsTrue(BattlePass.Claim(1, false));
            Assert.IsFalse(BattlePass.Claim(1, false), "claimed only once");
            BattlePass.UnlockPremium();
            Assert.AreEqual(3, BattlePass.ClaimAll());
            Assert.AreEqual(0, BattlePass.UnclaimedCount);
        }

        [Test]
        public void NewSeasonResetsThePass()
        {
            BattlePass.AddXp(1000);
            BattlePass.UnlockPremium();
            now = now.AddDays(PassDefs.SeasonDays);
            Assert.AreEqual(0, BattlePass.Tier);
            Assert.IsFalse(BattlePass.Premium);
        }

        [Test]
        public void BuyingThePassUnlocksTheRoyalTrack()
        {
            var product = ShopDefs.Product(ShopDefs.BattlePassProduct);
            var result = false;
            Monetization.Buy(product, success => result = success);
            Assert.IsTrue(result);
            Assert.IsTrue(BattlePass.Premium);
            Assert.IsFalse(Monetization.CanBuy(product));
        }

        [Test]
        public void StarterPackIsDeliveredOnce()
        {
            var pack = ShopDefs.Product(ShopDefs.StarterPack);
            Monetization.Buy(pack, null);
            Assert.IsTrue(SaveSystem.Data.OwnsHero(HeroIds.Gordafarid));
            Assert.AreEqual(300, Economy.Gems);
            Assert.IsFalse(Monetization.CanBuy(pack));
        }

        [Test]
        public void AdsAreCappedPerDay()
        {
            var placement = ShopDefs.Ad(ShopDefs.AdFreeGold);
            for (var i = 0; i < placement.dailyLimit; i++)
                Monetization.WatchAd(placement.id, null);
            Assert.AreEqual(placement.reward.amount * placement.dailyLimit, Economy.Gold);
            Assert.IsFalse(Monetization.CanWatchAd(placement.id));
            today++;
            Assert.IsTrue(Monetization.CanWatchAd(placement.id));
        }

        [Test]
        public void SkippedAdGivesNothing()
        {
            ((FakeAds)Monetization.Ads).Watch = false;
            var result = true;
            Monetization.WatchAd(ShopDefs.AdFreeGems, watched => result = watched);
            Assert.IsFalse(result);
            Assert.AreEqual(0, Economy.Gems);
            Assert.AreEqual(0, Monetization.AdsWatchedToday(ShopDefs.AdFreeGems));
        }

        [Test]
        public void DailyGiftOncePerDayAndCycles()
        {
            Assert.IsTrue(DailyReward.Claim());
            Assert.IsFalse(DailyReward.Claim());
            for (var i = 1; i < ShopDefs.Daily.Length; i++)
            {
                today++;
                Assert.IsTrue(DailyReward.Claim(2));
            }
            today++;
            Assert.AreEqual(0, DailyReward.NextIndex);
        }

        [Test]
        public void StarsFollowLivesLeft()
        {
            Assert.AreEqual(3, Progress.StarsFor(20, 20));
            Assert.AreEqual(3, Progress.StarsFor(18, 20));
            Assert.AreEqual(2, Progress.StarsFor(17, 20));
            Assert.AreEqual(2, Progress.StarsFor(6, 20));
            Assert.AreEqual(1, Progress.StarsFor(1, 20));
            Assert.AreEqual(0, Progress.StarsFor(0, 20));
        }

        [Test]
        public void WinningUnlocksTheNextLevelAndPaysLessOnReplay()
        {
            var first = LevelDefs.All[0];
            var second = LevelDefs.All[1];
            Assert.IsTrue(Progress.IsUnlocked(first));
            Assert.IsFalse(Progress.IsUnlocked(second));

            var win = Progress.RecordVictory(first, 2);
            Assert.IsTrue(win.firstWin);
            Assert.AreEqual(2 * Progress.GemsPerNewStar, win.gems);
            Assert.IsTrue(Progress.IsUnlocked(second));

            var replay = Progress.RecordVictory(first, 3);
            Assert.IsFalse(replay.firstWin);
            Assert.AreEqual(1, replay.newStars);
            Assert.Less(replay.gold, win.gold);
        }

        [Test]
        public void WinningTheWhiteCastleBringsGordafarid()
        {
            var result = Progress.RecordVictory(LevelDefs.Get("l7"), 1);
            Assert.AreEqual(HeroIds.Gordafarid, result.unlockedHero);
        }

        [Test]
        public void HeroTrainingCostsGoldAndStopsAtMax()
        {
            SaveSystem.Data.AddHero(HeroIds.Rostam);
            var rostam = HeroDefs.Get(HeroIds.Rostam);
            Assert.IsFalse(Heroes.Train(rostam));
            Economy.AddGold(100000);
            for (var i = 1; i < HeroDefs.MaxLevel; i++)
                Assert.IsTrue(Heroes.Train(rostam));
            Assert.IsFalse(Heroes.Train(rostam));
            Assert.AreEqual(HeroDefs.MaxLevel, Heroes.Level(HeroIds.Rostam));
        }

        [Test]
        public void UpgradesStackPerRank()
        {
            Economy.AddGold(100000);
            var def = UpgradeDefs.Get(UpgradeDefs.ArcherDamage);
            Assert.IsTrue(Upgrades.Buy(def));
            Assert.IsTrue(Upgrades.Buy(def));
            Assert.AreEqual(0.2f, Upgrades.Bonus(def.id), 0.0001f);
        }

        [Test]
        public void SaveSurvivesJsonRoundTrip()
        {
            var save = SaveSystem.NewGame();
            save.RecordWin("l1", 3);
            BattlePass.AddXp(10);
            var copy = UnityEngine.JsonUtility.FromJson<SaveData>(UnityEngine.JsonUtility.ToJson(save));
            copy.Repair();
            Assert.AreEqual(3, copy.GetStars("l1"));
            Assert.IsTrue(copy.OwnsHero(HeroIds.Rostam));
        }
    }
}
