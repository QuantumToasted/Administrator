using System.Collections.Generic;
using System.Linq;
using Administrator.Common;
using Administrator.Services;

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

        public GuildSettings Settings { get; set; }
    }
}