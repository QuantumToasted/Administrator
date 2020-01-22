using System;
using Administrator.Commands;
using Administrator.Services;
using Disqord;

namespace Administrator.Common.LocalizedEmbed
{
    public sealed class LocalizedEmbedBuilder
    {
        private readonly LocalEmbedBuilder _builder;
        private readonly LocalizationService _localization;
        private readonly LocalizedLanguage _language;

        public LocalizedEmbedBuilder(AdminModuleBase moduleBase)
            : this(moduleBase.Localization, moduleBase.Context.Language)
        { }

        public LocalizedEmbedBuilder(LocalizationService localization, LocalizedLanguage language)
        {
            _builder = new LocalEmbedBuilder();
            _localization = localization;
            _language = language;
        }

        public LocalizedEmbedBuilder WithLocalizedTitle(string key, params object[] args)
            => WithTitle(_localization.Localize(_language, key, args));

        public LocalizedEmbedBuilder WithTitle(string title)
        {
            _builder.WithTitle(title);
            return this;
        }

        public LocalizedEmbedBuilder WithLocalizedDescription(string key, params object[] args)
             => WithDescription(_localization.Localize(_language, key, args));

        public LocalizedEmbedBuilder WithDescription(string description)
        {
            _builder.WithDescription(description);
            return this;
        }

        public LocalizedEmbedBuilder WithUrl(string url)
        {
            _builder.WithUrl(url);
            return this;
        }

        public LocalizedEmbedBuilder WithImageUrl(string imageUrl)
        {
            _builder.WithImageUrl(imageUrl);
            return this;
        }

        public LocalizedEmbedBuilder WithThumbnailUrl(string thumbnailUrl)
        {
            _builder.WithThumbnailUrl(thumbnailUrl);
            return this;
        }

        public LocalizedEmbedBuilder WithTimestamp(DateTimeOffset? timestamp)
        {
            _builder.WithTimestamp(timestamp);
            return this;
        }

        public LocalizedEmbedBuilder WithColor(Color? color)
        {
            _builder.WithColor(color);
            return this;
        }

        public LocalizedEmbedBuilder WithAuthor(LocalizedAuthorBuilder builder)
        {
            _builder.WithAuthor(builder);
            return this;
        }

        public LocalizedEmbedBuilder AddField(LocalizedFieldBuilder builder)
        {
            _builder.AddField(builder);
            return this;
        }

        public LocalizedEmbedBuilder WithFooter(LocalizedFooterBuilder builder)
        {
            _builder.WithFooter(builder);
            return this;
        }

        public LocalEmbed Build()
            => _builder.Build();

        public static implicit operator LocalEmbedBuilder(LocalizedEmbedBuilder builder)
            => builder._builder;
    }
}