using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class TestCommands : AdminModuleBase
    {
        [Command("longerthan")]
        public Task<AdminCommandResult> MustBeLongerThanTest([MustBe(StringLength.LongerThan, 10)] string value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("shorterthan")]
        public Task<AdminCommandResult> MustBeShorterThanTest([MustBe(StringLength.ShorterThan, 3)] string value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("exactly")]
        public Task<AdminCommandResult> MustBeExactlyTest([MustBe(StringLength.Exactly, 5)] string value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("lessthan")]
        public Task<AdminCommandResult> MustBeLessThanTest([MustBe(Operator.LessThan, 3)] int value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("greaterthan")]
        public Task<AdminCommandResult> MustBeGreaterThanTest([MustBe(Operator.GreaterThan, 50)] int value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("equalto")]
        public Task<AdminCommandResult> MustBeEqualToTest([MustBe(Operator.EqualTo, 15)] int value)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("dmsonly")]
        [RequireContext(ContextType.DM)]
        public Task<AdminCommandResult> DMsOnlyTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("guildonly")]
        [RequireContext(ContextType.Guild)]
        public Task<AdminCommandResult> GuildOnlyTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("owneronly")]
        [RequireOwner]
        public Task<AdminCommandResult> OwnerOnlyTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("guildowneronly")]
        [RequireGuildOwner]
        public Task<AdminCommandResult> GuildOwnerOnlyTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("hierarchy")]
        public Task<AdminCommandResult> HierarchyTest([RequireHierarchy, Remainder] SocketGuildUser target)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("botguildperms")]
        [RequireBotPermissions(GuildPermission.Administrator | GuildPermission.ManageGuild)]
        public Task<AdminCommandResult> BotGuildPermissionsTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("botchannelperms")]
        [RequireBotPermissions(ChannelPermission.ManageRoles)]
        public Task<AdminCommandResult> BotChannelPermissionsTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("userguildperms")]
        [RequireUserPermissions(GuildPermission.Administrator | GuildPermission.ManageGuild)]
        public Task<AdminCommandResult> UserGuildPermissionsTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("userchannelperms")]
        [RequireUserPermissions(ChannelPermission.ManageRoles)]
        public Task<AdminCommandResult> UserChannelPermissionsTest()
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("finduser")]
        public Task<AdminCommandResult> FindUserTest([Remainder] SocketGuildUser target)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("findrole")]
        public Task<AdminCommandResult> FindRoleTest([Remainder] SocketRole target)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("findchannel")]
        public Task<AdminCommandResult> FindChannelTest([Remainder] SocketGuildChannel target)
            => CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());

        [Command("setlang")]
        public async Task<AdminCommandResult> SetLanguageAsync(string locale)
        {
            if (Context.IsPrivate)
            {
                var user = await Context.Database.GetOrCreateGlobalUserAsync(Context.User.Id);
                user.Language = Localization.Languages.First(x => x.CultureCode.Equals(locale, StringComparison.OrdinalIgnoreCase));
                Context.Database.GlobalUsers.Update(user);
                await Context.Database.SaveChangesAsync();
                return CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());
            }

            var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
            guild.Language =
                Localization.Languages.First(x => x.CultureCode.Equals(locale, StringComparison.OrdinalIgnoreCase));
            Context.Database.Guilds.Update(guild);
            await Context.Database.SaveChangesAsync();
            return CommandSuccess(Emote.Parse("<:mowpiffygootem:553849138647793674>").ToString());
        }

        [Command("fixate")]
        public Task<AdminCommandResult> Fixate(int center, int truncateTo, [Remainder] string text)
        {
            var original = $"{text}\n{$"^{center}".PadLeft(center + 1)}";
            text = text.FixateTo(ref center, truncateTo);
            var newText = $"{text}\n{$"^{center}".PadLeft(center + 1)}";

            return CommandSuccess(Format.Code($"{original}\n\n{newText}"));
        }
    }
}