using System.Collections.Generic;

namespace IranVsTuran.Defs
{
    public enum ProductType
    {
        /// <summary>Can be bought again and again (gem packs).</summary>
        Consumable,
        /// <summary>Bought once and kept forever (starter pack).</summary>
        NonConsumable,
        /// <summary>Bought once per battle-pass season.</summary>
        Season,
    }

    /// <summary>A real-money product sold through the platform store.</summary>
    public class ProductDef
    {
        public string id;
        public ProductType type;
        /// <summary>Shown until the store reports the localized price.</summary>
        public string fallbackPrice;
        public Reward[] rewards = new Reward[0];
        /// <summary>A ribbon such as "best value", or null.</summary>
        public string badgeKey;
        public string NameKey { get { return "product." + id; } }
    }

    /// <summary>Gold sold for gems inside the game.</summary>
    public class GoldOffer
    {
        public string id;
        public int gems;
        public int gold;
    }

    /// <summary>Where a rewarded ad can be watched, its reward and its daily limit.</summary>
    public class AdPlacement
    {
        public string id;
        public int dailyLimit;
        public Reward reward;
    }

    public static class ShopDefs
    {
        public const string BattlePassProduct = "battle_pass";
        public const string StarterPack = "starter_pack";

        public static readonly List<ProductDef> Products = new List<ProductDef>
        {
            new ProductDef { id = "gems_small", type = ProductType.Consumable, fallbackPrice = "$0.99", rewards = new[] { Reward.Gems(80) } },
            new ProductDef { id = "gems_medium", type = ProductType.Consumable, fallbackPrice = "$4.99", rewards = new[] { Reward.Gems(500) }, badgeKey = "shop.badge.popular" },
            new ProductDef { id = "gems_large", type = ProductType.Consumable, fallbackPrice = "$9.99", rewards = new[] { Reward.Gems(1100) } },
            new ProductDef { id = "gems_huge", type = ProductType.Consumable, fallbackPrice = "$19.99", rewards = new[] { Reward.Gems(2500) }, badgeKey = "shop.badge.best" },
            new ProductDef
            {
                id = StarterPack, type = ProductType.NonConsumable, fallbackPrice = "$2.99", badgeKey = "shop.badge.once",
                rewards = new[]
                {
                    Reward.Gems(300), Reward.Gold(2000), Reward.Hero(HeroIds.Gordafarid),
                    Reward.Item(ItemIds.Nushdaru, 3), Reward.Item(ItemIds.Simorgh, 3), Reward.Item(ItemIds.Naphtha, 3),
                },
            },
            new ProductDef { id = BattlePassProduct, type = ProductType.Season, fallbackPrice = "$4.99" },
        };

        public static readonly List<GoldOffer> GoldOffers = new List<GoldOffer>
        {
            new GoldOffer { id = "gold_small", gems = 40, gold = 800 },
            new GoldOffer { id = "gold_medium", gems = 150, gold = 3500 },
            new GoldOffer { id = "gold_large", gems = 400, gold = 10000 },
        };

        public const string AdFreeGold = "ad_gold";
        public const string AdFreeGems = "ad_gems";
        public const string AdDoubleReward = "ad_double";
        public const string AdRevive = "ad_revive";
        public const string AdDailyDouble = "ad_daily";

        public static readonly List<AdPlacement> Ads = new List<AdPlacement>
        {
            new AdPlacement { id = AdFreeGold, dailyLimit = 5, reward = Reward.Gold(150) },
            new AdPlacement { id = AdFreeGems, dailyLimit = 3, reward = Reward.Gems(5) },
            new AdPlacement { id = AdDoubleReward, dailyLimit = 10 },
            new AdPlacement { id = AdRevive, dailyLimit = 5 },
            new AdPlacement { id = AdDailyDouble, dailyLimit = 1 },
        };

        /// <summary>Lives given back when the player watches an ad after losing.</summary>
        public const int ReviveLives = 5;
        /// <summary>Gems that buy a second chance instead of an ad.</summary>
        public const int ReviveGems = 30;

        public static ProductDef Product(string id)
        {
            return Products.Find(p => p.id == id);
        }

        public static AdPlacement Ad(string id)
        {
            return Ads.Find(a => a.id == id);
        }

        /// <summary>Seven-day login calendar; day 7 is the big one.</summary>
        public static readonly Reward[] Daily =
        {
            Reward.Gold(100), Reward.Gems(5), Reward.Item(ItemIds.Treasure, 1), Reward.Gold(250),
            Reward.Gems(10), Reward.Item(ItemIds.Simorgh, 1), Reward.Gems(30),
        };
    }

    /// <summary>
    /// The battle pass ("نبردنامه"): 30 tiers per season, each worth <see cref="XpPerTier"/>
    /// experience earned by winning battles. The free track is open to everyone; the royal track
    /// is unlocked by buying the pass for the season.
    /// </summary>
    public static class PassDefs
    {
        public const int Tiers = 30;
        public const int XpPerTier = 100;
        public const int SeasonDays = 56;
        public const int TierSkipGems = 60;
        public const int XpPerWin = 60;
        public const int XpPerStar = 25;

        public static Reward Free(int tier)
        {
            if (tier % 10 == 0)
                return Reward.Gems(25);
            if (tier % 5 == 0)
                return Reward.Item(tier % 10 == 5 ? ItemIds.Simorgh : ItemIds.Nushdaru, 1);
            if (tier % 3 == 0)
                return Reward.Gems(5);
            return Reward.Gold(100 + 10 * tier);
        }

        public static Reward Premium(int tier)
        {
            switch (tier)
            {
                case 1: return Reward.Gems(50);
                case 10: return Reward.Hero(HeroIds.Zal);
                case 20: return Reward.Skin("esfandiar_gold");
                case 30: return Reward.Skin("rostam_babr");
            }
            if (tier % 5 == 0)
                return Reward.Gems(60);
            if (tier % 4 == 0)
                return Reward.Item(ItemIds.Naphtha, 2);
            if (tier % 3 == 0)
                return Reward.Item(ItemIds.Treasure, 2);
            return Reward.Gold(300 + 20 * tier);
        }
    }
}
