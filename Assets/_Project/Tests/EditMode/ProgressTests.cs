using System.Collections.Generic;
using Arash.Core;
using Arash.Levels;
using NUnit.Framework;

namespace Arash.Tests
{
    public class ProgressTests
    {
        static readonly StarRules Rules = new StarRules { healthForSecondStar = 0.5f, maxArrowsForThirdStar = 3 };
        static readonly List<string> Ids = new List<string> { "a", "b", "c" };

        [Test]
        public void Stars_LossGivesNone()
        {
            Assert.AreEqual(0, ProgressRules.Stars(false, 1f, 1, Rules));
        }

        [Test]
        public void Stars_WinEarnsOneToThree()
        {
            Assert.AreEqual(1, ProgressRules.Stars(true, 0.2f, 9, Rules));
            Assert.AreEqual(2, ProgressRules.Stars(true, 0.5f, 9, Rules));
            Assert.AreEqual(2, ProgressRules.Stars(true, 0.2f, 3, Rules));
            Assert.AreEqual(3, ProgressRules.Stars(true, 1f, 2, Rules));
        }

        [Test]
        public void FirstLevelIsOpen_OthersNeedThePreviousWin()
        {
            var save = new SaveData();
            Assert.IsTrue(ProgressRules.IsUnlocked(0, Ids, 0, save));
            Assert.IsFalse(ProgressRules.IsUnlocked(1, Ids, 0, save));

            save.RecordWin("a", 1);
            Assert.IsTrue(ProgressRules.IsUnlocked(1, Ids, 0, save));
            Assert.IsFalse(ProgressRules.IsUnlocked(2, Ids, 0, save));
        }

        [Test]
        public void ChapterStarRequirement_LocksLevels()
        {
            var save = new SaveData();
            save.RecordWin("a", 2);
            Assert.IsFalse(ProgressRules.IsUnlocked(1, Ids, 3, save));
            save.RecordWin("a", 3);
            Assert.IsTrue(ProgressRules.IsUnlocked(1, Ids, 3, save));
        }

        [Test]
        public void RecordWin_KeepsBestStars()
        {
            var save = new SaveData();
            save.RecordWin("a", 3);
            save.RecordWin("a", 1);
            save.RecordWin("b", 2);

            Assert.AreEqual(3, save.GetStars("a"));
            Assert.AreEqual(5, save.TotalStars);
            Assert.AreEqual(0, save.GetStars("missing"));
        }

        [Test]
        public void OutOfRangeIndex_IsLocked()
        {
            Assert.IsFalse(ProgressRules.IsUnlocked(-1, Ids, 0, new SaveData()));
            Assert.IsFalse(ProgressRules.IsUnlocked(3, Ids, 0, new SaveData()));
        }
    }
}
