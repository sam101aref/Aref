using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arash.Core
{
    public enum ItemCategory
    {
        Bow,
        Armor,
        Helmet,
        Shield,
        Quiver,
        Outfit,
        Slot,
    }

    public enum Currency
    {
        Coins,
        Gems,
    }

    /// <summary>What a bow's arrows do besides flying (F-55).</summary>
    public enum BowAbility
    {
        None,
        /// <summary>Burns the target and sets wooden cover alight.</summary>
        Fire,
        /// <summary>Three arrows at once.</summary>
        Triple,
        /// <summary>Passes through shields and several bodies.</summary>
        Piercing,
        /// <summary>Steers towards the nearest enemy.</summary>
        Simurgh,
        /// <summary>Each hit heals Arash a little.</summary>
        Haoma,
        /// <summary>Tishtrya's rain: the target draws and reloads slowly for a while.</summary>
        Frost,
        /// <summary>Lightning jumps to the next enemy.</summary>
        Thunder,
        /// <summary>Arash's own bow: a golden arrow, strong and quick.</summary>
        Golden,
    }

    /// <summary>One thing in the shop. Only the fields of its category matter.</summary>
    public class ShopItem
    {
        public string Id;
        public ItemCategory Category;
        public int Price;
        public Currency Currency = Currency.Coins;
        /// <summary>Level id that must be won before the item appears in the shop (or is given, if free).</summary>
        public string RequiresLevel;

        // Bows
        public float Damage = 1f;
        public float MaxSpeed = 28f;
        public float Reload = 0.75f;
        public int Ammo = 30;
        public float PreviewBonus;
        public BowAbility Ability;
        /// <summary>Colour of the arrow's trail; fully transparent for none (the starting bow).</summary>
        public Color Trail = Color.clear;

        // Armour and helmets: fraction of damage blocked
        public float ResistHead;
        public float ResistBody;
        public float ResistLimb;
        public float HealthBonus;

        // Shields
        public float Durability;

        // Quivers: extra arrows as a fraction of each bow's quiver
        public float AmmoBonus;

        // Outfits
        public Color Tunic;
        public Color Cape;
        public Color Cap;

        /// <summary>Sprite under Resources/Art for the item's look in battle (helmets, armour, shields, bows).</summary>
        public string Sprite;

        public string NameKey { get { return "item." + Id + ".name"; } }
        public string DescriptionKey { get { return "item." + Id + ".desc"; } }
        public bool IsFree { get { return Price <= 0; } }
    }

    /// <summary>A bow carried into battle, with its arrows for this level.</summary>
    public class CarriedBow
    {
        public ShopItem Item;
        public int Capacity;
    }

    /// <summary>Everything the player's equipment adds up to, applied at the start of a level.</summary>
    public class Loadout
    {
        public List<CarriedBow> Bows = new List<CarriedBow>();
        public float ResistHead;
        public float ResistBody;
        public float ResistLimb;
        public float HealthMultiplier = 1f;
        public ShopItem Shield;
        public ShopItem Helmet;
        public ShopItem Armor;
        public ShopItem Outfit;
    }

    public enum PurchaseResult
    {
        Bought,
        AlreadyOwned,
        Locked,
        NotEnoughCoins,
        NotEnoughGems,
    }

    /// <summary>
    /// The shop and equipment (F-55 to F-59). Everything opens as the story goes on: Arash starts
    /// with a plain wooden bow whose arrows leave no trail, one bow slot, and nothing else.
    /// Bows, armour, helmets, shields and quivers are bought with coins; a few rare items and the
    /// third bow slot cost gems. Outfits only change Arash's colours.
    /// </summary>
    public static class Armory
    {
        public const string DefaultBow = "bow.wood";
        public const string DefaultOutfit = "outfit.teal";
        public const string ArashBow = "bow.arash";
        public const int BaseSlots = 1;
        public const int MaxSlots = 3;

        static ShopItem Bow(string id, int price, string requires, float damage, float speed, float reload, int ammo,
            BowAbility ability, Color trail, float preview = 0f, Currency currency = Currency.Coins)
        {
            return new ShopItem
            {
                Id = id, Category = ItemCategory.Bow, Price = price, Currency = currency, RequiresLevel = requires,
                Damage = damage, MaxSpeed = speed, Reload = reload, Ammo = ammo, Ability = ability, Trail = trail,
                PreviewBonus = preview, Sprite = "Equipment/" + id.Replace('.', '_'),
            };
        }

        static ShopItem Outfit(string id, int price, string requires, Color tunic, Color cape, Color cap, Currency currency = Currency.Coins)
        {
            return new ShopItem { Id = id, Category = ItemCategory.Outfit, Price = price, Currency = currency, RequiresLevel = requires, Tunic = tunic, Cape = cape, Cap = cap };
        }

        static Color C(float r, float g, float b, float a = 1f)
        {
            return new Color(r, g, b, a);
        }

        public static readonly List<ShopItem> Items = new List<ShopItem>
        {
            Bow(DefaultBow, 0, null, 1f, 28f, 0.75f, 30, BowAbility.None, Color.clear),
            Bow("bow.horn", 200, "ch0_5", 1.1f, 30f, 0.65f, 30, BowAbility.None, C(1f, 0.95f, 0.8f, 0.55f), 0.05f),
            Bow("bow.fire", 350, "ch1_1", 1f, 28f, 0.8f, 15, BowAbility.Fire, C(1f, 0.5f, 0.1f)),
            Bow("bow.haoma", 400, "ch1_4", 0.9f, 28f, 0.7f, 12, BowAbility.Haoma, C(0.4f, 0.9f, 0.45f)),
            Bow("bow.triple", 600, "ch1_8", 0.8f, 28f, 0.9f, 12, BowAbility.Triple, C(0.55f, 0.75f, 1f)),
            Bow("bow.frost", 700, "ch2_4", 1f, 29f, 0.75f, 12, BowAbility.Frost, C(0.55f, 0.9f, 1f)),
            Bow("bow.piercing", 850, "ch2_8", 1.2f, 32f, 0.9f, 10, BowAbility.Piercing, C(0.75f, 0.45f, 1f), 0.05f),
            Bow("bow.simurgh", 1100, "ch3_8", 1f, 30f, 0.8f, 8, BowAbility.Simurgh, C(0.2f, 0.85f, 0.7f)),
            Bow("bow.thunder", 60, "ch4_4", 1f, 30f, 0.9f, 8, BowAbility.Thunder, C(1f, 0.95f, 0.4f), 0f, Currency.Gems),
            Bow(ArashBow, 0, "ch4_8", 1.3f, 34f, 0.5f, 30, BowAbility.Golden, C(1f, 0.82f, 0.3f), 0.15f),

            new ShopItem { Id = "armor.leather", Category = ItemCategory.Armor, Price = 250, RequiresLevel = "ch0_3", ResistBody = 0.15f, ResistLimb = 0.1f, HealthBonus = 0.1f, Sprite = "Equipment/armor_leather" },
            new ShopItem { Id = "armor.scale", Category = ItemCategory.Armor, Price = 750, RequiresLevel = "ch2_1", ResistBody = 0.3f, ResistLimb = 0.2f, HealthBonus = 0.2f, Sprite = "Equipment/armor_scale" },
            new ShopItem { Id = "armor.immortal", Category = ItemCategory.Armor, Price = 1500, RequiresLevel = "ch3_6", ResistBody = 0.45f, ResistLimb = 0.3f, HealthBonus = 0.35f, Sprite = "Equipment/armor_immortal" },

            new ShopItem { Id = "helmet.felt", Category = ItemCategory.Helmet, Price = 150, RequiresLevel = "ch0_4", ResistHead = 0.2f, Sprite = "Equipment/helmet_felt" },
            new ShopItem { Id = "helmet.iron", Category = ItemCategory.Helmet, Price = 600, RequiresLevel = "ch1_6", ResistHead = 0.4f, Sprite = "Equipment/helmet_iron" },
            new ShopItem { Id = "helmet.gold", Category = ItemCategory.Helmet, Price = 1200, RequiresLevel = "ch3_4", ResistHead = 0.6f, Sprite = "Equipment/helmet_gold" },

            new ShopItem { Id = "shield.wood", Category = ItemCategory.Shield, Price = 200, RequiresLevel = "ch1_2", Durability = 120f, Sprite = "pavise_wood" },
            new ShopItem { Id = "shield.bronze", Category = ItemCategory.Shield, Price = 700, RequiresLevel = "ch2_6", Durability = 260f, Sprite = "pavise_bronze" },
            new ShopItem { Id = "shield.simurgh", Category = ItemCategory.Shield, Price = 50, Currency = Currency.Gems, RequiresLevel = "ch3_8", Durability = 450f, Sprite = "pavise_simurgh" },

            new ShopItem { Id = "quiver.1", Category = ItemCategory.Quiver, Price = 250, RequiresLevel = "ch1_3", AmmoBonus = 0.2f },
            new ShopItem { Id = "quiver.2", Category = ItemCategory.Quiver, Price = 600, RequiresLevel = "ch2_3", AmmoBonus = 0.4f },
            new ShopItem { Id = "quiver.3", Category = ItemCategory.Quiver, Price = 1000, RequiresLevel = "ch3_3", AmmoBonus = 0.6f },

            new ShopItem { Id = "slot.2", Category = ItemCategory.Slot, Price = 500, RequiresLevel = "ch1_1" },
            new ShopItem { Id = "slot.3", Category = ItemCategory.Slot, Price = 40, Currency = Currency.Gems, RequiresLevel = "ch2_1" },

            Outfit(DefaultOutfit, 0, null, C(0.10f, 0.42f, 0.55f), C(0.66f, 0.15f, 0.17f), C(0.70f, 0.16f, 0.18f)),
            Outfit("outfit.lapis", 150, "ch0_2", C(0.14f, 0.22f, 0.56f), C(0.85f, 0.65f, 0.25f), C(0.85f, 0.65f, 0.25f)),
            Outfit("outfit.saffron", 200, "ch1_1", C(0.92f, 0.62f, 0.14f), C(0.45f, 0.12f, 0.12f), C(0.95f, 0.92f, 0.85f)),
            Outfit("outfit.crimson", 200, "ch1_5", C(0.64f, 0.11f, 0.14f), C(0.12f, 0.12f, 0.16f), C(0.92f, 0.75f, 0.30f)),
            Outfit("outfit.emerald", 300, "ch2_2", C(0.12f, 0.50f, 0.32f), C(0.90f, 0.85f, 0.70f), C(0.12f, 0.36f, 0.24f)),
            Outfit("outfit.night", 30, "ch2_6", C(0.16f, 0.16f, 0.22f), C(0.36f, 0.12f, 0.45f), C(0.20f, 0.20f, 0.26f), Currency.Gems),
            Outfit("outfit.royal", 40, "ch3_5", C(0.42f, 0.18f, 0.55f), C(0.92f, 0.74f, 0.30f), C(0.92f, 0.74f, 0.30f), Currency.Gems),
            Outfit("outfit.immortal", 60, "ch4_8", C(0.95f, 0.93f, 0.86f), C(0.92f, 0.74f, 0.30f), C(0.92f, 0.74f, 0.30f), Currency.Gems),
        };

        public static ShopItem Find(string id)
        {
            return string.IsNullOrEmpty(id) ? null : Items.FirstOrDefault(i => i.Id == id);
        }

        public static IEnumerable<ShopItem> InCategory(ItemCategory category)
        {
            return Items.Where(i => i.Category == category);
        }

        /// <summary>Its story requirement is met, so it shows in the shop.</summary>
        public static bool IsAvailable(SaveData save, ShopItem item)
        {
            return string.IsNullOrEmpty(item.RequiresLevel) || save.IsCompleted(item.RequiresLevel);
        }

        public static bool IsOwned(SaveData save, ShopItem item)
        {
            return (item.IsFree && IsAvailable(save, item)) || save.Owns(item.Id);
        }

        public static int Slots(SaveData save)
        {
            return BaseSlots + InCategory(ItemCategory.Slot).Count(i => IsOwned(save, i));
        }

        public static PurchaseResult TryBuy(SaveData save, ShopItem item)
        {
            if (!IsAvailable(save, item))
                return PurchaseResult.Locked;
            if (IsOwned(save, item))
                return PurchaseResult.AlreadyOwned;
            if (item.Currency == Currency.Gems)
            {
                if (save.gems < item.Price)
                    return PurchaseResult.NotEnoughGems;
                save.gems -= item.Price;
            }
            else
            {
                if (save.coins < item.Price)
                    return PurchaseResult.NotEnoughCoins;
                save.coins -= item.Price;
            }
            save.owned.Add(item.Id);
            AutoEquip(save, item);
            return PurchaseResult.Bought;
        }

        /// <summary>A new item is put to use straight away: into a free bow slot, or worn.</summary>
        static void AutoEquip(SaveData save, ShopItem item)
        {
            if (item.Category == ItemCategory.Bow)
            {
                if (!save.equippedBows.Contains(item.Id))
                {
                    if (save.equippedBows.Count < Slots(save))
                        save.equippedBows.Add(item.Id);
                    else
                        save.equippedBows[save.equippedBows.Count - 1] = item.Id;
                }
                return;
            }
            Equip(save, item);
        }

        /// <summary>
        /// Wears an owned item. Bows toggle in and out of the slots (at least one stays); other
        /// categories replace what was worn. Tapping a worn helmet, armour or shield takes it off.
        /// </summary>
        public static bool Equip(SaveData save, ShopItem item)
        {
            if (!IsOwned(save, item))
                return false;
            switch (item.Category)
            {
                case ItemCategory.Bow:
                    if (save.equippedBows.Contains(item.Id))
                    {
                        if (save.equippedBows.Count > 1)
                            save.equippedBows.Remove(item.Id);
                        return true;
                    }
                    if (save.equippedBows.Count >= Slots(save))
                        save.equippedBows.RemoveAt(save.equippedBows.Count - 1);
                    save.equippedBows.Add(item.Id);
                    return true;
                case ItemCategory.Armor:
                    save.equippedArmor = save.equippedArmor == item.Id ? null : item.Id;
                    return true;
                case ItemCategory.Helmet:
                    save.equippedHelmet = save.equippedHelmet == item.Id ? null : item.Id;
                    return true;
                case ItemCategory.Shield:
                    save.equippedShield = save.equippedShield == item.Id ? null : item.Id;
                    return true;
                case ItemCategory.Outfit:
                    save.equippedOutfit = item.Id;
                    return true;
                default:
                    return false; // quivers and slots work by being owned
            }
        }

        public static bool IsEquipped(SaveData save, ShopItem item)
        {
            switch (item.Category)
            {
                case ItemCategory.Bow: return save.equippedBows.Contains(item.Id);
                case ItemCategory.Armor: return save.equippedArmor == item.Id;
                case ItemCategory.Helmet: return save.equippedHelmet == item.Id;
                case ItemCategory.Shield: return save.equippedShield == item.Id;
                case ItemCategory.Outfit: return save.equippedOutfit == item.Id;
                default: return IsOwned(save, item);
            }
        }

        /// <summary>Story rewards: items the story hands over for free are added when available.</summary>
        public static void GrantStoryItems(SaveData save)
        {
            foreach (var item in Items)
                if (item.IsFree && item.Category == ItemCategory.Bow && item.Id != DefaultBow &&
                    IsAvailable(save, item) && !save.Owns(item.Id))
                {
                    save.owned.Add(item.Id);
                    AutoEquip(save, item);
                }
        }

        static ShopItem Owned(SaveData save, string id, ItemCategory category)
        {
            var item = Find(id);
            return item != null && item.Category == category && IsOwned(save, item) ? item : null;
        }

        public static Loadout CurrentLoadout(SaveData save)
        {
            var loadout = new Loadout();
            var quiverBonus = InCategory(ItemCategory.Quiver).Where(i => IsOwned(save, i)).Select(i => i.AmmoBonus).DefaultIfEmpty(0f).Max();

            foreach (var id in save.equippedBows.Take(Slots(save)))
            {
                var bow = Owned(save, id, ItemCategory.Bow);
                if (bow != null && loadout.Bows.All(b => b.Item != bow))
                    loadout.Bows.Add(new CarriedBow { Item = bow, Capacity = Mathf.RoundToInt(bow.Ammo * (1f + quiverBonus)) });
            }
            if (loadout.Bows.Count == 0)
            {
                var wood = Find(DefaultBow);
                loadout.Bows.Add(new CarriedBow { Item = wood, Capacity = Mathf.RoundToInt(wood.Ammo * (1f + quiverBonus)) });
            }

            loadout.Armor = Owned(save, save.equippedArmor, ItemCategory.Armor);
            loadout.Helmet = Owned(save, save.equippedHelmet, ItemCategory.Helmet);
            loadout.Shield = Owned(save, save.equippedShield, ItemCategory.Shield);
            loadout.Outfit = Owned(save, save.equippedOutfit, ItemCategory.Outfit) ?? Find(DefaultOutfit);

            if (loadout.Armor != null)
            {
                loadout.ResistBody = loadout.Armor.ResistBody;
                loadout.ResistLimb = loadout.Armor.ResistLimb;
                loadout.HealthMultiplier = 1f + loadout.Armor.HealthBonus;
            }
            if (loadout.Helmet != null)
                loadout.ResistHead = loadout.Helmet.ResistHead;
            return loadout;
        }

        /// <summary>
        /// Moves a version 1 save (old armory) to the new shop: bows and special arrows become the
        /// matching bows, everything without a match is refunded in coins.
        /// </summary>
        public static void MigrateFromVersion1(SaveData save)
        {
            var renamed = new Dictionary<string, string>
            {
                { "special.fire", "bow.fire" },
                { "special.triple", "bow.triple" },
                { "special.piercing", "bow.piercing" },
                { "special.simurgh", "bow.simurgh" },
                { "special.haoma", "bow.haoma" },
            };
            var refunds = new Dictionary<string, int> { { "bow.champion", 900 } };
            var upgradePrices = new Dictionary<string, int[]>
            {
                { "upgrade.health", new[] { 100, 200, 350 } },
                { "upgrade.aim", new[] { 120, 240, 400 } },
                { "upgrade.farr", new[] { 150, 300, 450 } },
            };

            var owned = new List<string>();
            foreach (var id in save.owned)
            {
                string newId;
                if (renamed.TryGetValue(id, out newId))
                    owned.Add(newId);
                else if (Find(id) != null)
                    owned.Add(id);
                else if (refunds.ContainsKey(id))
                    save.coins += refunds[id];
            }
            save.owned = owned.Distinct().ToList();

            foreach (var record in save.upgrades)
            {
                int[] prices;
                if (upgradePrices.TryGetValue(record.id, out prices))
                    for (var i = 0; i < record.level && i < prices.Length; i++)
                        save.coins += prices[i];
            }
            save.upgrades.Clear();

            save.equippedBows = new List<string>();
            var oldBow = Find(save.equippedBow);
            save.equippedBows.Add(oldBow != null && oldBow.Category == ItemCategory.Bow && IsOwned(save, oldBow) ? oldBow.Id : DefaultBow);
            if (Find(save.equippedOutfit) == null)
                save.equippedOutfit = DefaultOutfit;
            save.equippedBow = null;
            save.selectedSpecial = null;
        }
    }
}
