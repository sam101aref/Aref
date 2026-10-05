using System;
using IranVsTuran.Defs;

namespace IranVsTuran.Core
{
    /// <summary>
    /// Battle-pass state for the current season. Seasons are <see cref="PassDefs.SeasonDays"/> long
    /// and counted from a fixed date, so every device agrees on the season without a server; when
    /// a new season starts, experience, claims and the royal track reset.
    /// </summary>
    public static class BattlePass
    {
        static readonly DateTime Epoch = new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Overridable clock for tests.</summary>
        public static Func<DateTime> Now = () => DateTime.UtcNow;

        public static int SeasonNumber
        {
            get { return Math.Max(1, (int)Math.Floor((Now() - Epoch).TotalDays / PassDefs.SeasonDays) + 1); }
        }

        public static int DaysLeft
        {
            get
            {
                var end = Epoch.AddDays(SeasonNumber * PassDefs.SeasonDays);
                return Math.Max(0, (int)Math.Ceiling((end - Now()).TotalDays));
            }
        }

        static PassState State
        {
            get
            {
                var state = SaveSystem.Data.pass;
                var season = SeasonNumber;
                if (state.season != season)
                {
                    state.season = season;
                    state.xp = 0;
                    state.premium = false;
                    state.claimedFree.Clear();
                    state.claimedPremium.Clear();
                }
                return state;
            }
        }

        public static int Xp { get { return State.xp; } }
        public static bool Premium { get { return State.premium; } }

        /// <summary>Tiers reached (0–30).</summary>
        public static int Tier { get { return Math.Min(PassDefs.Tiers, State.xp / PassDefs.XpPerTier); } }

        /// <summary>Experience inside the current tier (0–99).</summary>
        public static int XpIntoTier { get { return Tier >= PassDefs.Tiers ? PassDefs.XpPerTier : State.xp % PassDefs.XpPerTier; } }

        public static void AddXp(int amount)
        {
            if (amount <= 0)
                return;
            var state = State;
            state.xp = Math.Min(PassDefs.Tiers * PassDefs.XpPerTier, state.xp + amount);
            Economy.Commit();
        }

        public static void UnlockPremium()
        {
            State.premium = true;
            Economy.Commit();
        }

        public static bool IsClaimed(int tier, bool premium)
        {
            return (premium ? State.claimedPremium : State.claimedFree).Contains(tier);
        }

        public static bool CanClaim(int tier, bool premium)
        {
            if (tier < 1 || tier > Tier || IsClaimed(tier, premium))
                return false;
            return !premium || Premium;
        }

        public static bool Claim(int tier, bool premium)
        {
            if (!CanClaim(tier, premium))
                return false;
            (premium ? State.claimedPremium : State.claimedFree).Add(tier);
            Economy.Grant(premium ? PassDefs.Premium(tier) : PassDefs.Free(tier));
            return true;
        }

        /// <summary>Claims everything available; returns how many rewards were claimed.</summary>
        public static int ClaimAll()
        {
            var claimed = 0;
            for (var tier = 1; tier <= Tier; tier++)
            {
                if (Claim(tier, false))
                    claimed++;
                if (Claim(tier, true))
                    claimed++;
            }
            return claimed;
        }

        public static int UnclaimedCount
        {
            get
            {
                var count = 0;
                for (var tier = 1; tier <= Tier; tier++)
                {
                    if (CanClaim(tier, false))
                        count++;
                    if (CanClaim(tier, true))
                        count++;
                }
                return count;
            }
        }

        /// <summary>Buys the next tier with gems.</summary>
        public static bool SkipTier()
        {
            if (Tier >= PassDefs.Tiers || !Economy.TrySpendGems(PassDefs.TierSkipGems))
                return false;
            var state = State;
            state.xp = (state.xp / PassDefs.XpPerTier + 1) * PassDefs.XpPerTier;
            Economy.Commit();
            return true;
        }

        /// <summary>Experience for a won battle.</summary>
        public static int XpForWin(int stars)
        {
            return PassDefs.XpPerWin + PassDefs.XpPerStar * stars;
        }
    }
}
