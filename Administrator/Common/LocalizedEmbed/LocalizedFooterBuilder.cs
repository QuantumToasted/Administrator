using Administrator.Commands;
using Administrator.Services;
using Disqord;

namespace Administrator.Common.LocalizedEmbed
{
    public sealed class LocalizedFooterBuilder
    {
        private readonly LocalEmbedFooterBuilder _builder;
        private readonly LocalizationService _localization;
        private readonly LocalizedLanguage _language;

        public LocalizedFooterBuilder(AdminModuleBase moduleBase)
            : this(moduleBase.Localization, moduleBase.Context.Language)
        { }

        public LocalizedFooterBuilder(LocalizationService localization, LocalizedLanguage language)
        {
            _builder = new LocalEmbedFooterBuilder();
            _localization = localization;
            _language = language;
        }

        public LocalizedFooterBuilder WithLocalizedText(string key, params object[] args)
            => WithText(_localization.Localize(_language, key, args));

        public LocalizedFooterBuilder WithText(string text)
        {
            _builder.WithText(text);
            return this;
        }

        public LocalizedFooterBuilder WithIconUrl(string iconUrl)
        {
            _builder.WithIconUrl(iconUrl);
            return this;
        }

        public static implicit operator LocalEmbedFooterBuilder(LocalizedFooterBuilder builder)
            => builder._builder;
    }
}