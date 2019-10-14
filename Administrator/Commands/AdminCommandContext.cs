using System;
using Administrator.Common;
using Administrator.Database;
using Administrator.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCommandContext : CommandContext, IDisposable
    {
        private readonly LocalizationService _localization;

        public AdminCommandContext(SocketUserMessage message, string prefix, LocalizedLanguage language, IServiceProvider provider)
            : base(provider)
        {
            _localization = provider.GetRequiredService<LocalizationService>();
            Client = provider.GetRequiredService<DiscordSocketClient>();
            Message = message;
            Prefix = prefix;
            Database = new AdminDatabaseContext(provider);
            Language = language;
        }

        public DiscordSocketClient Client { get; }

        public SocketUser User => Message.Author;

        public SocketGuild Guild => (Message.Channel as SocketGuildChannel)?.Guild;

        public SocketUserMessage Message { get; }

        public ISocketMessageChannel Channel => Message.Channel;

        public bool IsPrivate => Message.Channel is IPrivateChannel;

        public string Prefix { get; }

        public AdminDatabaseContext Database { get; }

        public LocalizedLanguage Language { get; }

        public string Localize(string key, params object[] args)
            => _localization.Localize(Language, key, args);

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}