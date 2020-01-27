using System;
using System.Threading.Tasks;
using Administrator.Commands;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Disqord.Events;

namespace Administrator.Services
{
    public sealed class GreetingService : Service,
        IHandler<MemberJoinedEventArgs>,
        IHandler<MemberLeftEventArgs>
    {
        public GreetingService(IServiceProvider provider) 
            : base(provider)
        { }

        public async Task HandleAsync(MemberJoinedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(args.Member.Guild.Id);

            if (string.IsNullOrWhiteSpace(guild.Greeting))
                return;

            var greeting = await guild.Greeting.FormatPlaceHoldersAsync(
                AdminCommandContext.MockContext(guild.Language, _provider, args.Member, args.Member.Guild));

            var hasEmbed = JsonEmbed.TryParse(greeting, out var embed);

            if (guild.DmGreeting)
            {
                if (hasEmbed)
                {
                    _ = args.Member.SendMessageAsync(embed.Text ?? string.Empty, embed: embed.ToLocalEmbed());
                }
                else
                {
                    _ = args.Member.SendMessageAsync(greeting);
                }

                return;
            }

            if (!(await ctx.GetLoggingChannelAsync(args.Member.Guild.Id, LogType.Greeting) is { } channel))
                return;

            if (hasEmbed)
            {
                await channel.SendMessageAsync(embed.Text ?? string.Empty, embed: embed.ToLocalEmbed());
                return;
            }

            await channel.SendMessageAsync(greeting);
        }

        public async Task HandleAsync(MemberLeftEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var guild = await ctx.GetOrCreateGuildAsync(args.Guild.Id);

            if (string.IsNullOrWhiteSpace(guild.Goodbye))
                return;

            var goodbye = await guild.Goodbye.FormatPlaceHoldersAsync(
                AdminCommandContext.MockContext(guild.Language, _provider, args.User, args.Guild));

            if (!(await ctx.GetLoggingChannelAsync(args.Guild.Id, LogType.Goodbye) is { } channel))
                return;

            if (JsonEmbed.TryParse(goodbye, out var embed))
            {
                await channel.SendMessageAsync(embed.Text ?? string.Empty, embed: embed.ToLocalEmbed());
                return;
            }

            await channel.SendMessageAsync(goodbye);
        }
    }
}