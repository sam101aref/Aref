using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum HeroAbility
    {
        /// <summary>Rostam's mace: damages and stuns everything around him.</summary>
        Stomp,
        /// <summary>Gordafarid's volley: arrows at every enemy in range.</summary>
        Volley,
        /// <summary>Zal burns a Simorgh feather: heals the hero and nearby soldiers, hurts enemies.</summary>
        Feather,
        /// <summary>Esfandiar the brazen-bodied: invulnerable and spinning for a few seconds.</summary>
        Brazen,
    }

    public class HeroDef
    {
        public string id;
        public float hp;
        public float armor;
        public float damageMin;
        public float damageMax;
        public float interval = 1f;
        /// <summary>0 for melee heroes.</summary>
        public float range;
        public DamageType damageType = DamageType.Physical;
        public float speed = 1.6f;
        public float respawn = 15f;
        public HeroAbility ability;
        public float abilityCooldown = 12f;
        public float abilityValue;
        public float abilityRadius = 1.6f;
        /// <summary>0 = cannot be bought (free or story reward).</summary>
        public int gemPrice;
        /// <summary>Winning this level grants the hero for free.</summary>
        public string unlockLevel = string.Empty;
        public Look look;

        public string NameKey { get { return "hero." + id; } }
        public string DescKey { get { return "hero." + id + ".desc"; } }
        public string AbilityKey { get { return "hero." + id + ".ability"; } }
    }

    public class SkinDef
    {
        public string id;
        public string heroId;
        public uint main;
        public uint accent;
        public Headgear head;
        public int gemPrice;
        public string NameKey { get { return "skin." + id; } }
    }

    public class ItemDef
    {
        public string id;
        public int gemPrice;
        /// <summary>Needs a spot on the map (the naphtha jar); the others act at once.</summary>
        public bool targeted;
        public string NameKey { get { return "item." + id; } }
        public string DescKey { get { return "item." + id + ".desc"; } }
    }

    public class UpgradeDef
    {
        public string id;
        public float perRank;
        public int[] costs;
        public int MaxRank { get { return costs.Length; } }
        public string NameKey { get { return "upgrade." + id; } }
        public string DescKey { get { return "upgrade." + id + ".desc"; } }
    }

    public static class HeroDefs
    {
        public const int MaxLevel = 10;

        public static readonly List<HeroDef> All = new List<HeroDef>
        {
            new HeroDef
            {
                id = HeroIds.Rostam, hp = 420, armor = 0.4f, damageMin = 16, damageMax = 28, interval = 1.1f, speed = 1.5f, respawn = 15f,
                ability = HeroAbility.Stomp, abilityCooldown = 12f, abilityValue = 60f, abilityRadius = 1.7f,
                look = new Look { main = 0x8B5A2B, accent = 0xC9A227, skin = 0xD9A57A, head = Headgear.DivHelm, weapon = Weapon.Mace, shield = true, cape = 0xB0302A, beard = 0x2A1A10, scale = 1.35f },
            },
            new HeroDef
            {
                id = HeroIds.Gordafarid, hp = 260, armor = 0.2f, damageMin = 14, damageMax = 22, interval = 0.8f, range = 3.2f, speed = 1.8f, respawn = 12f,
                ability = HeroAbility.Volley, abilityCooldown = 10f, abilityValue = 30f, abilityRadius = 3.2f, gemPrice = 300, unlockLevel = "l7",
                look = new Look { main = 0x2E5A8B, accent = 0xC9A227, skin = 0xE6B98E, head = Headgear.Helmet, weapon = Weapon.Bow, cape = 0xE8E8E8, hair = 0x3A2418, scale = 1.25f },
            },
            new HeroDef
            {
                id = HeroIds.Zal, hp = 300, armor = 0.1f, damageMin = 22, damageMax = 36, interval = 1.3f, range = 2.6f, damageType = DamageType.Magic,
                speed = 1.5f, respawn = 14f, ability = HeroAbility.Feather, abilityCooldown = 14f, abilityValue = 120f, abilityRadius = 2.2f, gemPrice = 450,
                look = new Look { main = 0xEFEFEF, accent = 0x3E8B7A, skin = 0xE6B98E, weapon = Weapon.Staff, cape = 0x3E8B7A, hair = 0xF4F4F4, beard = 0xF4F4F4, scale = 1.3f },
            },
            new HeroDef
            {
                id = HeroIds.Esfandiar, hp = 520, armor = 0.55f, damageMin = 20, damageMax = 32, interval = 1f, speed = 1.6f, respawn = 16f,
                ability = HeroAbility.Brazen, abilityCooldown = 15f, abilityValue = 4f, abilityRadius = 1.5f, gemPrice = 650,
                look = new Look { main = 0xB08D57, accent = 0x6A6A6A, skin = 0xE0B48A, head = Headgear.SpikeHelm, weapon = Weapon.Spear, shield = true, cape = 0x1E4A7A, beard = 0x3A2418, scale = 1.35f },
            },
        };

        public static readonly List<SkinDef> Skins = new List<SkinDef>
        {
            // Rostam's legendary leopard-skin coat, the babr-e bayan: the battle pass's top reward.
            new SkinDef { id = "rostam_babr", heroId = HeroIds.Rostam, main = 0xC8923A, accent = 0x3A2A1A, head = Headgear.Tiger },
            new SkinDef { id = "rostam_royal", heroId = HeroIds.Rostam, main = 0x1E4A7A, accent = 0xE8C35A, head = Headgear.Plume, gemPrice = 200 },
            new SkinDef { id = "gordafarid_white", heroId = HeroIds.Gordafarid, main = 0xE8E8E8, accent = 0x2E5A8B, head = Headgear.Helmet, gemPrice = 150 },
            new SkinDef { id = "esfandiar_gold", heroId = HeroIds.Esfandiar, main = 0xE8C35A, accent = 0x8E2B25, head = Headgear.Crown },
        };

        public static HeroDef Get(string id)
        {
            return All.Find(h => h.id == id);
        }

        public static SkinDef GetSkin(string id)
        {
            return Skins.Find(s => s.id == id);
        }

        /// <summary>Gold to train a hero from <paramref name="level"/> to the next level.</summary>
        public static int TrainCost(int level)
        {
            return 200 * level;
        }

        /// <summary>Hit point and damage multiplier at a hero level.</summary>
        public static float LevelMultiplier(int level)
        {
            return 1f + 0.12f * (level - 1);
        }

        /// <summary>The hero's look with a skin applied.</summary>
        public static Look LookFor(HeroDef hero, string skinId)
        {
            var skin = string.IsNullOrEmpty(skinId) ? null : GetSkin(skinId);
            if (skin == null || skin.heroId != hero.id)
                return hero.look;
            var look = hero.look.Clone();
            look.main = skin.main;
            look.accent = skin.accent;
            look.head = skin.head;
            if (skin.id == "rostam_babr")
                look.cape = 0x3A2A1A;
            return look;
        }
    }

    public static class ItemDefs
    {
        public static readonly List<ItemDef> All = new List<ItemDef>
        {
            new ItemDef { id = ItemIds.Nushdaru, gemPrice = 30 },
            new ItemDef { id = ItemIds.Simorgh, gemPrice = 40 },
            new ItemDef { id = ItemIds.Naphtha, gemPrice = 25, targeted = true },
            new ItemDef { id = ItemIds.Treasure, gemPrice = 20 },
        };

        public const int NushdaruLives = 5;
        public const float SimorghFreeze = 5f;
        public const float NaphthaDamage = 350f;
        public const float NaphthaRadius = 1.6f;
        public const int TreasureCoins = 250;

        public static ItemDef Get(string id)
        {
            return All.Find(i => i.id == id);
        }
    }

    /// <summary>Permanent upgrades bought with gold in the Armory.</summary>
    public static class UpgradeDefs
    {
        public const string ArcherDamage = "archer_dmg";
        public const string ArcherRange = "archer_range";
        public const string BarracksHp = "barracks_hp";
        public const string BarracksRespawn = "barracks_respawn";
        public const string MageDamage = "mage_dmg";
        public const string MageSpeed = "mage_speed";
        public const string ArtilleryDamage = "artillery_dmg";
        public const string ArtillerySplash = "artillery_splash";
        public const string SpellArrows = "spell_arrows";
        public const string SpellReinforce = "spell_reinforce";
        public const string Economy = "economy";

        static readonly int[] Three = { 250, 600, 1200 };
        static readonly int[] Two = { 400, 1000 };

        public static readonly List<UpgradeDef> All = new List<UpgradeDef>
        {
            new UpgradeDef { id = ArcherDamage, perRank = 0.10f, costs = Three },
            new UpgradeDef { id = ArcherRange, perRank = 0.06f, costs = Two },
            new UpgradeDef { id = BarracksHp, perRank = 0.12f, costs = Three },
            new UpgradeDef { id = BarracksRespawn, perRank = 0.12f, costs = Two },
            new UpgradeDef { id = MageDamage, perRank = 0.10f, costs = Three },
            new UpgradeDef { id = MageSpeed, perRank = 0.06f, costs = Two },
            new UpgradeDef { id = ArtilleryDamage, perRank = 0.10f, costs = Three },
            new UpgradeDef { id = ArtillerySplash, perRank = 0.08f, costs = Two },
            new UpgradeDef { id = SpellArrows, perRank = 0.20f, costs = Three },
            new UpgradeDef { id = SpellReinforce, perRank = 0.20f, costs = Three },
            new UpgradeDef { id = Economy, perRank = 20f, costs = Three },
        };

        public static UpgradeDef Get(string id)
        {
            return All.Find(u => u.id == id);
        }
    }

    /// <summary>The two spells every battle has: Arash's rain of arrows and Kaveh's blacksmiths.</summary>
    public static class SpellDefs
    {
        public const float ArrowsCooldown = 45f;
        public const float ArrowsRadius = 1.4f;
        public const int ArrowsCount = 12;
        public const float ArrowsDamageMin = 18f;
        public const float ArrowsDamageMax = 32f;

        public const float ReinforceCooldown = 18f;
        public const float ReinforceDuration = 18f;
        public const float ReinforceHp = 80f;
        public const float ReinforceMin = 3f;
        public const float ReinforceMax = 6f;
    }
}
