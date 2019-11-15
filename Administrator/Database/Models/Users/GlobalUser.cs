using System.Collections.Generic;
using System.Linq;
using Administrator.Common;
using Administrator.Services;

namespace Administrator.Database
{
    public sealed class GlobalUser : User
    {
        private GlobalUser(ulong id)
            : base(id)
        { }

        public GlobalUser(ulong id, LocalizationService localization)
            : base(id)
        {
            Id = id;
            Language = localization.Languages.First(x => x.CultureCode.Equals("en-US"));
        }

        public LocalizedLanguage Language { get; set; }

        public LevelUpNotification LevelUpPreferences { get; set; }

        public List<ulong> HighlightBlacklist { get; set; }

        public override bool Equals(object obj)
            => (obj as GlobalUser)?.Id == Id;

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                return hash * 31 + Id.GetHashCode();
            }
        }
    }
}