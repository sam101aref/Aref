using System;

namespace IranVsTuran.Defs
{
    // Game data is plain C# (no Unity types) so it can be checked by tests and tools anywhere.

    /// <summary>A 2D point in world units. The battlefield is 19.2 × 10.8 units around the origin.</summary>
    [Serializable]
    public struct P
    {
        public float x;
        public float y;

        public P(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static float Distance(P a, P b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public enum DamageType
    {
        Physical,
        Magic,
        True,
    }

    public enum RewardType
    {
        Gold,
        Gems,
        Item,
        Hero,
        Skin,
        PassXp,
    }

    /// <summary>Something the player receives: currency, an item, a hero or a skin.</summary>
    [Serializable]
    public struct Reward
    {
        public RewardType type;
        public string id;
        public int amount;

        public static Reward Gold(int amount) { return new Reward { type = RewardType.Gold, amount = amount }; }
        public static Reward Gems(int amount) { return new Reward { type = RewardType.Gems, amount = amount }; }
        public static Reward Item(string id, int amount) { return new Reward { type = RewardType.Item, id = id, amount = amount }; }
        public static Reward Hero(string id) { return new Reward { type = RewardType.Hero, id = id, amount = 1 }; }
        public static Reward Skin(string id) { return new Reward { type = RewardType.Skin, id = id, amount = 1 }; }
    }

    public static class HeroIds
    {
        public const string Rostam = "rostam";
        public const string Gordafarid = "gordafarid";
        public const string Zal = "zal";
        public const string Esfandiar = "esfandiar";
    }

    public static class ItemIds
    {
        /// <summary>The healing elixir of the Shahnameh: restores lives.</summary>
        public const string Nushdaru = "nushdaru";
        /// <summary>A feather of the Simorgh: freezes every enemy for a few seconds.</summary>
        public const string Simorgh = "simorgh";
        /// <summary>A jar of burning naphtha thrown at a spot.</summary>
        public const string Naphtha = "naphtha";
        /// <summary>Kay Khosrow's treasure: extra coins in battle.</summary>
        public const string Treasure = "treasure";
    }

    /// <summary>How a character is drawn by the procedural art.</summary>
    public enum Body
    {
        Human,
        Div,
        Beast,
        Wolfman,
        Winged,
        Dragon,
        Snake,
        Elephant,
    }

    public enum Headgear
    {
        None,
        Helmet,
        SpikeHelm,
        Cap,
        Hood,
        Horns,
        Crown,
        Plume,
        Tiger,
        /// <summary>Rostam's helmet, made from the White Div's head.</summary>
        DivHelm,
    }

    public enum Weapon
    {
        None,
        Sword,
        Spear,
        Bow,
        Club,
        Staff,
        Mace,
        Axe,
    }

    /// <summary>Description of a character's look; colors are 0xRRGGBB.</summary>
    [Serializable]
    public class Look
    {
        public Body body = Body.Human;
        public uint main = 0x8B2E2E;
        public uint accent = 0x3A3A3A;
        public uint skin = 0xE0B48A;
        public Headgear head = Headgear.None;
        public Weapon weapon = Weapon.None;
        public bool shield;
        public bool mounted;
        /// <summary>Cape colour, 0 for none.</summary>
        public uint cape;
        /// <summary>Beard colour, 0 for none.</summary>
        public uint beard;
        /// <summary>Long hair colour, 0 for none.</summary>
        public uint hair;
        /// <summary>Zahhak's serpents growing from the shoulders.</summary>
        public bool snakes;
        public float scale = 1f;

        public Look Clone()
        {
            return (Look)MemberwiseClone();
        }

        /// <summary>Stable text used to cache the generated sprite.</summary>
        public string Key
        {
            get
            {
                return string.Format("{0}_{1:X6}_{2:X6}_{3:X6}_{4}_{5}_{6}{7}{8}_{9:X6}_{10:X6}_{11:X6}", body, main, accent, skin, head,
                    weapon, shield ? 1 : 0, mounted ? 1 : 0, snakes ? 1 : 0, cape, beard, hair);
            }
        }
    }
}
