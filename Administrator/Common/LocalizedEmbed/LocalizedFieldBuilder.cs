using Administrator.Commands;
using Administrator.Services;
using Disqord;

namespace Administrator.Common.LocalizedEmbed
{
    public sealed class LocalizedFieldBuilder
    {
        private readonly LocalEmbedFieldBuilder _builder;
        private readonly LocalizationService _localization;
        private readonly LocalizedLanguage _language;

        public LocalizedFieldBuilder(AdminModuleBase moduleBase)
            : this(moduleBase.Localization, moduleBase.Context.Language)
        { }

        public LocalizedFieldBuilder(LocalizationService localization, LocalizedLanguage language)
        {
            _builder = new LocalEmbedFieldBuilder();
            _localization = localization;
            _language = language;
        }

        public LocalizedFieldBuilder WithLocalizedName(string key, params object[] args)
            => WithName(_localization.Localize(_language, key, args));

        public LocalizedFieldBuilder WithName(string name)
        {
            _builder.WithName(name);
            return this;
        }

        public LocalizedFieldBuilder WithLocalizedValue(string key, params object[] args)
            => WithValue(_localization.Localize(_language, key, args));

        public LocalizedFieldBuilder WithValue(object value)
        {
            _builder.WithValue(value);
            return this;
        }

        public LocalizedFieldBuilder WithValue(string value)
        {
            _builder.WithValue(value);
            return this;
        }

        public LocalizedFieldBuilder WithIsInline(bool isInline)
        {
            _builder.WithIsInline(isInline);
            return this;
        }

        public static implicit operator LocalEmbedFieldBuilder(LocalizedFieldBuilder builder)
            => builder._builder;
    }
}