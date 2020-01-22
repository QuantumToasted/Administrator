using Administrator.Commands;
using Administrator.Services;
using Disqord;

namespace Administrator.Common.LocalizedEmbed
{
    public sealed class LocalizedAuthorBuilder
    {
        private readonly LocalEmbedAuthorBuilder _builder;
        private readonly LocalizationService _localization;
        private readonly LocalizedLanguage _language;

        public LocalizedAuthorBuilder(AdminModuleBase moduleBase)
            : this(moduleBase.Localization, moduleBase.Context.Language)
        { }

        public LocalizedAuthorBuilder(LocalizationService localization, LocalizedLanguage language)
        {
            _builder = new LocalEmbedAuthorBuilder();
            _localization = localization;
            _language = language;
        }

        public LocalizedAuthorBuilder WithLocalizedName(string key, params object[] args)
            => WithName(_localization.Localize(_language, key, args));

        public LocalizedAuthorBuilder WithName(string name)
        {
            _builder.WithName(name);
            return this;
        }

        public LocalEmbedAuthorBuilder WithUrl(string url)
        {
            _builder.WithUrl(url);
            return this;
        }

        public LocalizedAuthorBuilder WithIconUrl(string iconUrl)
        {
            _builder.WithIconUrl(iconUrl);
            return this;
        }

        public static implicit operator LocalEmbedAuthorBuilder(LocalizedAuthorBuilder builder)
            => builder._builder;
    }
}