namespace Administrator.Core;

public static class UserXpExtensions
{
    private const int MAX_LEVEL_CIVILIAN = 25;
    private const int MAX_LEVEL_FREELANCE = 50;
    private const int MAX_LEVEL_MERCENARY = 75;
    private const int MAX_LEVEL_COMMANDO = 100;
    private const int MAX_LEVEL_ASSASSIN = 125;
    private const int MAX_LEVEL_ELITE = 150;

    private const int XP_PER_LEVEL_CIVILIAN = 750;
    private const int XP_PER_LEVEL_FREELANCE = 1250;
    private const int XP_PER_LEVEL_MERCENARY = 2000;
    private const int XP_PER_LEVEL_COMMANDO = 3000;
    private const int XP_PER_LEVEL_ASSASSIN = 4500;
    private const int XP_PER_LEVEL_ELITE = 7000;

    private const int MAX_XP_CIVILIAN = 18750;
    private const int MAX_XP_FREELANCE = 50000;
    private const int MAX_XP_MERCENARY = 100000;
    private const int MAX_XP_COMMANDO = 175000;
    private const int MAX_XP_ASSASSIN = 287500;
    private const int MAX_XP_ELITE = 455500;
    private const int MAX_XP_TIER = 462500;
    
    extension<TUserXp>(TUserXp xp) where TUserXp : IUserXp
    {
        public int CurrentLevelXp
        {
            get
            {
                if (xp.TotalXp % MAX_XP_TIER > MAX_XP_ELITE) return xp.TotalXp % MAX_XP_ELITE;
                return xp.Grade switch
                {
                    Grade.Civilian => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER) % XP_PER_LEVEL_CIVILIAN,
                    Grade.Freelance => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_CIVILIAN) % XP_PER_LEVEL_FREELANCE,
                    Grade.Mercenary => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_FREELANCE) %
                                       XP_PER_LEVEL_MERCENARY,
                    Grade.Commando => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_MERCENARY) % XP_PER_LEVEL_COMMANDO,
                    Grade.Assassin => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_COMMANDO) % XP_PER_LEVEL_ASSASSIN,
                    Grade.Elite => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_ASSASSIN) % XP_PER_LEVEL_ELITE,
                    _ => xp.TotalXp
                };
            }
        }

        public int NextLevelXp
        {
            get
            {
                return xp.Grade switch
                {
                    Grade.Civilian => 750,
                    Grade.Freelance => 1250,
                    Grade.Mercenary => 2000,
                    Grade.Commando => 3000,
                    Grade.Assassin => 4500,
                    Grade.Elite => 7000,
                    _ => xp.TotalXp
                };
            }
        }

        public int NextLevelTotalXp
        {
            get
            {
                return xp.Grade switch
                {
                    Grade.Civilian => xp.Level * XP_PER_LEVEL_CIVILIAN + (xp.Tier - 1) * MAX_XP_TIER,
                    Grade.Freelance => (xp.Level - MAX_LEVEL_CIVILIAN) * XP_PER_LEVEL_FREELANCE + MAX_XP_CIVILIAN +
                                       (xp.Tier - 1) * MAX_XP_TIER,
                    Grade.Mercenary => (xp.Level - MAX_LEVEL_FREELANCE) * XP_PER_LEVEL_MERCENARY + MAX_XP_FREELANCE +
                                       (xp.Tier - 1) * MAX_XP_TIER,
                    Grade.Commando => (xp.Level - MAX_LEVEL_MERCENARY) * XP_PER_LEVEL_COMMANDO + MAX_XP_MERCENARY +
                                      (xp.Tier - 1) * MAX_XP_TIER,
                    Grade.Assassin => (xp.Level - MAX_LEVEL_COMMANDO) * XP_PER_LEVEL_ASSASSIN + MAX_XP_COMMANDO +
                                      (xp.Tier - 1) * MAX_XP_TIER,
                    Grade.Elite => (xp.Level - MAX_LEVEL_ASSASSIN) * XP_PER_LEVEL_ELITE + MAX_XP_ASSASSIN +
                                   (xp.Tier - 1) * MAX_XP_TIER,
                    _ => xp.Tier * MAX_XP_TIER
                };
            }
        }

        public int Level
        {
            get
            {
                var tierModulo = xp.TotalXp % MAX_XP_TIER;
                return tierModulo switch
                {
                    < MAX_XP_CIVILIAN => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER) / XP_PER_LEVEL_CIVILIAN + 1,
                    < MAX_XP_FREELANCE => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_CIVILIAN) /
                        XP_PER_LEVEL_FREELANCE + MAX_LEVEL_CIVILIAN + 1,
                    < MAX_XP_MERCENARY => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_FREELANCE) /
                        XP_PER_LEVEL_MERCENARY + MAX_LEVEL_FREELANCE + 1,
                    < MAX_XP_COMMANDO => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_MERCENARY) /
                        XP_PER_LEVEL_COMMANDO + MAX_LEVEL_MERCENARY + 1,
                    < MAX_XP_ASSASSIN => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_COMMANDO) /
                        XP_PER_LEVEL_ASSASSIN + MAX_LEVEL_COMMANDO + 1,
                    < MAX_XP_ELITE => (xp.TotalXp - (xp.Tier - 1) * MAX_XP_TIER - MAX_XP_ASSASSIN) / XP_PER_LEVEL_ELITE +
                                      MAX_LEVEL_ASSASSIN + 1,
                    _ => MAX_LEVEL_ELITE
                };
            }
        }

        public int Tier => xp.TotalXp / MAX_XP_TIER + 1;

        public Grade Grade
        {
            get
            {
                return xp.Level switch
                {
                    <= MAX_LEVEL_CIVILIAN => Grade.Civilian,
                    <= MAX_LEVEL_FREELANCE => Grade.Freelance,
                    <= MAX_LEVEL_MERCENARY => Grade.Mercenary,
                    <= MAX_LEVEL_COMMANDO => Grade.Commando,
                    <= MAX_LEVEL_ASSASSIN => Grade.Assassin,
                    _ => Grade.Elite
                };
            }
        }
    }
}