using System.Collections.Generic;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using NUnit.Framework;

namespace IranVsTuran.Tests
{
    /// <summary>Every piece of game data is valid and every text exists in both languages.</summary>
    public class ContentTests
    {
        static IEnumerable<string> RequiredKeys()
        {
            foreach (var chapter in LevelDefs.Chapters)
                yield return chapter.NameKey;
            foreach (var level in LevelDefs.All)
            {
                yield return level.NameKey;
                yield return level.DescKey;
                if (!string.IsNullOrEmpty(level.boss))
                    yield return "boss." + level.boss;
            }
            foreach (var enemy in EnemyDefs.All)
            {
                yield return enemy.NameKey;
                yield return enemy.LoreKey;
            }
            foreach (var tower in TowerDefs.All)
            {
                yield return tower.NameKey;
                yield return tower.DescKey;
            }
            foreach (var hero in HeroDefs.All)
            {
                yield return hero.NameKey;
                yield return hero.DescKey;
                yield return hero.AbilityKey;
            }
            foreach (var skin in HeroDefs.Skins)
                yield return skin.NameKey;
            foreach (var item in ItemDefs.All)
            {
                yield return item.NameKey;
                yield return item.DescKey;
            }
            foreach (var upgrade in UpgradeDefs.All)
            {
                yield return upgrade.NameKey;
                yield return upgrade.DescKey;
            }
            foreach (var product in ShopDefs.Products)
            {
                yield return product.NameKey;
                if (product.badgeKey != null)
                    yield return product.badgeKey;
            }
            foreach (var scene in StoryDefs.All)
                foreach (var slide in scene.slides)
                {
                    yield return slide.textKey;
                    if (slide.speaker != null)
                        yield return StoryDefs.NameKeyOf(slide.speaker);
                }
            for (var i = 0; i < 3; i++)
                yield return "difficulty." + i;
        }

        [Test]
        public void AllContentTextExists()
        {
            var missing = new List<string>();
            foreach (var key in RequiredKeys())
                if (key == null || !Loc.Has(key))
                    missing.Add(key ?? "(null)");
            Assert.IsEmpty(missing, "Missing strings: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryStringHasEnglishAndPersian()
        {
            foreach (var table in Strings.All)
                foreach (var entry in table)
                {
                    Assert.AreEqual(3, entry.Length, entry[0]);
                    Assert.IsFalse(string.IsNullOrEmpty(entry[1]), entry[0] + " has no English");
                    Assert.IsFalse(string.IsNullOrEmpty(entry[2]), entry[0] + " has no Persian");
                }
        }

        [Test]
        public void CutscenesReferencedByChaptersExist()
        {
            foreach (var chapter in LevelDefs.Chapters)
            {
                Assert.IsNotNull(StoryDefs.Get(chapter.intro), chapter.intro);
                Assert.IsNotNull(StoryDefs.Get(chapter.outro), chapter.outro);
                foreach (var id in chapter.levels)
                    Assert.IsNotNull(LevelDefs.Get(id), id);
            }
        }

        [Test]
        public void TowerSlotsSitBesideTheRoad()
        {
            foreach (var level in LevelDefs.All)
            {
                for (var i = 0; i < level.slots.Length; i++)
                {
                    var slot = level.slots[i];
                    var nearest = float.MaxValue;
                    for (var p = 0; p < level.PathCount; p++)
                        nearest = System.Math.Min(nearest, Geometry.PathDistance(slot, level.Path(p)));
                    Assert.That(nearest, Is.InRange(1.0f, 2.0f), level.id + " slot " + i);
                    for (var j = i + 1; j < level.slots.Length; j++)
                        Assert.Greater(P.Distance(slot, level.slots[j]), 1.4f, level.id + " slots " + i + "/" + j);
                    Assert.That(System.Math.Abs(slot.x), Is.LessThan(9.2f), level.id);
                    Assert.That(System.Math.Abs(slot.y), Is.LessThan(4.2f), level.id);
                }
            }
        }

        [Test]
        public void WavesUseKnownEnemiesAndBossesCloseTheLevel()
        {
            foreach (var level in LevelDefs.All)
            {
                Assert.Greater(level.waves.Length, 4, level.id);
                foreach (var wave in level.waves)
                {
                    Assert.Greater(wave.EnemyCount, 0, level.id);
                    foreach (var group in wave.groups)
                    {
                        Assert.IsNotNull(EnemyDefs.Get(group.enemy), level.id + ": " + group.enemy);
                        Assert.Less(group.path, level.PathCount, level.id);
                        Assert.Greater(group.interval, 0f, level.id);
                    }
                }
                if (!string.IsNullOrEmpty(level.boss))
                {
                    var last = level.waves[level.waves.Length - 1];
                    Assert.IsTrue(last.groups.Exists(g => g.enemy == level.boss), level.id + " boss");
                    Assert.IsTrue(EnemyDefs.Get(level.boss).boss, level.boss);
                }
            }
        }

        [Test]
        public void WavesAreTheSameEveryTime()
        {
            var a = WaveGen.Build(42, 8, 50, 300, 2, null, new RosterEntry("soldier", 1), new RosterEntry("raider", 2));
            var b = WaveGen.Build(42, 8, 50, 300, 2, null, new RosterEntry("soldier", 1), new RosterEntry("raider", 2));
            for (var i = 0; i < a.Length; i++)
                Assert.AreEqual(a[i].EnemyCount, b[i].EnemyCount);
        }

        [Test]
        public void AbilitiesSummonKnownEnemies()
        {
            foreach (var enemy in EnemyDefs.All)
                foreach (var ability in enemy.abilities)
                    if (ability.type == EnemyAbility.Summon || ability.type == EnemyAbility.SpawnOnDeath)
                        Assert.IsNotNull(EnemyDefs.Get(ability.summonId), enemy.id);
        }

        [Test]
        public void TowerUpgradesFormATree()
        {
            foreach (var id in TowerDefs.Buildable)
                Assert.AreEqual(1, TowerDefs.Get(id).tier, id);
            foreach (var tower in TowerDefs.All)
                foreach (var next in tower.upgrades)
                {
                    var def = TowerDefs.Get(next);
                    Assert.IsNotNull(def, next);
                    Assert.AreEqual(tower.family, def.family, next);
                    Assert.AreEqual(tower.tier + 1, def.tier, next);
                }
        }

        [Test]
        public void PassRewardsPointToRealThings()
        {
            for (var tier = 1; tier <= PassDefs.Tiers; tier++)
                foreach (var reward in new[] { PassDefs.Free(tier), PassDefs.Premium(tier) })
                {
                    Assert.Greater(reward.amount, 0, "tier " + tier);
                    if (reward.type == RewardType.Hero)
                        Assert.IsNotNull(HeroDefs.Get(reward.id));
                    if (reward.type == RewardType.Skin)
                        Assert.IsNotNull(HeroDefs.GetSkin(reward.id));
                    if (reward.type == RewardType.Item)
                        Assert.IsNotNull(ItemDefs.Get(reward.id));
                }
        }
    }
}
