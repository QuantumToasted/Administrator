﻿using System;
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
                return Grade switch
                {
                    Grade.Civilian => ((TotalXp - (Tier - 1) * MAX_XP_TIER) % XP_PER_LEVEL_CIVILIAN),
                    Grade.Freelance => ((TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_CIVILIAN) %
                                        XP_PER_LEVEL_FREELANCE),
                    Grade.Mercenary => ((TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_FREELANCE) %
                                        XP_PER_LEVEL_MERCENARY),
                    Grade.Commando => ((TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_MERCENARY) % XP_PER_LEVEL_COMMANDO),
                    Grade.Assassin => ((TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_COMMANDO) % XP_PER_LEVEL_ASSASSIN),
                    Grade.Elite => ((TotalXp - (Tier - 1) * MAX_XP_TIER - MAX_XP_ASSASSIN) % XP_PER_LEVEL_ELITE),
                    _ => TotalXp
                };
            }
        }

        public int NextLevelXp
        {
            get
            {
                return Grade switch
                {
                    Grade.Civilian => 750,
                    Grade.Freelance => 1250,
                    Grade.Mercenary => 2000,
                    Grade.Commando => 3000,
                    Grade.Assassin => 4500,
                    Grade.Elite => 7000,
                    _ => TotalXp
                };
            }
        }

        public int NextLevelTotalXp
        {
            get
            {
                return Grade switch
                {
                    Grade.Civilian => (Level * XP_PER_LEVEL_CIVILIAN + (Tier - 1) * MAX_XP_TIER),
                    Grade.Freelance => ((Level - MAX_LEVEL_CIVILIAN) * XP_PER_LEVEL_FREELANCE + MAX_XP_CIVILIAN +
                                        (Tier - 1) * MAX_XP_TIER),
                    Grade.Mercenary => ((Level - MAX_LEVEL_FREELANCE) * XP_PER_LEVEL_MERCENARY + MAX_XP_FREELANCE +
                                        (Tier - 1) * MAX_XP_TIER),
                    Grade.Commando => ((Level - MAX_LEVEL_MERCENARY) * XP_PER_LEVEL_COMMANDO + MAX_XP_MERCENARY +
                                       (Tier - 1) * MAX_XP_TIER),
                    Grade.Assassin => ((Level - MAX_LEVEL_COMMANDO) * XP_PER_LEVEL_ASSASSIN + MAX_XP_COMMANDO +
                                       (Tier - 1) * MAX_XP_TIER),
                    Grade.Elite => ((Level - MAX_LEVEL_ASSASSIN) * XP_PER_LEVEL_ELITE + MAX_XP_ASSASSIN +
                                    (Tier - 1) * MAX_XP_TIER),
                    _ => (Tier * MAX_XP_TIER)
                };
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