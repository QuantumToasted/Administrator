using Administrator.Common.LocalizedEmbed;
using Administrator.Services;
using Disqord;

namespace Administrator.Extensions
{
    public static class EmbedExtensions
    {
        private static readonly ConfigurationService Config = ConfigurationService.Basic;

        public static LocalEmbedBuilder WithSuccessColor(this LocalEmbedBuilder builder)
            => builder.WithColor(Config.SuccessColor);

        public static LocalEmbedBuilder WithWarnColor(this LocalEmbedBuilder builder)
            => builder.WithColor(Config.WarnColor);

        public static LocalEmbedBuilder WithErrorColor(this LocalEmbedBuilder builder)
            => builder.WithColor(Config.ErrorColor);

        public static LocalizedEmbedBuilder WithSuccessColor(this LocalizedEmbedBuilder builder)
            => builder.WithColor(Config.SuccessColor);

        public static LocalizedEmbedBuilder WithWarnColor(this LocalizedEmbedBuilder builder)
            => builder.WithColor(Config.WarnColor);

        public static LocalizedEmbedBuilder WithErrorColor(this LocalizedEmbedBuilder builder)
            => builder.WithColor(Config.ErrorColor);
    }
}
