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
        private CachedUser _user;
        private CachedGuild _guild;
        private ICachedMessageChannel _channel;

        private AdminCommandContext(LocalizedLanguage language, IServiceProvider provider, CachedUser user,
            CachedGuild guild, CachedUserMessage message, ICachedMessageChannel channel)
            : base(provider)
        {
            User = user;
            Guild = guild;
            Message = message;
            Channel = channel;
            Database = new AdminDatabaseContext(provider);
            Language = language;
        }

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

        public CachedUser User
        {
            get => _user ??= Message?.Author;
            private set => _user = value;
        }

        public CachedGuild Guild
        {
            get => _guild ??= (Message?.Channel as CachedGuildChannel)?.Guild;
            private set => _guild = value;
        }

        public CachedUserMessage Message { get; }

        public ICachedMessageChannel Channel
        {
            get => _channel ??= Message.Channel;
            private set => _channel = value;
        }

        public bool IsPrivate => Channel is IPrivateChannel;

        public string Prefix { get; }

        public AdminDatabaseContext Database { get; }

        public LocalizedLanguage Language { get; set; }

        public string Localize(string key, params object[] args)
            => _localization.Localize(Language, key, args);

        public void Dispose()
        {
            Database.Dispose();
        }

        public static AdminCommandContext MockContext(LocalizedLanguage language, IServiceProvider provider, CachedUser user = null,
            CachedGuild guild = null, CachedUserMessage message = null, ICachedMessageChannel channel = null)
            => new AdminCommandContext(language, provider, user, guild, message, channel);
    }
}