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
    public sealed class AdminCommandContext : ICommandContext, IDisposable
    {
        private readonly LocalizationService _localization;

        public AdminCommandContext(SocketUserMessage message, string prefix, LocalizedLanguage langage, IServiceProvider provider)
        {
            _localization = provider.GetRequiredService<LocalizationService>();
            Client = provider.GetRequiredService<DiscordSocketClient>();
            User = message.Author;
            Guild = (message.Channel as SocketGuildChannel)?.Guild;
            Message = message;
            Channel = message.Channel;
            IsPrivate = message.Channel is IPrivateChannel;
            Prefix = prefix;
            Language = langage;
            Database = new AdminDatabaseContext(provider);
        }
        
        public DiscordSocketClient Client { get; }
        
        public SocketUser User { get; }

        public SocketGuild Guild { get; }
        
        public SocketUserMessage Message { get; }

        public ISocketMessageChannel Channel { get; }

        public bool IsPrivate { get; }
        
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