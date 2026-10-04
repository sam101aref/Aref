using System.Collections.Generic;
using Arash.Core;

namespace Arash.Levels
{
    /// <summary>Star rating and unlock rules (F-12, F-13), kept free of Unity objects for testing.</summary>
    public static class ProgressRules
    {
        /// <summary>
        /// 0 stars for a loss. A win earns one star, plus one for finishing with enough health,
        /// plus one for winning with few arrows.
        /// </summary>
        public static int Stars(bool won, float healthFraction, int arrowsUsed, StarRules rules)
        {
            if (!won)
                return 0;
            var stars = 1;
            if (healthFraction >= rules.healthForSecondStar)
                stars++;
            if (arrowsUsed <= rules.maxArrowsForThirdStar)
                stars++;
            return stars;
        }

        /// <summary>
        /// A level is open when its chapter's star requirement is met and it is the first level,
        /// or the level before it (in play order) has been won.
        /// </summary>
        public static bool IsUnlocked(int index, IList<string> orderedLevelIds, int chapterStarsRequired, SaveData save)
        {
            if (index < 0 || index >= orderedLevelIds.Count)
                return false;
            if (save.TotalStars < chapterStarsRequired)
                return false;
            return index == 0 || save.IsCompleted(orderedLevelIds[index - 1]);
        }
    }
}
