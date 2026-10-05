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
        public void Shop_BuyingCostsCoinsAndRespectsStoryLocks()
        {
            var save = new SaveData { coins = 1000 };
            var horn = Armory.Find("bow.horn");

            Assert.AreEqual(PurchaseResult.Locked, Armory.TryBuy(save, horn));
            save.RecordWin("ch0_5", 1);
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, horn));
            Assert.AreEqual(800, save.coins);
            Assert.AreEqual(PurchaseResult.AlreadyOwned, Armory.TryBuy(save, horn));

            save.coins = 10;
            save.RecordWin("ch0_2", 1);
            Assert.AreEqual(PurchaseResult.NotEnoughCoins, Armory.TryBuy(save, Armory.Find("outfit.lapis")));
        }

        [Test]
        public void Shop_RareItemsCostGems()
        {
            var save = new SaveData { coins = 100000, gems = 10 };
            save.RecordWin("ch2_1", 1);
            var slot = Armory.Find("slot.3");
            Assert.AreEqual(PurchaseResult.NotEnoughGems, Armory.TryBuy(save, slot));
            save.gems = 45;
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, slot));
            Assert.AreEqual(5, save.gems);
            Assert.AreEqual(100000, save.coins);
        }

        [Test]
        public void Shop_BowSlotsLimitTheBowsCarried()
        {
            var save = new SaveData { coins = 100000 };
            foreach (var level in new[] { "ch0_5", "ch1_1", "ch1_4" })
                save.RecordWin(level, 1);

            // One slot: a new bow replaces the one carried.
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, Armory.Find("bow.horn")));
            CollectionAssert.AreEqual(new[] { "bow.horn" }, save.equippedBows);

            // A second slot: the next bow is carried as well.
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, Armory.Find("slot.2")));
            Assert.AreEqual(2, Armory.Slots(save));
            Assert.AreEqual(PurchaseResult.Bought, Armory.TryBuy(save, Armory.Find("bow.fire")));
            CollectionAssert.AreEqual(new[] { "bow.horn", "bow.fire" }, save.equippedBows);

            // Equipping an owned bow when the slots are full swaps out the last one; the last bow cannot be removed.
            save.owned.Add(Armory.DefaultBow);
            Armory.Equip(save, Armory.Find(Armory.DefaultBow));
            CollectionAssert.AreEqual(new[] { "bow.horn", Armory.DefaultBow }, save.equippedBows);
            Armory.Equip(save, Armory.Find("bow.horn"));
            Armory.Equip(save, Armory.Find(Armory.DefaultBow));
            CollectionAssert.AreEqual(new[] { Armory.DefaultBow }, save.equippedBows);

            var loadout = Armory.CurrentLoadout(save);
            Assert.AreEqual(1, loadout.Bows.Count);
            Assert.AreEqual(BowAbility.None, loadout.Bows[0].Item.Ability);
            Assert.AreEqual(0f, loadout.Bows[0].Item.Trail.a, 0.0001f, "the starting bow leaves no trail");
        }

        [Test]
        public void Shop_StoryBowIsGivenAndEquipped()
        {
            var save = new SaveData();
            var arashBow = Armory.Find(Armory.ArashBow);
            Assert.IsFalse(Armory.IsOwned(save, arashBow));

            save.RecordWin("ch4_8", 3);
            Armory.GrantStoryItems(save);
            Assert.IsTrue(save.Owns(Armory.ArashBow));
            Assert.AreEqual(Armory.ArashBow, Armory.CurrentLoadout(save).Bows[0].Item.Id);
        }

        [Test]
        public void Loadout_AddsUpArmourHelmetAndQuiver()
        {
            var save = new SaveData();
            save.owned.AddRange(new[] { "armor.scale", "helmet.iron", "quiver.2" });
            save.equippedArmor = "armor.scale";
            save.equippedHelmet = "helmet.iron";

            var loadout = Armory.CurrentLoadout(save);
            Assert.AreEqual(0.3f, loadout.ResistBody, 0.0001f);
            Assert.AreEqual(0.4f, loadout.ResistHead, 0.0001f);
            Assert.AreEqual(1.2f, loadout.HealthMultiplier, 0.0001f);
            Assert.AreEqual(42, loadout.Bows[0].Capacity); // 30 arrows + 40%

            // Gear that is not owned is ignored.
            save.equippedShield = "shield.simurgh";
            Assert.IsNull(Armory.CurrentLoadout(save).Shield);
        }

        [Test]
        public void Save_OldArmoryIsMigrated()
        {
            var save = new SaveData { version = 1, coins = 100 };
            save.owned.AddRange(new[] { "bow.horn", "bow.champion", "special.fire", "outfit.lapis" });
            save.upgrades.Add(new UpgradeRecord { id = "upgrade.health", level = 2 });
            save.equippedBow = "bow.horn";

            Armory.MigrateFromVersion1(save);

            Assert.IsTrue(save.Owns("bow.fire"));
            Assert.IsTrue(save.Owns("outfit.lapis"));
            Assert.IsFalse(save.Owns("bow.champion"));
            Assert.AreEqual(100 + 900 + 100 + 200, save.coins);
            Assert.AreEqual(0, save.upgrades.Count);
            CollectionAssert.AreEqual(new[] { "bow.horn" }, save.equippedBows);
        }

        [Test]
        public void Gems_AreClaimedOnce()
        {
            var save = new SaveData();
            Assert.IsTrue(save.ClaimGems("stars3.ch0_1", 3));
            Assert.IsFalse(save.ClaimGems("stars3.ch0_1", 3));
            Assert.AreEqual(3, save.gems);
        }

        [Test]
        public void Health_ArmourReducesDamageByZone()
        {
            var go = new UnityEngine.GameObject("Target");
            try
            {
                var health = go.AddComponent<Health>();
                health.SetResistance(0.5f, 0.25f, 0f);
                Assert.AreEqual(0.5f, health.DamageTaken(HitZoneType.Head), 0.0001f);
                Assert.AreEqual(0.75f, health.DamageTaken(HitZoneType.Torso), 0.0001f);
                Assert.AreEqual(1f, health.DamageTaken(HitZoneType.Limb), 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
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
