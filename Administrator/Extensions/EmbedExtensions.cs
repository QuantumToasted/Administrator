using Administrator.Services;
using Discord;

namespace Administrator.Extensions
{
    public static class EmbedExtensions
    {
        private static readonly ConfigurationService Config = ConfigurationService.Basic;

        public static EmbedBuilder WithSuccessColor(this EmbedBuilder builder)
            => builder.WithColor(Config.SuccessColor);

        public static EmbedBuilder WithWarnColor(this EmbedBuilder builder)
            => builder.WithColor(Config.WarnColor);

        public static EmbedBuilder WithErrorColor(this EmbedBuilder builder)
            => builder.WithColor(Config.ErrorColor);
    }
}
