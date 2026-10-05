using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum TowerFamily
    {
        Archer,
        Barracks,
        Mage,
        Artillery,
    }

    public enum ProjectileKind
    {
        Arrow,
        Fire,
        Light,
        Boulder,
        Naphtha,
    }

    public enum TowerSpecial
    {
        None,
        /// <summary>Chance (<c>specialChance</c>) of a shot dealing <c>specialValue</c>× damage.</summary>
        Crit,
        /// <summary>Fires at two targets at once.</summary>
        DoubleShot,
        /// <summary>Sets the target on fire: <c>specialValue</c> damage per second for 3 s.</summary>
        Burn,
        /// <summary>Slows by <c>specialValue</c> for 2 s and jumps to two more enemies.</summary>
        SlowChain,
        /// <summary>Stuns ground enemies hit for <c>specialValue</c> seconds.</summary>
        Stun,
        /// <summary>Leaves burning ground: <c>specialValue</c> damage per second for 3 s.</summary>
        BurningGround,
        /// <summary>Soldiers regenerate <c>specialValue</c> hit points per second.</summary>
        Regen,
    }

    public class TowerDef
    {
        public string id;
        public TowerFamily family;
        /// <summary>1–3, then 4 for the two specialisations.</summary>
        public int tier;
        /// <summary>Price to build (tier 1) or to upgrade into this tower.</summary>
        public int cost;
        public float range;
        public float damageMin;
        public float damageMax;
        /// <summary>Seconds between attacks.</summary>
        public float interval = 1f;
        public DamageType damageType = DamageType.Physical;
        public float splash;
        public ProjectileKind projectile;
        public TowerSpecial special;
        public float specialValue;
        public float specialChance;

        // Barracks
        public int soldiers;
        public float soldierHp;
        public float soldierMin;
        public float soldierMax;
        public float soldierArmor;
        public float respawn = 10f;

        /// <summary>Towers this one can be upgraded into (one for tiers 1–2, two choices at tier 3).</summary>
        public string[] upgrades = new string[0];

        public string NameKey { get { return "tower." + id; } }
        /// <summary>Specialisations have their own description; tiers 1–3 share their family's.</summary>
        public string DescKey { get { return tier >= 4 ? "tower." + id + ".desc" : "family." + family.ToString().ToLowerInvariant(); } }
        public bool CanHitFlying { get { return family == TowerFamily.Archer || family == TowerFamily.Mage; } }
    }

    /// <summary>
    /// Four tower families, three tiers each, and two specialisations per family at tier 4 —
    /// the Kingdom Rush formula, dressed in the Shahnameh: archers of Arash, Zabuli champions,
    /// fire temples and siege engines.
    /// </summary>
    public static class TowerDefs
    {
        public const float SellRefund = 0.6f;

        public static readonly List<TowerDef> All = new List<TowerDef>
        {
            // ---- archers ----
            new TowerDef { id = "archer1", family = TowerFamily.Archer, tier = 1, cost = 70, range = 2.8f, damageMin = 4, damageMax = 6, interval = 0.8f, upgrades = new[] { "archer2" } },
            new TowerDef { id = "archer2", family = TowerFamily.Archer, tier = 2, cost = 110, range = 3.0f, damageMin = 7, damageMax = 11, interval = 0.7f, upgrades = new[] { "archer3" } },
            new TowerDef { id = "archer3", family = TowerFamily.Archer, tier = 3, cost = 160, range = 3.2f, damageMin = 10, damageMax = 16, interval = 0.6f, upgrades = new[] { "archer4a", "archer4b" } },
            new TowerDef
            {
                id = "archer4a", family = TowerFamily.Archer, tier = 4, cost = 250, range = 4.2f, damageMin = 35, damageMax = 55, interval = 1.4f,
                special = TowerSpecial.Crit, specialChance = 0.2f, specialValue = 3f,
            },
            new TowerDef
            {
                id = "archer4b", family = TowerFamily.Archer, tier = 4, cost = 230, range = 3.3f, damageMin = 11, damageMax = 16, interval = 0.4f,
                special = TowerSpecial.DoubleShot,
            },

            // ---- barracks ----
            new TowerDef
            {
                id = "barracks1", family = TowerFamily.Barracks, tier = 1, cost = 70, range = 2.2f,
                soldiers = 3, soldierHp = 50, soldierMin = 1, soldierMax = 3, upgrades = new[] { "barracks2" },
            },
            new TowerDef
            {
                id = "barracks2", family = TowerFamily.Barracks, tier = 2, cost = 110, range = 2.3f,
                soldiers = 3, soldierHp = 100, soldierMin = 3, soldierMax = 5, soldierArmor = 0.15f, upgrades = new[] { "barracks3" },
            },
            new TowerDef
            {
                id = "barracks3", family = TowerFamily.Barracks, tier = 3, cost = 160, range = 2.4f,
                soldiers = 3, soldierHp = 160, soldierMin = 6, soldierMax = 10, soldierArmor = 0.3f, upgrades = new[] { "barracks4a", "barracks4b" },
            },
            new TowerDef
            {
                id = "barracks4a", family = TowerFamily.Barracks, tier = 4, cost = 240, range = 2.5f,
                soldiers = 3, soldierHp = 320, soldierMin = 15, soldierMax = 24, soldierArmor = 0.5f, respawn = 12f,
                special = TowerSpecial.Regen, specialValue = 8f,
            },
            new TowerDef
            {
                id = "barracks4b", family = TowerFamily.Barracks, tier = 4, cost = 250, range = 2.6f,
                soldiers = 4, soldierHp = 230, soldierMin = 12, soldierMax = 18, soldierArmor = 0.35f, respawn = 9f,
            },

            // ---- fire temples ----
            new TowerDef { id = "mage1", family = TowerFamily.Mage, tier = 1, cost = 100, range = 2.6f, damageMin = 9, damageMax = 17, interval = 1.5f, damageType = DamageType.Magic, projectile = ProjectileKind.Fire, upgrades = new[] { "mage2" } },
            new TowerDef { id = "mage2", family = TowerFamily.Mage, tier = 2, cost = 160, range = 2.8f, damageMin = 23, damageMax = 43, interval = 1.5f, damageType = DamageType.Magic, projectile = ProjectileKind.Fire, upgrades = new[] { "mage3" } },
            new TowerDef { id = "mage3", family = TowerFamily.Mage, tier = 3, cost = 240, range = 3.0f, damageMin = 40, damageMax = 74, interval = 1.5f, damageType = DamageType.Magic, projectile = ProjectileKind.Fire, upgrades = new[] { "mage4a", "mage4b" } },
            new TowerDef
            {
                id = "mage4a", family = TowerFamily.Mage, tier = 4, cost = 300, range = 3.1f, damageMin = 76, damageMax = 140, interval = 1.6f,
                damageType = DamageType.Magic, projectile = ProjectileKind.Fire, special = TowerSpecial.Burn, specialValue = 20f,
            },
            new TowerDef
            {
                id = "mage4b", family = TowerFamily.Mage, tier = 4, cost = 280, range = 3.2f, damageMin = 50, damageMax = 90, interval = 1.3f,
                damageType = DamageType.Magic, projectile = ProjectileKind.Light, special = TowerSpecial.SlowChain, specialValue = 0.4f,
            },

            // ---- siege engines ----
            new TowerDef { id = "artillery1", family = TowerFamily.Artillery, tier = 1, cost = 125, range = 2.8f, damageMin = 8, damageMax = 15, interval = 3f, splash = 1.0f, projectile = ProjectileKind.Boulder, upgrades = new[] { "artillery2" } },
            new TowerDef { id = "artillery2", family = TowerFamily.Artillery, tier = 2, cost = 180, range = 2.9f, damageMin = 18, damageMax = 32, interval = 3f, splash = 1.05f, projectile = ProjectileKind.Boulder, upgrades = new[] { "artillery3" } },
            new TowerDef { id = "artillery3", family = TowerFamily.Artillery, tier = 3, cost = 260, range = 3.0f, damageMin = 30, damageMax = 50, interval = 3f, splash = 1.1f, projectile = ProjectileKind.Boulder, upgrades = new[] { "artillery4a", "artillery4b" } },
            new TowerDef
            {
                id = "artillery4a", family = TowerFamily.Artillery, tier = 4, cost = 320, range = 3.2f, damageMin = 60, damageMax = 100, interval = 3.2f, splash = 1.35f,
                projectile = ProjectileKind.Boulder, special = TowerSpecial.Stun, specialValue = 1.2f,
            },
            new TowerDef
            {
                id = "artillery4b", family = TowerFamily.Artillery, tier = 4, cost = 300, range = 3.0f, damageMin = 35, damageMax = 55, interval = 2.6f, splash = 1.2f,
                projectile = ProjectileKind.Naphtha, special = TowerSpecial.BurningGround, specialValue = 18f,
            },
        };

        /// <summary>The tier-1 tower of each family, in build-menu order.</summary>
        public static readonly string[] Buildable = { "archer1", "barracks1", "mage1", "artillery1" };

        static Dictionary<string, TowerDef> byId;

        public static TowerDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, TowerDef>();
                foreach (var def in All)
                    byId[def.id] = def;
            }
            TowerDef result;
            return byId.TryGetValue(id, out result) ? result : null;
        }
    }
}
