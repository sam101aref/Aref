using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum EnemyAbility
    {
        /// <summary>Heals wounded allies nearby by <c>value</c> hit points.</summary>
        HealAllies,
        /// <summary>Calls <c>count</c> × <c>summonId</c> next to itself.</summary>
        Summon,
        /// <summary>Burns soldiers and the hero around it for <c>value</c> damage.</summary>
        FireBreath,
        /// <summary>Stuns soldiers and the hero around it for <c>value</c> seconds.</summary>
        Stomp,
        /// <summary>Silences the nearest tower for <c>value</c> seconds.</summary>
        DisableTower,
        /// <summary>Below half health, moves and attacks <c>value</c> times faster (once).</summary>
        Enrage,
        /// <summary>Jumps <c>value</c> units forward along its path.</summary>
        Teleport,
        /// <summary>Regains <c>value</c> hit points every second (cooldown ignored).</summary>
        Regenerate,
        /// <summary>On death releases <c>count</c> × <c>summonId</c>.</summary>
        SpawnOnDeath,
    }

    public class AbilitySpec
    {
        public EnemyAbility type;
        public float cooldown = 8f;
        public float value;
        public float radius = 1.6f;
        public string summonId;
        public int count;
    }

    public class EnemyDef
    {
        public string id;
        public float hp;
        /// <summary>World units per second.</summary>
        public float speed = 0.9f;
        /// <summary>Fraction of physical damage blocked (0–0.9).</summary>
        public float armor;
        /// <summary>Fraction of magic damage blocked (0–0.9).</summary>
        public float magicResist;
        public int bounty;
        /// <summary>Lives lost when it gets through.</summary>
        public int lives = 1;
        public float meleeMin = 1f;
        public float meleeMax = 3f;
        public float attackInterval = 1f;
        /// <summary>Ranged attackers shoot soldiers and the hero from this distance (0 = melee only).</summary>
        public float rangedRange;
        public float rangedMin;
        public float rangedMax;
        public float rangedInterval = 1.4f;
        public bool flying;
        public bool boss;
        public AbilitySpec[] abilities = new AbilitySpec[0];
        public Look look = new Look();

        public string NameKey { get { return "enemy." + id; } }
        public string LoreKey { get { return "lore." + id; } }
    }

    /// <summary>
    /// The forces of Turan and the demons (divs) of Mazandaran, from Ferdowsi's Shahnameh.
    /// Hit points and speeds are tuned against the tower table in <see cref="TowerDefs"/>.
    /// </summary>
    public static class EnemyDefs
    {
        public static readonly List<EnemyDef> All = new List<EnemyDef>
        {
            // ---- Turan ----
            new EnemyDef
            {
                id = "soldier", hp = 40, speed = 0.9f, bounty = 6, meleeMin = 1, meleeMax = 4,
                look = new Look { main = 0x8E2B25, accent = 0x3B3B3B, head = Headgear.SpikeHelm, weapon = Weapon.Sword, shield = true },
            },
            new EnemyDef
            {
                id = "raider", hp = 32, speed = 1.6f, bounty = 7, meleeMin = 1, meleeMax = 3,
                look = new Look { main = 0xA0522D, accent = 0x5A3A22, head = Headgear.Cap, weapon = Weapon.Spear, mounted = true },
            },
            new EnemyDef
            {
                id = "archer", hp = 35, speed = 0.9f, bounty = 8, meleeMin = 1, meleeMax = 2,
                rangedRange = 2.2f, rangedMin = 3, rangedMax = 6, rangedInterval = 1.3f,
                look = new Look { main = 0x6B3A2A, accent = 0x2F2F2F, head = Headgear.Hood, weapon = Weapon.Bow },
            },
            new EnemyDef
            {
                id = "heavy", hp = 150, speed = 0.65f, armor = 0.6f, bounty = 16, meleeMin = 4, meleeMax = 8,
                look = new Look { main = 0x4A4A52, accent = 0x8E2B25, head = Headgear.Helmet, weapon = Weapon.Mace, shield = true, scale = 1.1f },
            },
            new EnemyDef
            {
                id = "cavalry", hp = 190, speed = 1.35f, armor = 0.3f, bounty = 20, meleeMin = 6, meleeMax = 10,
                look = new Look { main = 0x7A1F1F, accent = 0x2B2B2B, head = Headgear.Helmet, weapon = Weapon.Spear, mounted = true, shield = true },
            },
            new EnemyDef
            {
                id = "shaman", hp = 120, speed = 0.8f, magicResist = 0.6f, bounty = 22, meleeMin = 2, meleeMax = 5,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.HealAllies, cooldown = 4f, value = 40f, radius = 1.8f } },
                look = new Look { main = 0x3E2C5A, accent = 0xC9A227, head = Headgear.Hood, weapon = Weapon.Staff },
            },
            new EnemyDef
            {
                id = "elephant", hp = 1150, speed = 0.5f, armor = 0.35f, bounty = 85, lives = 3, meleeMin = 20, meleeMax = 40, attackInterval = 1.6f,
                look = new Look { body = Body.Elephant, main = 0x8A8A8A, accent = 0x8E2B25, scale = 1.5f },
            },

            // ---- beasts and divs of Mazandaran ----
            new EnemyDef
            {
                id = "lion", hp = 170, speed = 1.45f, bounty = 15, meleeMin = 5, meleeMax = 10,
                look = new Look { body = Body.Beast, main = 0xC8923A, accent = 0x7A4A16 },
            },
            new EnemyDef
            {
                id = "gorgsar", hp = 125, speed = 1.25f, armor = 0.2f, bounty = 14, meleeMin = 4, meleeMax = 8,
                look = new Look { body = Body.Wolfman, main = 0x5E5A55, accent = 0x3A2A1A, weapon = Weapon.Axe },
            },
            new EnemyDef
            {
                id = "imp", hp = 26, speed = 1.4f, bounty = 3, meleeMin = 1, meleeMax = 3,
                look = new Look { body = Body.Div, main = 0x6A8A4A, skin = 0x6A8A4A, accent = 0x2A2A2A, head = Headgear.Horns, scale = 0.65f },
            },
            new EnemyDef
            {
                id = "div", hp = 500, speed = 0.6f, magicResist = 0.2f, bounty = 45, lives = 2, meleeMin = 18, meleeMax = 30, attackInterval = 1.4f,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.SpawnOnDeath, summonId = "imp", count = 2 } },
                look = new Look { body = Body.Div, main = 0x4E6B3A, skin = 0x5C7D45, accent = 0x3A2A1A, head = Headgear.Horns, weapon = Weapon.Club, scale = 1.35f },
            },
            new EnemyDef
            {
                id = "winged", hp = 150, speed = 1.05f, bounty = 20, flying = true, meleeMin = 0, meleeMax = 0,
                look = new Look { body = Body.Winged, main = 0x5A3E6E, skin = 0x7A5A8E, accent = 0x2A1A3A, head = Headgear.Horns },
            },
            new EnemyDef
            {
                id = "witch", hp = 230, speed = 0.8f, magicResist = 0.7f, bounty = 30, meleeMin = 3, meleeMax = 6,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.HealAllies, cooldown = 3.5f, value = 60f, radius = 2f } },
                look = new Look { main = 0x6A1E5A, accent = 0x1E1E1E, skin = 0xB8C7A0, head = Headgear.Hood, weapon = Weapon.Staff },
            },
            new EnemyDef
            {
                id = "snake", hp = 60, speed = 1.2f, bounty = 4, meleeMin = 3, meleeMax = 6,
                look = new Look { body = Body.Snake, main = 0x3E6B2E, accent = 0xC9A227, scale = 0.8f },
            },

            // ---- champions and bosses ----
            new EnemyDef
            {
                id = "garsivaz", hp = 2400, speed = 0.55f, armor = 0.3f, magicResist = 0.2f, bounty = 300, lives = 20, boss = true,
                meleeMin = 25, meleeMax = 40,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.Summon, cooldown = 10f, summonId = "soldier", count = 3 } },
                look = new Look { main = 0x7A1F1F, accent = 0xC9A227, head = Headgear.Plume, weapon = Weapon.Sword, shield = true, beard = 0x2A1A10, cape = 0x1E1E1E, scale = 1.5f },
            },
            new EnemyDef
            {
                id = "dragon", hp = 3400, speed = 0.5f, armor = 0.2f, magicResist = 0.3f, bounty = 400, lives = 20, boss = true,
                meleeMin = 40, meleeMax = 60, attackInterval = 1.5f,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.FireBreath, cooldown = 6f, value = 70f, radius = 1.8f } },
                look = new Look { body = Body.Dragon, main = 0x2E5A3A, accent = 0xC94A2A, scale = 1.9f },
            },
            new EnemyDef
            {
                id = "divsepid", hp = 6500, speed = 0.42f, armor = 0.2f, magicResist = 0.3f, bounty = 600, lives = 20, boss = true,
                meleeMin = 60, meleeMax = 90, attackInterval = 1.6f,
                abilities = new[]
                {
                    new AbilitySpec { type = EnemyAbility.Stomp, cooldown = 8f, value = 3f, radius = 2f },
                    new AbilitySpec { type = EnemyAbility.DisableTower, cooldown = 11f, value = 5f, radius = 4f },
                },
                look = new Look { body = Body.Div, main = 0xE8E8E8, skin = 0xF2F2F2, accent = 0x5A5A5A, head = Headgear.Horns, weapon = Weapon.Club, scale = 2.1f },
            },
            new EnemyDef
            {
                id = "houman", hp = 5200, speed = 0.6f, armor = 0.45f, magicResist = 0.1f, bounty = 500, lives = 20, boss = true,
                meleeMin = 40, meleeMax = 70,
                abilities = new[]
                {
                    new AbilitySpec { type = EnemyAbility.Enrage, value = 1.6f },
                    new AbilitySpec { type = EnemyAbility.Summon, cooldown = 14f, summonId = "cavalry", count = 2 },
                },
                look = new Look { main = 0x5A1A1A, accent = 0xC9A227, head = Headgear.Plume, weapon = Weapon.Mace, shield = true, mounted = true, beard = 0x2A1A10, cape = 0x8E2B25, scale = 1.5f },
            },
            new EnemyDef
            {
                id = "ashkbus", hp = 5000, speed = 0.55f, armor = 0.3f, magicResist = 0.3f, bounty = 500, lives = 20, boss = true,
                meleeMin = 30, meleeMax = 50, rangedRange = 3f, rangedMin = 40, rangedMax = 60, rangedInterval = 1.5f,
                abilities = new[] { new AbilitySpec { type = EnemyAbility.Summon, cooldown = 12f, summonId = "archer", count = 3 } },
                look = new Look { main = 0x3A3A6A, accent = 0xC9A227, head = Headgear.Plume, weapon = Weapon.Bow, beard = 0x3A2418, cape = 0x8E2B25, scale = 1.5f },
            },
            new EnemyDef
            {
                id = "akvan", hp = 7000, speed = 0.55f, magicResist = 0.5f, bounty = 700, lives = 20, boss = true,
                meleeMin = 50, meleeMax = 80, attackInterval = 1.4f,
                abilities = new[]
                {
                    new AbilitySpec { type = EnemyAbility.Teleport, cooldown = 9f, value = 2.5f },
                    new AbilitySpec { type = EnemyAbility.DisableTower, cooldown = 12f, value = 4f, radius = 4f },
                },
                look = new Look { body = Body.Div, main = 0x8A6A3A, skin = 0xA88A5A, accent = 0x3A2A1A, head = Headgear.Horns, weapon = Weapon.Club, scale = 2f },
            },
            new EnemyDef
            {
                id = "afrasiab", hp = 13000, speed = 0.45f, armor = 0.4f, magicResist = 0.4f, bounty = 1000, lives = 20, boss = true,
                meleeMin = 70, meleeMax = 110,
                abilities = new[]
                {
                    new AbilitySpec { type = EnemyAbility.Summon, cooldown = 12f, summonId = "heavy", count = 2 },
                    new AbilitySpec { type = EnemyAbility.DisableTower, cooldown = 10f, value = 5f, radius = 4.5f },
                    new AbilitySpec { type = EnemyAbility.Enrage, value = 1.4f },
                },
                look = new Look { main = 0x1E1E1E, accent = 0xC9A227, head = Headgear.Crown, weapon = Weapon.Sword, shield = true, mounted = true, beard = 0x1A1A1A, cape = 0x8E2B25, scale = 1.7f },
            },
            new EnemyDef
            {
                id = "zahhak", hp = 11000, speed = 0.45f, armor = 0.3f, magicResist = 0.3f, bounty = 1000, lives = 20, boss = true,
                meleeMin = 60, meleeMax = 100,
                abilities = new[]
                {
                    new AbilitySpec { type = EnemyAbility.Summon, cooldown = 8f, summonId = "snake", count = 4 },
                    new AbilitySpec { type = EnemyAbility.Regenerate, value = 25f },
                },
                look = new Look { main = 0x4A1A3A, accent = 0x3E6B2E, head = Headgear.Crown, weapon = Weapon.Staff, beard = 0x1A1A1A, cape = 0x2A1A2A, snakes = true, scale = 1.7f },
            },
        };

        static Dictionary<string, EnemyDef> byId;

        public static EnemyDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, EnemyDef>();
                foreach (var def in All)
                    byId[def.id] = def;
            }
            EnemyDef result;
            return byId.TryGetValue(id, out result) ? result : null;
        }
    }
}
