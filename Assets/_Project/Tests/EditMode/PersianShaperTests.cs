using Arash.Localization;
using NUnit.Framework;

namespace Arash.Tests
{
    public class PersianShaperTests
    {
        [Test]
        public void JoinsLettersAndReversesForDisplay()
        {
            // salam: seen initial, lam-alef final ligature, meem isolated; shown right-to-left.
            Assert.AreEqual("\uFEE1\uFEFC\uFEB3", PersianShaper.Shape("\u0633\u0644\u0627\u0645"));
            // baad: beh initial, alef final, dal isolated (alef never joins the next letter).
            Assert.AreEqual("\uFEA9\uFE8E\uFE91", PersianShaper.Shape("\u0628\u0627\u062F"));
        }

        [Test]
        public void LamAlef_BecomesLigature()
        {
            Assert.AreEqual("\uFEFB", PersianShaper.Shape("\u0644\u0627"));
            // کلاس: keheh initial, lam-alef final ligature, seen isolated.
            Assert.AreEqual("\uFEB1\uFEFC\uFB90", PersianShaper.Shape("\u06A9\u0644\u0627\u0633"));
        }

        [Test]
        public void ZeroWidthNonJoiner_BreaksTheJoinAndIsRemoved()
        {
            // می‌شود: meem initial, farsi yeh final | sheen initial, waw final, dal isolated.
            Assert.AreEqual("\uFEA9\uFEEE\uFEB7\uFBFD\uFEE3", PersianShaper.Shape("\u0645\u06CC\u200C\u0634\u0648\u062F"));
        }

        [Test]
        public void NumbersKeepLeftToRightOrder()
        {
            Assert.AreEqual("\u06F1\u06F2 \uFEEA\uFEE0\uFEA3\uFEAE\uFEE3", PersianShaper.Shape("\u0645\u0631\u062D\u0644\u0647 \u06F1\u06F2"));
        }

        [Test]
        public void LatinWordsKeepTheirOrder()
        {
            Assert.AreEqual("Arash The Archer \uFBFC\uFEAF\uFE8E\uFE91", PersianShaper.Shape("\u0628\u0627\u0632\u06CC Arash The Archer"));
        }

        [Test]
        public void BracketsAreMirrored()
        {
            Assert.AreEqual("(\uFEE1\uFEFC\uFEB3)", PersianShaper.Shape("(\u0633\u0644\u0627\u0645)"));
        }

        [Test]
        public void LinesAreShapedSeparately()
        {
            Assert.AreEqual("\uFEFB\nOK", PersianShaper.Shape("\u0644\u0627\nOK"));
        }

        [Test]
        public void EnglishIsUntouched()
        {
            Assert.AreEqual("Level 2 (Easy)", PersianShaper.Shape("Level 2 (Easy)"));
        }

        [Test]
        public void PersianDigits()
        {
            Assert.AreEqual("\u06F1\u06F0\u06F5", PersianShaper.ToPersianDigits("105"));
        }
    }
}
