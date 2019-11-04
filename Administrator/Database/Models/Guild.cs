using System;
using System.Collections.Generic;
using System.Linq;
using Administrator.Common;
using Administrator.Services;
using Discord;

namespace Administrator.Database
{
    public sealed class Guild
    {
        private Guild()
        { }
        
        public Guild(ulong id, LocalizationService localization)
        {
            Id = id;
            Language = localization.Languages.First(x => x.CultureCode.Equals("en-US"));
        }
        
        public ulong Id { get; set; }
        
        public LocalizedLanguage Language { get; set; }
        
        public List<string> CustomPrefixes { get; set; } = new List<string>();

        public List<ulong> BlacklistedModmailAuthors { get; set; } = new List<ulong>();

        public GuildSettings Settings { get; set; } = GuildSettings.Punishments | GuildSettings.XpTracking;

        public TimeSpan XpGainInterval { get; set; } = LevelService.XpGainInterval;

        public int XpRate { get; set; } = LevelService.XP_RATE;

        public LevelUpNotification LevelUpWhitelist { get; set; } = LevelUpNotification.Reaction;

        public IEmote LevelUpEmote { get; set; } = EmoteTools.Tada;
    }
}