using System;
using Administrator.Common;
using Administrator.Database;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCommandContext : ICommandContext, IDisposable
    {
        public AdminCommandContext(DiscordShardedClient client, SocketUserMessage message, string prefix, IServiceProvider provider)
        {
            Client = client;
            Shard = client.GetShardFor(Guild);
            User = message.Author;
            Guild = (message.Channel as SocketGuildChannel)?.Guild;
            Message = message;
            Channel = message.Channel;
            IsPrivate = message.Channel is IPrivateChannel;
            Prefix = prefix;
            Database = new AdminDatabaseContext(provider);
        }
        
        public DiscordShardedClient Client { get; }

        public DiscordSocketClient Shard { get; }
        
        public SocketUser User { get; }

        public SocketGuild Guild { get; }
        
        public SocketUserMessage Message { get; }

        public ISocketMessageChannel Channel { get; }

        public bool IsPrivate { get; }
        
        public string Prefix { get; }

        public AdminDatabaseContext Database { get; }
        
        public LocalizedLanguage Language { get; private set; }

        public void SetLanguage(LocalizedLanguage language)
        {
            Language = language;
        }

        public void Dispose()
        {
            Database.Dispose();
        }
    }
}