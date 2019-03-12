using System.Linq;
using Administrator.Common;
using Administrator.Services;

namespace Administrator.Database
{
    public sealed class GlobalUser
    {
        private GlobalUser()
        { }

        public GlobalUser(ulong id, LocalizationService localization)
        {
            Id = id;
            Language = localization.Languages.First(x => x.CultureCode.Equals("en-US"));
        }
        
        public ulong Id { get; set; }
        
        public LocalizedLanguage Language { get; set; }
    }
}