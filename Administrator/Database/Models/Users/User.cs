using System;
using System.Collections.Generic;
using Administrator.Common;

namespace Administrator.Database
{
    public abstract class User
    {
        public const int MAX_LEVEL_CIVILIAN = 25;
        public const int MAX_LEVEL_FREELANCE = 50;
        public const int MAX_LEVEL_MERCENARY = 75;
        public const int MAX_LEVEL_COMMANDO = 100;
        public const int MAX_LEVEL_ASSASSIN = 125;
        public const int MAX_LEVEL_ELITE = 150;

        public const int XP_PER_LEVEL_CIVILIAN = 750;
        public const int XP_PER_LEVEL_FREELANCE = 1250;
        public const int XP_PER_LEVEL_MERCENARY = 2000;
        public const int XP_PER_LEVEL_COMMANDO = 3000;
        public const int XP_PER_LEVEL_ASSASSIN = 4500;
        public const int XP_PER_LEVEL_ELITE = 7000;

        public const int MAX_XP_CIVILIAN = 18750;
        public const int MAX_XP_FREELANCE = 50000;
        public const int MAX_XP_MERCENARY = 100000;
        public const int MAX_XP_COMMANDO = 175000;
        public const int MAX_XP_ASSASSIN = 287500;
        public const int MAX_XP_ELITE = 455500;
        public const int MAX_XP_TIER = 462500;

        private User()
        { }

        protected User(ulong id)
        {
            Id = id;
        }

        public ulong Id { get; set; }

        public List<string> PreviousNames { get; set; }

        public int TotalXp { get; set; }

        public int CurrentLevelXp
        {
            get
            {
                if (TotalXp % MAX_XP_TIER > MAX_XP_ELITE) return TotalXp % MAX_XP_ELITE;
                switch (Grade)
                {
                    case Grade.Civilian:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER) % XP_PER_LEVEL_CIVILIAN;
                    case Grade.Freelance:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_CIVILIAN) % XP_PER_LEVEL_FREELANCE;
                    case Grade.Mercenary:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_FREELANCE) % XP_PER_LEVEL_MERCENARY;
                    case Grade.Commando:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_MERCENARY) % XP_PER_LEVEL_COMMANDO;
                    case Grade.Assassin:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_COMMANDO) % XP_PER_LEVEL_ASSASSIN;
                    case Grade.Elite:
                        return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_ASSASSIN) % XP_PER_LEVEL_ELITE;
                    default:
                        return TotalXp;
                }
            }
        }

        public int NextLevelXp
        {
            get
            {
                switch (Grade)
                {
                    case Grade.Civilian:
                        return 750;
                    case Grade.Freelance:
                        return 1250;
                    case Grade.Mercenary:
                        return 2000;
                    case Grade.Commando:
                        return 3000;
                    case Grade.Assassin:
                        return 4500;
                    case Grade.Elite:
                        return 7000;
                    default:
                        return TotalXp;
                }
            }
        }

        public int NextLevelTotalXp
        {
            get
            {
                switch (Grade)
                {
                    case Grade.Civilian:
                        return Level * XP_PER_LEVEL_CIVILIAN +
                               (Tier - 1) * MAX_XP_TIER;
                    case Grade.Freelance:
                        return (Level - MAX_LEVEL_CIVILIAN) * XP_PER_LEVEL_FREELANCE + MAX_XP_CIVILIAN +
                               (Tier - 1) * MAX_XP_TIER;
                    case Grade.Mercenary:
                        return (Level - MAX_LEVEL_FREELANCE) * XP_PER_LEVEL_MERCENARY + MAX_XP_FREELANCE +
                               (Tier - 1) * MAX_XP_TIER;
                    case Grade.Commando:
                        return (Level - MAX_LEVEL_MERCENARY) * XP_PER_LEVEL_COMMANDO + MAX_XP_MERCENARY +
                               (Tier - 1) * MAX_XP_TIER;
                    case Grade.Assassin:
                        return (Level - MAX_LEVEL_COMMANDO) * XP_PER_LEVEL_ASSASSIN + MAX_XP_COMMANDO +
                               (Tier - 1) * MAX_XP_TIER;
                    case Grade.Elite:
                        return (Level - MAX_LEVEL_ASSASSIN) * XP_PER_LEVEL_ELITE + MAX_XP_ASSASSIN +
                               (Tier - 1) * MAX_XP_TIER;
                    default:
                        return Tier * MAX_XP_TIER;
                }
            }
        }

        public int Level
        {
            get
            {
                if (TotalXp % MAX_XP_TIER < XP_PER_LEVEL_FREELANCE) return 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_CIVILIAN)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER) / XP_PER_LEVEL_CIVILIAN + 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_FREELANCE)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_CIVILIAN) / XP_PER_LEVEL_FREELANCE +
                           MAX_LEVEL_CIVILIAN + 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_MERCENARY)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_FREELANCE) / XP_PER_LEVEL_MERCENARY +
                           MAX_LEVEL_FREELANCE + 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_COMMANDO)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_MERCENARY) / XP_PER_LEVEL_COMMANDO +
                           MAX_LEVEL_MERCENARY + 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_ASSASSIN)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_COMMANDO) / XP_PER_LEVEL_ASSASSIN +
                           MAX_LEVEL_COMMANDO + 1;
                if (TotalXp % MAX_XP_TIER < MAX_XP_ELITE)
                    return (TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_ASSASSIN) / XP_PER_LEVEL_ELITE +
                           MAX_LEVEL_ASSASSIN + 1;
                return MAX_LEVEL_ELITE;
            }
        }

        public int Tier
            => TotalXp / MAX_XP_TIER + 1;

        public Grade Grade
        {
            get
            {
                if (Level <= MAX_LEVEL_CIVILIAN) return Grade.Civilian;
                if (Level <= MAX_LEVEL_FREELANCE) return Grade.Freelance;
                if (Level <= MAX_LEVEL_MERCENARY) return Grade.Mercenary;
                if (Level <= MAX_LEVEL_COMMANDO) return Grade.Commando;
                return Level <= MAX_LEVEL_ASSASSIN ? Grade.Assassin : Grade.Elite;
            }
        }

        public DateTimeOffset LastXpGain { get; set; }

        public DateTimeOffset LastLevelUp { get; set; } = DateTimeOffset.UtcNow;
    }
}