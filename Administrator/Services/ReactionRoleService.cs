﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Disqord;
using Disqord.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Services
{
    public sealed class ReactionRoleService : Service, 
        IHandler<ReactionAddedEventArgs>, 
        IHandler<ReactionRemovedEventArgs>,
        IHandler<MessageDeletedEventArgs>,
        IHandler<ChannelDeletedEventArgs>
    {
        private readonly LoggingService _logging;

        public ReactionRoleService(IServiceProvider provider)
            : base(provider)
        {
            _logging = _provider.GetRequiredService<LoggingService>();
        }

        public async Task HandleAsync(ReactionAddedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            var user = args.User.HasValue
                ? args.User.Value
                : await args.User.Downloadable.DownloadAsync() as IUser;

            if (user is null) // TODO: Remove after fix
            {
                await _logging.LogErrorAsync($"User with ID {args.User.Id} was null on reaction add.", "ReactionRoles");
                return;
            }

            if (user.IsBot)
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (await ctx.GetReactionRoleAsync(channel.Guild.Id, args.Message.Id, args.Emoji) is { } role)
            {
                await channel.Guild.GrantRoleAsync(args.User.Id, role.Id);
            }
        }

        public async Task HandleAsync(ReactionRemovedEventArgs args)
        {
            if (!(args.Channel is CachedTextChannel channel))
                return;

            var user = args.User.HasValue 
                ? args.User.Value 
                : await args.User.Downloadable.GetOrDownloadAsync() as IUser;

            if (user is null) // TODO: Remove after fix
            {
                await _logging.LogErrorAsync($"User with ID {args.User.Id} was null on reaction remove.", "ReactionRoles");
                return;
            }

            if (user.IsBot)
                return;

            using var ctx = new AdminDatabaseContext(_provider);
            if (await ctx.GetReactionRoleAsync(channel.Guild.Id, args.Message.Id, args.Emoji) is { } role)
            {
                await channel.Guild.RevokeRoleAsync(args.User.Id, role.Id);
            }
        }

        public async Task HandleAsync(MessageDeletedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var reactionRoles = await ctx.ReactionRoles.Where(x => x.MessageId == args.Message.Id)
                .ToListAsync();
            if (reactionRoles.Count > 0)
                ctx.ReactionRoles.RemoveRange(reactionRoles);
        }

        public async Task HandleAsync(ChannelDeletedEventArgs args)
        {
            using var ctx = new AdminDatabaseContext(_provider);
            var reactionRoles = await ctx.ReactionRoles.Where(x => x.ChannelId == args.Channel.Id)
                .ToListAsync();
            if (reactionRoles.Count > 0)
                ctx.ReactionRoles.RemoveRange(reactionRoles);
        }
    }
}