using System;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;

namespace Administrator.Commands
{
    [Name("Moderation")]
    public sealed class ModerationModule : DiscordGuildModuleBase
    {
        public PunishmentService Punishments { get; set; }

        [Command("ban", "b")]
        [RequireAuthorGuildPermissions(Permission.BanMembers)]
        [RequireBotGuildPermissions(Permission.BanMembers)]
        public Task<DiscordCommandResult> BanAsync(IMember target,
            TimeSpan duration,
            Upload attachment,
            [Remainder] string reason = null)
        {
            return BanAsync(target, (TimeSpan?) duration, attachment, reason);
        }
        
        [Command("ban", "b")]
        [RequireAuthorGuildPermissions(Permission.BanMembers)]
        [RequireBotGuildPermissions(Permission.BanMembers)]
        public Task<DiscordCommandResult> BanAsync(IMember target,
            TimeSpan duration,
            [Remainder] string reason = null)
        {
            return BanAsync(target, duration, default, reason);
        }
        
        [Command("ban", "b")]
        [RequireAuthorGuildPermissions(Permission.BanMembers)]
        [RequireBotGuildPermissions(Permission.BanMembers)]
        public Task<DiscordCommandResult> BanAsync(IMember target,
            Upload attachment,
            [Remainder] string reason = null)
        {
            return BanAsync(target, default, attachment, reason);
        }
        
        [Command("ban", "b")]
        [RequireAuthorGuildPermissions(Permission.BanMembers)]
        [RequireBotGuildPermissions(Permission.BanMembers)]
        public Task<DiscordCommandResult> BanAsync(IMember target,
            [Remainder] string reason = null)
        {
            return BanAsync(target, default, default, reason);
        }

        /*
        [Command("hackban")]
        [RequireUserGuildPermissions(Permission.BanMembers)]
        [RequireBotGuildPermissions(Permission.BanMembers)]
        public async Task<DiscordCommandResult> BanAsync(Snowflake targetId,
            Upload attachment,
            [Remainder] string reason = null)
        {
            var target = await Context.Bot.GetOrFetchUserAsync(targetId);
            if (Context.Bot.GetMember(Context.GuildId, targetId) is not null)
            {
                
            }
        }
        */

        private async Task<DiscordCommandResult> BanAsync(IUser target, TimeSpan? duration, Upload attachment, string reason)
        {
            if (await Context.Bot.FetchBanAsync(Context.GuildId, target.Id) is not null)
            {
                return Response($"{Markdown.Bold(target.Tag.Sanitize())} has already been banned!");
            }

            var result = await Punishments.BanAsync(Context.Guild, target, Context.Author, duration, reason, attachment);
            if (!result.IsSuccessful)
                return Response(result.FailureReason);

            return Response(
                $"{result.Punishment} {Markdown.Bold(target.Format())} has left the server [VAC banned from secure server].");
        }
    }
}