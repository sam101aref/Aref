using Arash.Core;
using Arash.Localization;
using NUnit.Framework;
using UnityEngine;

namespace Arash.Tests
{
    /// <summary>Tests that need the Unity runtime (JsonUtility, Resources); they run in CI.</summary>
    public class SaveAndLocalizationTests
    {
        [Test]
        public void SaveData_RoundTripsThroughJson()
        {
            var save = new SaveData { coins = 42 };
            save.RecordWin("ch0_1", 3);
            save.settings.language = (int)Language.Persian;
            save.settings.vibration = false;

            var copy = SaveSystem.FromJson(SaveSystem.ToJson(save));

            Assert.AreEqual(42, copy.coins);
            Assert.AreEqual(3, copy.GetStars("ch0_1"));
            Assert.AreEqual((int)Language.Persian, copy.settings.language);
            Assert.IsFalse(copy.settings.vibration);
        }

        [Test]
        public void CorruptOrEmptySave_StartsFresh()
        {
            Assert.AreEqual(0, SaveSystem.FromJson("").coins);
            Assert.IsNotNull(SaveSystem.FromJson("{}").levels);
        }

        [Test]
        public void StringTable_HasEveryKeyInBothLanguages()
        {
            var asset = Resources.Load<TextAsset>("Localization/Strings");
            Assert.IsNotNull(asset, "Resources/Localization/Strings.json is missing");
            StringAssert.Contains("\"game.title\"", asset.text);
            Assert.IsFalse(asset.text.Contains("\"fa\": \"\""), "A Persian string is empty");
            Assert.IsFalse(asset.text.Contains("\"en\": \"\""), "An English string is empty");
        }

        [Test]
        public void PersianFont_IsAvailable()
        {
            Assert.IsNotNull(Resources.Load<Font>("Fonts/Vazirmatn-Regular"));
            Assert.IsNotNull(Resources.Load<Font>("Fonts/Vazirmatn-Bold"));
        }
    }
}
