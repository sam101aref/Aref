using System;
using System.Collections.Generic;
using Arash.Core;
using Arash.Story;
using UnityEngine;

namespace Arash.Levels
{
    /// <summary>The ordered list of chapters and levels. Loaded from Resources/Levels/LevelCatalog.</summary>
    [CreateAssetMenu(menuName = "Arash/Level Catalog", fileName = "LevelCatalog")]
    public class LevelCatalog : ScriptableObject
    {
        public const string ResourcePath = "Levels/LevelCatalog";

        [Serializable]
        public class Chapter
        {
            public string titleKey;
            [Min(0), Tooltip("Total stars needed to open this chapter (GDD 4.8).")]
            public int starsToUnlock;
            [Tooltip("Played before the chapter's first level, the first time.")]
            public CutsceneDefinition introCutscene;
            public List<LevelDefinition> levels = new List<LevelDefinition>();
        }

        public List<Chapter> chapters = new List<Chapter>();
        [Tooltip("Played after the final level.")]
        public CutsceneDefinition endingCutscene;

        static LevelCatalog loaded;

        public static LevelCatalog Load()
        {
            if (loaded == null)
                loaded = Resources.Load<LevelCatalog>(ResourcePath);
            return loaded;
        }

        /// <summary>All levels in play order.</summary>
        public List<LevelDefinition> AllLevels()
        {
            var all = new List<LevelDefinition>();
            foreach (var chapter in chapters)
                foreach (var level in chapter.levels)
                    if (level != null)
                        all.Add(level);
            return all;
        }

        public LevelDefinition First()
        {
            var all = AllLevels();
            return all.Count > 0 ? all[0] : null;
        }

        public LevelDefinition Next(LevelDefinition level)
        {
            var all = AllLevels();
            var index = all.IndexOf(level);
            return index >= 0 && index + 1 < all.Count ? all[index + 1] : null;
        }

        /// <summary>All cutscenes in story order, for the story book.</summary>
        public List<CutsceneDefinition> AllCutscenes()
        {
            var all = new List<CutsceneDefinition>();
            foreach (var chapter in chapters)
            {
                if (chapter.introCutscene != null && !all.Contains(chapter.introCutscene))
                    all.Add(chapter.introCutscene);
                foreach (var level in chapter.levels)
                    if (level != null && level.outroCutscene != null && !all.Contains(level.outroCutscene))
                        all.Add(level.outroCutscene);
            }
            if (endingCutscene != null && !all.Contains(endingCutscene))
                all.Add(endingCutscene);
            return all;
        }

        public Chapter ChapterOf(LevelDefinition level)
        {
            return chapters.Find(c => c.levels.Contains(level));
        }

        public int ChapterIndexOf(LevelDefinition level)
        {
            return chapters.FindIndex(c => c.levels.Contains(level));
        }

        public bool IsUnlocked(LevelDefinition level, SaveData save)
        {
            if (!Monetization.IsChapterAccessible(ChapterIndexOf(level), save))
                return false;
            var all = AllLevels();
            var ids = all.ConvertAll(l => l.id);
            var chapter = ChapterOf(level);
            return ProgressRules.IsUnlocked(all.IndexOf(level), ids, chapter != null ? chapter.starsToUnlock : 0, save);
        }
    }
}
