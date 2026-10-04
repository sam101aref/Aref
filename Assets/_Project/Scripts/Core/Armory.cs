using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arash.Core
{
    public enum ArmoryCategory
    {
        Bow,
        Upgrade,
        SpecialArrow,
        Outfit,
    }

    /// <summary>Special arrows fired with a full farr meter (F-32).</summary>
    public enum SpecialArrow
    {
        None,
        Fire,
        Triple,
        Piercing,
        Simurgh,
        Haoma,
    }

    public class ArmoryItem
    {
        public string Id;
        public ArmoryCategory Category;
        /// <summary>Price in coins; for upgrades, the price of each level.</summary>
        public int[] Prices = { 0 };
        /// <summary>Level id that must be won before the item can be bought (or is given, if free).</summary>
        public string RequiresLevel;

        // Bows
        public float MaxSpeed = 28f;
        public float PreviewBonus;
        public float FarrBonus;

        // Upgrades: bonus per level
        public float PerLevel;

        public SpecialArrow Special;
        public Color Tunic;

        public string NameKey { get { return "armory." + Id + ".name"; } }
        public string DescriptionKey { get { return "armory." + Id + ".desc"; } }
        public int MaxLevel { get { return Category == ArmoryCategory.Upgrade ? Prices.Length : 1; } }
        public bool IsFree { get { return Category != ArmoryCategory.Upgrade && Prices[0] == 0; } }
    }

    /// <summary>Everything the player's equipment adds up to, applied at the start of a level.</summary>
    public struct Loadout
    {
        public float MaxSpeed;
        public float HealthMultiplier;
        public float PreviewBonus;
        public float FarrMultiplier;
        public Color Tunic;
        public List<SpecialArrow> Specials;
    }

    public enum PurchaseResult
    {
        Bought,
        AlreadyOwned,
        Locked,
        NotEnoughCoins,
        MaxedOut,
    }

    /// <summary>
    /// The armory (F-33, F-34): bows, upgrades, special arrows and outfits bought with coins.
    /// Items can require a story level to be won first; Arash's own bow is given by the story.
    /// </summary>
    public static class Armory
    {
        public const string DefaultBow = "bow.wood";
        public const string DefaultOutfit = "outfit.teal";
        public const string HealthUpgrade = "upgrade.health";
        public const string AimUpgrade = "upgrade.aim";
        public const string FarrUpgrade = "upgrade.farr";

        public static readonly List<ArmoryItem> Items = new List<ArmoryItem>
        {
            new ArmoryItem { Id = DefaultBow, Category = ArmoryCategory.Bow, MaxSpeed = 28f },
            new ArmoryItem { Id = "bow.horn", Category = ArmoryCategory.Bow, Prices = new[] { 300 }, RequiresLevel = "ch0_5", MaxSpeed = 30f, PreviewBonus = 0.05f },
            new ArmoryItem { Id = "bow.champion", Category = ArmoryCategory.Bow, Prices = new[] { 900 }, RequiresLevel = "ch2_8", MaxSpeed = 32f, PreviewBonus = 0.1f, FarrBonus = 0.1f },
            new ArmoryItem { Id = "bow.arash", Category = ArmoryCategory.Bow, RequiresLevel = "ch4_8", MaxSpeed = 34f, PreviewBonus = 0.15f, FarrBonus = 0.2f },

            new ArmoryItem { Id = HealthUpgrade, Category = ArmoryCategory.Upgrade, Prices = new[] { 100, 200, 350 }, PerLevel = 0.1f },
            new ArmoryItem { Id = AimUpgrade, Category = ArmoryCategory.Upgrade, Prices = new[] { 120, 240, 400 }, PerLevel = 0.08f },
            new ArmoryItem { Id = FarrUpgrade, Category = ArmoryCategory.Upgrade, Prices = new[] { 150, 300, 450 }, PerLevel = 0.15f },

            new ArmoryItem { Id = "special.haoma", Category = ArmoryCategory.SpecialArrow, Prices = new[] { 200 }, RequiresLevel = "ch0_5", Special = SpecialArrow.Haoma },
            new ArmoryItem { Id = "special.fire", Category = ArmoryCategory.SpecialArrow, Prices = new[] { 150 }, RequiresLevel = "ch1_1", Special = SpecialArrow.Fire },
            new ArmoryItem { Id = "special.triple", Category = ArmoryCategory.SpecialArrow, Prices = new[] { 250 }, RequiresLevel = "ch1_8", Special = SpecialArrow.Triple },
            new ArmoryItem { Id = "special.piercing", Category = ArmoryCategory.SpecialArrow, Prices = new[] { 300 }, RequiresLevel = "ch2_8", Special = SpecialArrow.Piercing },
            new ArmoryItem { Id = "special.simurgh", Category = ArmoryCategory.SpecialArrow, Prices = new[] { 400 }, RequiresLevel = "ch3_8", Special = SpecialArrow.Simurgh },

            new ArmoryItem { Id = DefaultOutfit, Category = ArmoryCategory.Outfit, Tunic = new Color(0.10f, 0.42f, 0.55f) },
            new ArmoryItem { Id = "outfit.lapis", Category = ArmoryCategory.Outfit, Prices = new[] { 150 }, Tunic = new Color(0.12f, 0.2f, 0.55f) },
            new ArmoryItem { Id = "outfit.saffron", Category = ArmoryCategory.Outfit, Prices = new[] { 200 }, Tunic = new Color(0.9f, 0.62f, 0.12f) },
            new ArmoryItem { Id = "outfit.crimson", Category = ArmoryCategory.Outfit, Prices = new[] { 200 }, Tunic = new Color(0.62f, 0.1f, 0.14f) },
            new ArmoryItem { Id = "outfit.royal", Category = ArmoryCategory.Outfit, Prices = new[] { 300 }, RequiresLevel = "ch4_8", Tunic = new Color(0.4f, 0.16f, 0.5f) },
        };

        public static ArmoryItem Find(string id)
        {
            return Items.FirstOrDefault(i => i.Id == id);
        }

        public static IEnumerable<ArmoryItem> InCategory(ArmoryCategory category)
        {
            return Items.Where(i => i.Category == category);
        }

        /// <summary>Its story requirement is met.</summary>
        public static bool IsAvailable(SaveData save, ArmoryItem item)
        {
            return string.IsNullOrEmpty(item.RequiresLevel) || save.IsCompleted(item.RequiresLevel);
        }

        public static bool IsOwned(SaveData save, ArmoryItem item)
        {
            if (item.Category == ArmoryCategory.Upgrade)
                return save.UpgradeLevel(item.Id) > 0;
            return (item.IsFree && IsAvailable(save, item)) || save.Owns(item.Id);
        }

        /// <summary>Price of the next purchase (next level for upgrades); -1 when nothing is left to buy.</summary>
        public static int NextPrice(SaveData save, ArmoryItem item)
        {
            if (item.Category == ArmoryCategory.Upgrade)
            {
                var level = save.UpgradeLevel(item.Id);
                return level < item.Prices.Length ? item.Prices[level] : -1;
            }
            return IsOwned(save, item) ? -1 : item.Prices[0];
        }

        public static PurchaseResult TryBuy(SaveData save, ArmoryItem item)
        {
            if (!IsAvailable(save, item))
                return PurchaseResult.Locked;
            var price = NextPrice(save, item);
            if (price < 0)
                return item.Category == ArmoryCategory.Upgrade ? PurchaseResult.MaxedOut : PurchaseResult.AlreadyOwned;
            if (save.coins < price)
                return PurchaseResult.NotEnoughCoins;

            save.coins -= price;
            if (item.Category == ArmoryCategory.Upgrade)
                save.SetUpgradeLevel(item.Id, save.UpgradeLevel(item.Id) + 1);
            else
                save.owned.Add(item.Id);
            return PurchaseResult.Bought;
        }

        /// <summary>Equips an owned bow or outfit, or selects an owned special arrow.</summary>
        public static bool Equip(SaveData save, ArmoryItem item)
        {
            if (!IsOwned(save, item))
                return false;
            switch (item.Category)
            {
                case ArmoryCategory.Bow: save.equippedBow = item.Id; return true;
                case ArmoryCategory.Outfit: save.equippedOutfit = item.Id; return true;
                case ArmoryCategory.SpecialArrow: save.selectedSpecial = item.Id; return true;
                default: return false;
            }
        }

        public static bool IsEquipped(SaveData save, ArmoryItem item)
        {
            return save.equippedBow == item.Id || save.equippedOutfit == item.Id || save.selectedSpecial == item.Id;
        }

        public static Loadout CurrentLoadout(SaveData save)
        {
            var bow = Find(save.equippedBow);
            if (bow == null || !IsOwned(save, bow))
                bow = Find(DefaultBow);
            var outfit = Find(save.equippedOutfit);
            if (outfit == null || !IsOwned(save, outfit))
                outfit = Find(DefaultOutfit);

            var specials = InCategory(ArmoryCategory.SpecialArrow).Where(i => IsOwned(save, i)).Select(i => i.Special).ToList();
            var selected = Find(save.selectedSpecial);
            if (selected != null && specials.Remove(selected.Special))
                specials.Insert(0, selected.Special); // the selected arrow comes first

            return new Loadout
            {
                MaxSpeed = bow.MaxSpeed,
                HealthMultiplier = 1f + save.UpgradeLevel(HealthUpgrade) * Find(HealthUpgrade).PerLevel,
                PreviewBonus = bow.PreviewBonus + save.UpgradeLevel(AimUpgrade) * Find(AimUpgrade).PerLevel,
                FarrMultiplier = 1f + bow.FarrBonus + save.UpgradeLevel(FarrUpgrade) * Find(FarrUpgrade).PerLevel,
                Tunic = outfit.Tunic,
                Specials = specials,
            };
        }
    }
}
