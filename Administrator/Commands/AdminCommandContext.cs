using System;
using Administrator.Common;
using Administrator.Database;
using Administrator.Services;
using Disqord;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCommandContext : CommandContext, IDisposable
    {
        private readonly LocalizationService _localization;

        public AdminCommandContext(CachedUserMessage message, string prefix, LocalizedLanguage language, IServiceProvider provider)
            : base(provider)
        {
            _localization = provider.GetRequiredService<LocalizationService>();
            Client = provider.GetRequiredService<DiscordClient>();
            Message = message;
            Prefix = prefix;
            Database = new AdminDatabaseContext(provider);
            Language = language;
        }

        public DiscordClient Client { get; }

        public CachedUser User => Message.Author;

        public CachedGuild Guild => (Message.Channel as CachedGuildChannel)?.Guild;

        public CachedUserMessage Message { get; }

        public ICachedMessageChannel Channel => Message.Channel;

        public bool IsPrivate => Message.Channel is IPrivateChannel;

        public string Prefix { get; }

        public AdminDatabaseContext Database { get; }

        public LocalizedLanguage Language { get; set; }

        public string Localize(string key, params object[] args)
            => _localization.Localize(Language, key, args);

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}