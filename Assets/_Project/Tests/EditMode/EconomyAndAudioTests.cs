using System;
using System.Collections.Generic;
using Arash.Audio;
using Arash.Combat;
using Arash.Core;
using NUnit.Framework;

namespace Arash.Tests
{
    public class EconomyAndAudioTests
    {
        [Test]
        public void Farr_HeadshotsFillFasterAndMeterCaps()
        {
            Assert.Greater(FarrRules.Gain(HitZoneType.Head, false), FarrRules.Gain(HitZoneType.Torso, false));
            Assert.AreEqual(0f, FarrRules.Gain(HitZoneType.Armor, false), 0.0001f);

            var meter = new FarrMeter { GainMultiplier = 2f };
            meter.Add(0.3f);
            Assert.AreEqual(0.6f, meter.Value, 0.0001f);
            meter.Add(1f);
            Assert.IsTrue(meter.IsFull);
            Assert.IsTrue(meter.TrySpend());
            Assert.AreEqual(0f, meter.Value, 0.0001f);
            Assert.IsFalse(meter.TrySpend());
        }

        [Test]
        public void Armory_BuyingCostsCoinsAndRespectsStoryLocks()
        {
            var save = new SaveData { coins = 1000 };
            var horn = Armory.Find("bow.horn");

            Assert.AreEqual(PurchaseResult.Locked, Armory.TryBuy(save, horn));
            save.RecordWin("ch0_5", 1);
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, horn));
            Assert.AreEqual(700, save.coins);
            Assert.AreEqual(PurchaseResult.AlreadyOwned, Armory.TryBuy(save, horn));

            save.coins = 10;
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, Armory.TryBuy(save, Armory.Find("outfit.lapis")));
        }

        [Test]
        public void Armory_UpgradesHaveLevels()
        {
            var save = new SaveData { coins = 10000 };
            var health = Armory.Find(Armory.HealthUpgrade);
            for (var i = 0; i < health.MaxLevel; i++)
                Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, health));

            Assert.AreEqual(PurchaseResult.MaxedOut, Armory.TryBuy(save, health));
            Assert.AreEqual(10000 - 100 - 200 - 350, save.coins);
            Assert.AreEqual(1.3f, Armory.CurrentLoadout(save).HealthMultiplier, 0.0001f);
        }

        [Test]
        public void Armory_StoryBowIsGivenAndLoadoutFollowsEquipment()
        {
            var save = new SaveData();
            var arashBow = Armory.Find("bow.arash");
            Assert.IsFalse(Armory.IsOwned(save, arashBow));

            save.RecordWin("ch4_8", 3);
            Assert.IsTrue(Armory.IsOwned(save, arashBow));
            Assert.IsTrue(Armory.Equip(save, arashBow));
            var loadout = Armory.CurrentLoadout(save);
            Assert.AreEqual(34f, loadout.MaxSpeed, 0.0001f);
            Assert.AreEqual(1.2f, loadout.FarrMultiplier, 0.0001f);

            // Equipment that is not owned falls back to the defaults.
            save.equippedBow = "bow.champion";
            Assert.AreEqual(28f, Armory.CurrentLoadout(save).MaxSpeed, 0.0001f);
        }

        [Test]
        public void Armory_SelectedSpecialArrowComesFirst()
        {
            var save = new SaveData();
            save.owned.Add("special.fire");
            save.owned.Add("special.triple");
            save.selectedSpecial = "special.triple";

            var specials = Armory.CurrentLoadout(save).Specials;
            Assert.AreEqual(2, specials.Count);
            Assert.AreEqual(SpecialArrow.Triple, specials[0]);
        }

        [Test]
        public void Monetization_PremiumLocksLaterChaptersUntilBought()
        {
            var save = new SaveData();
            var previous = Monetization.Mode;
            try
            {
                Monetization.Mode = MonetizationMode.Free;
                Assert.IsTrue(Monetization.IsChapterAccessible(4, save));

                Monetization.Mode = MonetizationMode.Premium;
                Assert.IsTrue(Monetization.IsChapterAccessible(1, save));
                Assert.IsFalse(Monetization.IsChapterAccessible(2, save));
                save.entitlements.Add(Monetization.FullGameProduct);
                Assert.IsTrue(Monetization.IsChapterAccessible(2, save));
            }
            finally
            {
                Monetization.Mode = previous;
            }
        }

        [Test]
        public void ShurScale_HasTheKoronSecondAndOctaves()
        {
            var tonic = 200f;
            Assert.AreEqual(tonic, PersianScale.Frequency(tonic, 0), 0.001f);
            Assert.AreEqual(tonic * Math.Pow(2, 150 / 1200.0), PersianScale.Frequency(tonic, 1), 0.01f);
            Assert.AreEqual(tonic * 2f, PersianScale.Frequency(tonic, 7), 0.001f);
            Assert.AreEqual(tonic / 2f, PersianScale.Frequency(tonic, -7), 0.001f);
        }

        [Test]
        public void Synth_PluckHasTheRequestedLengthAndStaysBounded()
        {
            var pluck = Synth.Pluck(220f, 0.5f, 0.996f, 1);
            Assert.AreEqual(Synth.Samples(0.5f), pluck.Length);
            foreach (var s in pluck)
                Assert.IsTrue(Math.Abs(s) <= 1.0001f);
        }

        [Test]
        public void Music_IsDeterministicAndLoopLength()
        {
            var a = MusicComposer.Compose(7, 180f, 4, true);
            var b = MusicComposer.Compose(7, 180f, 4, true);

            Assert.AreEqual(Synth.Samples(4 * 6 * 60f / 180f), a.Length);
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i += 997)
                Assert.AreEqual(a[i], b[i], 0f);
        }

        [Test]
        public void EverySoundEffectBuilds()
        {
            foreach (Sfx sfx in Enum.GetValues(typeof(Sfx)))
                Assert.Greater(SfxLibrary.Build(sfx).Length, 100f);
        }

        [Test]
        public void TelemetryFormatsParameters()
        {
            var text = Telemetry.Format(new Dictionary<string, object> { { "level", "ch1_1" }, { "won", true } });
            Assert.AreEqual("level=ch1_1 won=True", text);
        }
    }
}
