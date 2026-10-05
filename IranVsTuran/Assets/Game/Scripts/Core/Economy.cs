using System;
using IranVsTuran.Defs;

namespace IranVsTuran.Core
{
    /// <summary>
    /// The two persistent currencies: gold (earned in battle, spent on upgrades and training) and
    /// gems (rarer: hero unlocks, items, skins, tier skips). Every change is saved immediately.
    /// </summary>
    public static class Economy
    {
        /// <summary>Raised whenever gold, gems or items change, so currency bars can refresh.</summary>
        public static event Action Changed;

        public static int Gold { get { return SaveSystem.Data.gold; } }
        public static int Gems { get { return SaveSystem.Data.gems; } }

        public static void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            SaveSystem.Data.gold += amount;
            Commit();
        }

        public static void AddGems(int amount)
        {
            if (amount <= 0)
                return;
            SaveSystem.Data.gems += amount;
            Commit();
        }

        public static bool TrySpendGold(int amount)
        {
            if (amount < 0 || SaveSystem.Data.gold < amount)
                return false;
            SaveSystem.Data.gold -= amount;
            Commit();
            return true;
        }

        public static bool TrySpendGems(int amount)
        {
            if (amount < 0 || SaveSystem.Data.gems < amount)
                return false;
            SaveSystem.Data.gems -= amount;
            Commit();
            return true;
        }

        public static int ItemCount(string itemId)
        {
            return SaveSystem.Data.ItemCount(itemId);
        }

        public static void AddItem(string itemId, int amount)
        {
            SaveSystem.Data.AddItem(itemId, amount);
            Commit();
        }

        public static bool TryUseItem(string itemId)
        {
            if (SaveSystem.Data.ItemCount(itemId) <= 0)
                return false;
            SaveSystem.Data.AddItem(itemId, -1);
            Commit();
            return true;
        }

        public static bool BuyItem(ItemDef item)
        {
            if (!TrySpendGems(item.gemPrice))
                return false;
            AddItem(item.id, 1);
            return true;
        }

        public static bool BuyGold(GoldOffer offer)
        {
            if (!TrySpendGems(offer.gems))
                return false;
            AddGold(offer.gold);
            return true;
        }

        /// <summary>
        /// Gives a reward. A hero or skin the player already owns is converted to gems, so a
        /// reward is never wasted.
        /// </summary>
        public static void Grant(Reward reward)
        {
            var save = SaveSystem.Data;
            switch (reward.type)
            {
                case RewardType.Gold:
                    save.gold += reward.amount;
                    break;
                case RewardType.Gems:
                    save.gems += reward.amount;
                    break;
                case RewardType.Item:
                    save.AddItem(reward.id, reward.amount);
                    break;
                case RewardType.Hero:
                    if (save.OwnsHero(reward.id))
                        save.gems += DuplicateHeroGems;
                    else
                        save.AddHero(reward.id);
                    break;
                case RewardType.Skin:
                    if (save.skins.Contains(reward.id))
                        save.gems += DuplicateSkinGems;
                    else
                    {
                        save.skins.Add(reward.id);
                        var skin = HeroDefs.GetSkin(reward.id);
                        if (skin != null)
                            save.AddHero(skin.heroId);
                    }
                    break;
                case RewardType.PassXp:
                    BattlePass.AddXp(reward.amount);
                    break;
            }
            Commit();
        }

        public static void Grant(Reward[] rewards)
        {
            foreach (var reward in rewards)
                Grant(reward);
        }

        public const int DuplicateHeroGems = 150;
        public const int DuplicateSkinGems = 100;

        /// <summary>Saves and notifies listeners; call after changing currencies directly.</summary>
        public static void Commit()
        {
            SaveSystem.Save();
            if (Changed != null)
                Changed();
        }
    }
}
