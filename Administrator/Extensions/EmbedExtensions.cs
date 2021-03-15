using Disqord;

namespace Administrator.Extensions
{
    public static class EmbedExtensions
    {
        public static readonly Color SuccessColor = 0x8650AC;

        public static readonly Color ErrorColor = 0xEB4B4B;

        public static readonly Color WarningColor = 0xFFD700;
        
        public static LocalEmbedBuilder WithSuccessColor(this LocalEmbedBuilder builder) 
            => builder.WithColor(SuccessColor);
        
        public static LocalEmbedBuilder WithErrorColor(this LocalEmbedBuilder builder) 
            => builder.WithColor(ErrorColor);

        public static LocalEmbedBuilder WithWarningColor(this LocalEmbedBuilder builder) 
            => builder.WithColor(WarningColor);
    }
}