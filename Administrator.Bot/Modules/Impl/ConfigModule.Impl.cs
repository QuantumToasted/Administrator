using System.Text;
using Administrator.Bot.AutoComplete;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public enum Mode
{
    View,
    Modify,
    Remove
}

public sealed partial class ConfigModule(AdminDbContext db, SlashCommandMentionService mentions) : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> SetJoinRole(IRole? role, bool disable)
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        if (disable)
        {
            guild.JoinRoleId = null;
            await db.SaveChangesAsync();
            return Response("This server's join role has been disabled.");
        }
        
        if (role is not null)
        {
            guild.JoinRoleId = role.Id;
            await db.SaveChangesAsync();
            return Response($"This server's join role has been set to {role.Mention}.");
        }
        
        if (guild.JoinRoleId is { } joinRoleId)
        {
            if (Bot.GetRole(Context.GuildId, joinRoleId) is not { } joinRole)
            {
                guild.JoinRoleId = null;
                await db.SaveChangesAsync();
                return Response($"This server's join role (ID {Markdown.Code(joinRoleId)}) could not be found or was deleted, so it has been disabled.");
            }

            return Response($"This server's join role is currently {joinRole.Mention}.");
        }

        return Response("This server has no join role set up!");
    }

    public partial async Task<IResult> SetLevelUpEmoji(IEmoji emoji)
    {
        if (emoji is ICustomEmoji { Id: var emojiId } && !Bot.GetGuilds().Values.SelectMany(x => x.Emojis.Keys).Contains(emojiId))
            return Response("I do not have access to emojis from this server, so I cannot add it as a reaction.").AsEphemeral();
        
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        guild.LevelUpEmoji = emoji.ToString()!;
        await db.SaveChangesAsync();
        return Response($"The server's custom level-up emoji has been changed to {emoji}.");
    }
    
    public sealed partial class LoggingChannelConfigModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> View()
        {
            var loggingChannels = await db.LoggingChannels.Where(x => x.GuildId == Context.GuildId)
                .ToDictionaryAsync(x => x.EventType);
            
            var responseBuilder = new StringBuilder();
            foreach (var flag in Enum.GetValues<LogEventType>())
            {
                var valueName = flag.ToString("G");
                var description = flag.Humanize();
                var currentChannel = loggingChannels.TryGetValue(flag, out var channel) ? Mention.Channel(channel.ChannelId) : "None";

                responseBuilder.AppendNewline(Markdown.Bold(valueName))
                    .AppendNewline(Markdown.Italics(description))
                    .AppendNewline($"Current channel: {currentChannel}")
                    .AppendNewline();
            }

            return Response(responseBuilder.ToString());
        }
        
        public partial async Task<IResult> Set(LogEventType type, IChannel channel)
        {
            if (await db.LoggingChannels.FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.EventType == type) is { } loggingChannel)
            {
                loggingChannel.ChannelId = channel.Id;
            }
            else
            {
                loggingChannel = new LoggingChannel(Context.GuildId, type, channel.Id);
                db.LoggingChannels.Add(loggingChannel);
            }

            await db.SaveChangesAsync();
            return Response($"{Markdown.Bold(type.ToString("G").Humanize(LetterCasing.Sentence))} events will now be logged to {Mention.Channel(channel.Id)}.");
        }

        public partial async Task<IResult> Remove(LogEventType type)
        {
            if (await db.LoggingChannels.FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.EventType == type) is { } loggingChannel)
            {
                db.LoggingChannels.Remove(loggingChannel);
                await db.SaveChangesAsync();
            }

            return Response($"{Markdown.Bold(type.ToString().Humanize(LetterCasing.Sentence))} events will no longer be logged.");
        }

        public partial async Task Clear(IChannel? channel)
        {
            var promptBuilder = new StringBuilder("All logging events will be disabled in ");
            
            List<LoggingChannel> channelsToClear;
            if (channel is null)
            {
                channelsToClear = await db.LoggingChannels.Where(x => x.GuildId == Context.GuildId).ToListAsync();
                promptBuilder.Append("this server");
            }
            else
            {
                channelsToClear = await db.LoggingChannels.Where(x => x.GuildId == Context.GuildId && x.ChannelId == channel.Id).ToListAsync();
                promptBuilder.Append($"the channel {Mention.Channel(channel.Id)}");
            }
            
            if (channelsToClear.Count > 0)
            {
                promptBuilder.AppendNewline(":").AppendJoin('\n', channelsToClear.Select(x => $"- {Markdown.Bold(x.EventType.ToString("G"))}")).AppendNewline();
            }
            else
            {
                promptBuilder.AppendNewline(".");
            }

            promptBuilder.AppendNewline().AppendNewline(Markdown.Bold("This action CANNOT be undone."));

            var view = new AdminPromptView(promptBuilder.ToString())
                .OnConfirm("Logging channels cleared.");

            await View(view);

            if (view.Result)
            {
                db.LoggingChannels.RemoveRange(channelsToClear);
                await db.SaveChangesAsync();
            }
        }
    }

    public sealed partial class SettingConfigModule(AdminDbContext db, SlashCommandMentionService mentions, EmojiService emojis) : DiscordApplicationGuildModuleBase
    {
        private Guild _guild = null!;
        
        public override async ValueTask OnBeforeExecuted()
        {
            _guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        }

        public partial IResult View()
        {
            var yes = emojis.Names["white_check_mark"];
            var no = emojis.Names["x"];
            
            var responseBuilder = new StringBuilder();
            foreach (var flag in Enum.GetValues<GuildSettings>().Except([GuildSettings.Default]))
            {
                var valueName = flag.ToString("G");
                var description = flag.Humanize();
                var currentValue = _guild.HasSetting(flag) ? $"{yes} {Markdown.Bold("Enabled")}" : $"{no} {Markdown.Bold("Disabled")}";

                responseBuilder.AppendNewline(Markdown.Bold(valueName))
                    .AppendNewline(Markdown.Italics(description))
                    .AppendNewline($"Current value: {currentValue}")
                    .AppendNewline();
            }

            return Response(responseBuilder.ToString());
        }

        public partial async Task<IResult> Enable(GuildSettingFlags setting)
        {
            _guild.Settings |= (GuildSettings) setting;
            await db.SaveChangesAsync();

            return Response($"{Markdown.Bold(setting.ToString())} setting enabled.\n" + setting switch
            {
                GuildSettingFlags.AutomaticPunishmentDetection => "Non-bot bans, timeouts, etc. will now generate punishment cases automatically.",
                GuildSettingFlags.LogModeratorsInPunishments => "Moderators' names will now be shown in punishment embeds.",
                GuildSettingFlags.LogImagesInPunishments => "Image attachments will now be shown in punishment embeds.",
                GuildSettingFlags.FilterDiscordInvites => "Discord invites not from this server will now be filtered when posted.",
                GuildSettingFlags.TrackServerXp => "Server XP will now be tracked and incremented over time.",
                GuildSettingFlags.IgnoreBotMessages => 
                    $"Bot message updates & deletions will now be logged (to the channel configured in {mentions.GetMention("config logging-channels set")}).",
                GuildSettingFlags.AutoQuote => "Message links posted in chat will now trigger an automatic quote post by the bot.",
                _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null)
            });
        }

        public partial async Task<IResult> Disable(GuildSettingFlags setting)
        {
            _guild.Settings &= (GuildSettings) ~setting;
            await db.SaveChangesAsync();

            return Response($"{Markdown.Bold(setting.ToString())} setting disabled.\n" + setting switch
            {
                GuildSettingFlags.AutomaticPunishmentDetection => "Non-bot bans, timeouts, etc. will no longer generate punishment cases automatically.",
                GuildSettingFlags.LogModeratorsInPunishments => "Moderators' names will no longer be shown in punishment embeds.",
                GuildSettingFlags.LogImagesInPunishments => "Image attachments will no longer be shown in punishment embeds.",
                GuildSettingFlags.FilterDiscordInvites => "Discord invites not from this server will no longer be filtered when posted.",
                GuildSettingFlags.TrackServerXp => "Server XP will no longer be tracked and incremented over time.",
                GuildSettingFlags.IgnoreBotMessages => 
                    $"Bot message updates & deletions will no longer be logged (to the channel configured in {mentions.GetMention("config logging-channels set")}).",
                GuildSettingFlags.AutoQuote => "Message links posted in chat will no longer trigger an automatic quote post by the bot.",
                _ => throw new ArgumentOutOfRangeException(nameof(setting), setting, null)
            });
        }
    }

    public sealed partial class InviteFilterExemptionConfigModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> Add(IUser? user, IRole? role, IChannel? channel, Snowflake? guildId, string? inviteCode)
        {
            var exemptions = await db.InviteFilterExemptions.Where(x => x.GuildId == Context.GuildId)
                .ToListAsync();
            
            try
            {
                if (user is not null && exemptions.All(x => x.TargetId != user.Id))
                {
                    db.InviteFilterExemptions.Add(new InviteFilterExemption(Context.GuildId, InviteFilterExemptionType.User, user.Id, null));
                    return Response($"{user.Mention} has been exempted from the invite filter.");
                }
                else if (role is not null && exemptions.All(x => x.TargetId != role.Id))
                {
                    db.InviteFilterExemptions.Add(new InviteFilterExemption(Context.GuildId, InviteFilterExemptionType.Role, role.Id, null));
                    return Response($"Users with the role {role.Mention} have been exempted from the invite filter.");
                }
                else if (channel is not null && exemptions.All(x => x.TargetId != channel.Id))
                {
                    db.InviteFilterExemptions.Add(new InviteFilterExemption(Context.GuildId, InviteFilterExemptionType.Channel, channel.Id, null));
                    return Response($"{Mention.Channel(channel.Id)} has been exempted from the invite filter.");
                }
                else if (guildId.HasValue && exemptions.All(x => x.TargetId == guildId.Value))
                {
                    db.InviteFilterExemptions.Add(new InviteFilterExemption(Context.GuildId, InviteFilterExemptionType.Guild, guildId.Value, null));
                    return Response($"Invites from the server with the ID {Markdown.Code(guildId.Value)} have been exempted from the invite filter.");
                }
                else if (!string.IsNullOrWhiteSpace(inviteCode) && exemptions.All(x => x.InviteCode?.Equals(inviteCode) != true))
                {
                    db.InviteFilterExemptions.Add(new InviteFilterExemption(Context.GuildId, InviteFilterExemptionType.InviteCode, null, inviteCode));
                    return Response($"The invite code {Markdown.Code(inviteCode)} has been exempted from the invite filter.");
                }
                else if (user is null && role is null && channel is null && !guildId.HasValue && string.IsNullOrWhiteSpace(inviteCode))
                {
                    return Response("No options were specified.").AsEphemeral();
                }
                else
                {
                    return Response("The specified entry already exists in the invite filter exemption list!").AsEphemeral();
                }
            }
            finally
            {
                await db.SaveChangesAsync();
            }
        }
        
        public partial async Task<IResult> Remove(IUser? user, IRole? role, IChannel? channel, Snowflake? guildId, string? inviteCode)
        {
            var exemptions = await db.InviteFilterExemptions.Where(x => x.GuildId == Context.GuildId)
                .ToListAsync();
            
            try
            {
                if (user is not null && exemptions.FirstOrDefault(x => x.TargetId == user.Id) is { } userExemption)
                {
                    db.InviteFilterExemptions.Remove(userExemption);
                    return Response($"{user.Mention} has been removed from the invite filter exemption list.");
                }
                else if (role is not null && exemptions.FirstOrDefault(x => x.TargetId == role.Id) is { } roleExemption)
                {
                    db.InviteFilterExemptions.Remove(roleExemption);
                    return Response($"Users with the role {role.Mention} have been removed from the invite filter exemption list.");

                }
                else if (channel is not null && exemptions.FirstOrDefault(x => x.TargetId == channel.Id) is { } channelExemption)
                {
                    db.InviteFilterExemptions.Remove(channelExemption);
                    return Response($"{Mention.Channel(channel.Id)} has been removed from the invite filter exemption list.");
                }
                else if (guildId.HasValue && exemptions.FirstOrDefault(x => x.TargetId == guildId.Value) is { } guildIdExemption)
                {
                    db.InviteFilterExemptions.Remove(guildIdExemption);
                    return Response($"Invites from the server with the ID {Markdown.Code(guildId.Value)} have been removed from the invite filter exemption list.\n" +
                                    $"(You may have to add the vanity invite manually as an invite code.)");
                }
                else if (!string.IsNullOrWhiteSpace(inviteCode) && exemptions.FirstOrDefault(x => x.InviteCode?.Equals(inviteCode) == true) is { } inviteCodeExemption)
                {
                    db.InviteFilterExemptions.Remove(inviteCodeExemption);
                    return Response($"The invite code {Markdown.Bold(inviteCodeExemption.InviteCode)} has been removed from the invite filter exemption list.");
                }
                else if (user is null && role is null && channel is null && !guildId.HasValue && string.IsNullOrWhiteSpace(inviteCode))
                {
                    return Response("No options were specified.").AsEphemeral();
                }
                else
                {
                    return Response("The specified entry does not exist in the invite filter exemption list!").AsEphemeral();
                }
            }
            finally
            {
                await db.SaveChangesAsync();
            }
        }
    }

    public sealed partial class AutomaticPunishmentConfigModule(AdminDbContext db, AutoCompleteService autoComplete) : DiscordApplicationGuildModuleBase
    {
        public enum PunishmentTypeSelection
        {
            Ban = 1,
            Kick = 3,
            Timeout = 5
        }

        public partial async Task<IResult> List()
        {
            var automaticPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == Context.GuildId)
                .OrderBy(x => x.DemeritPoints).ToListAsync();
            if (automaticPunishments.Count == 0)
                return Response("No automatic punishments are currently set up for this server!");

            var pages = automaticPunishments.Chunk(10)
                .Select(x =>
                {
                    var embed = new LocalEmbed()
                        .WithUnusualColor()
                        .WithDescription(string.Join('\n', 
                            x.Select(y =>
                            {
                                var builder = new StringBuilder(Markdown.Bold("demerit point".ToQuantity(y.DemeritPoints)))
                                    .Append(" - ")
                                    .Append(AutomaticPunishmentAutoCompleteFormatter.FormatWarningPunishment(y, false));

                                return builder.ToString();
                            })));

                    return new Page()
                        .WithContent("Automatic punishments for this server:")
                        .AddEmbed(embed);
                }).ToList();
            
            if (pages.Count == 1)
            {
                var firstPage = pages[0];
                return Response(new LocalInteractionMessageResponse { Content = firstPage.Content, Embeds = firstPage.Embeds });
            }
            
            return Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction));
        }

        public partial async Task<IResult> Set(int demeritPoints, PunishmentTypeSelection type, TimeSpan? duration)
        {
            if (type is PunishmentTypeSelection.Kick && duration.HasValue)
                duration = null;

            if (type is PunishmentTypeSelection.Timeout && duration > TimeSpan.FromDays(28))
                return Response("Timeouts may not be any longer than 28 days (4 weeks) in length.").AsEphemeral();
            
            if (await db.AutomaticPunishments
                    .FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.DemeritPoints == demeritPoints) is { } automaticPunishment)
            {
                automaticPunishment.PunishmentType = (PunishmentType)type;
                automaticPunishment.PunishmentDuration = duration;
            }
            else
            {
                automaticPunishment = new AutomaticPunishment(Context.GuildId, demeritPoints, (PunishmentType)type, duration);
                db.AutomaticPunishments.Add(automaticPunishment);
            }

            await db.SaveChangesAsync();
            return Response(
                $"Automatic punishment created/updated. Upon receiving {Markdown.Bold("demerit point".ToQuantity(demeritPoints))}, users will receive the following punishment:\n" +
                Markdown.Bold(AutomaticPunishmentAutoCompleteFormatter.FormatWarningPunishment(automaticPunishment, false)));
        }

        public partial async Task<IResult> Remove(int demeritPoints)
        {
            if (await db.AutomaticPunishments
                    .FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.DemeritPoints == demeritPoints) is { } automaticPunishment)
            {
                db.AutomaticPunishments.Remove(automaticPunishment);
            }

            await db.SaveChangesAsync();
            return Response(
                $"Automatic punishment removed. Users will no longer receive an automatic punishment at {Markdown.Bold("demerit point".ToQuantity(demeritPoints))}.");
        }

        public partial async Task AutoCompleteAutomaticPunishments(AutoComplete<int> demeritPoints)
        {
            var automaticPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == Context.GuildId)
                .OrderBy(x => x.DemeritPoints)
                .ToListAsync();

            autoComplete.AutoComplete(demeritPoints, automaticPunishments);
        }
    }

    public sealed partial class XpExemptChannelConfigModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> Add(IChannel channel)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.XpExemptChannelIds.TryAddUnique(channel.Id);
            await db.SaveChangesAsync();
            return Response($"Messages sent in {Mention.Channel(channel.Id)} will no longer grant XP.");
        }
        
        public partial async Task<IResult> Remove(IChannel channel)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.XpExemptChannelIds.Remove(channel.Id);
            await db.SaveChangesAsync();
            return Response($"Messages sent in {Mention.Channel(channel.Id)} will now grant XP.");
        }
    }

    public partial async Task<IResult> SetTagLimits(int limit)
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        guild.MaximumTagsPerUser = limit >= 0 ? limit : null;
        await db.SaveChangesAsync();
        return Response("Tag limit updated.\n" +
                        (limit > 0
                            ? $"Users will now be limited to creating {"tag".ToQuantity(limit)} each."
                            : "Users will have no limit to the number of tags they can create."));
    }

    public sealed partial class AutoQuoteExemptChannelConfigModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> Add(IChannel channel)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.AutoQuoteExemptChannelIds.TryAddUnique(channel.Id);
            await db.SaveChangesAsync();
            return Response($"Message links sent in {Mention.Channel(channel.Id)} will no longer trigger the automatic quote feature.");
        }
        
        public partial async Task<IResult> Remove(IChannel channel)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.AutoQuoteExemptChannelIds.Remove(channel.Id);
            await db.SaveChangesAsync();
            return Response($"Message links sent in {Mention.Channel(channel.Id)} will now trigger the automatic quote feature.");
        }
    }

    public sealed partial class GreetingConfigModule(AdminDbContext db, IPlaceholderFormatter formatter, SlashCommandMentionService mentions) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> ModifyMessage(Mode mode)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);

            if (mode is Mode.View)
            {
                if (guild.GreetingMessage is null)
                    return Response("No greeting message has been defined for this server!").AsEphemeral();

                await Deferral();
                var message = await guild.GreetingMessage.ToLocalMessageAsync<LocalInteractionMessageResponse>(formatter, Context);
                return Response(message);
            }

            if (mode is Mode.Remove)
            {
                guild.GreetingMessage = null;
                await db.SaveChangesAsync();
                return Response("The greeting message has been removed/disabled for this server.");
            }

            LocalMessage baseGreetingMessage;
            if (guild.GreetingMessage is not null)
            {
                baseGreetingMessage = await guild.GreetingMessage.ToLocalMessageAsync<LocalMessage>(new DiscordPlaceholderFormatter(), Context);
            }
            else
            {
                baseGreetingMessage = new LocalMessage().WithContent("Welcome to {guild.name}, {user.mention}!");
            }
            
            var view = new GreetingMessageEditView(baseGreetingMessage);
            return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
        }

        public partial async Task<IResult> ModifyGreetingDm(bool dm)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.DmGreetingMessage = dm;
            await db.SaveChangesAsync();
            return Response(dm
                ? "The server's greeting message will be sent in the user's DMs."
                : "The server's greeting message will no longer be sent in the user's DMs.\n" +
                  $"(You must configure the greeting channel via {mentions.GetMention("config logging-channels set")} instead.)");
        }
    }
    
    public sealed partial class GoodbyeConfigModule(AdminDbContext db, IPlaceholderFormatter formatter) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> ModifyMessage(Mode mode)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);

            if (mode is Mode.View)
            {
                if (guild.GoodbyeMessage is null)
                    return Response("No goodbye message has been defined for this server!").AsEphemeral();

                await Deferral();
                var message = await guild.GoodbyeMessage.ToLocalMessageAsync<LocalInteractionMessageResponse>(formatter, Context);
                return Response(message);
            }
            
            if (mode is Mode.Remove)
            {
                guild.GoodbyeMessage = null;
                await db.SaveChangesAsync();
                return Response("The goodbye message has been removed/disabled for this server.");
            }
            
            LocalMessage baseGoodbyeMessage;
            if (guild.GoodbyeMessage is not null)
            {
                baseGoodbyeMessage = await guild.GoodbyeMessage.ToLocalMessageAsync<LocalMessage>(new DiscordPlaceholderFormatter(), Context);
            }
            else
            {
                baseGoodbyeMessage = new LocalMessage().WithContent("Hope to see you again soon, {user.mention}!");
            }

            var view = new GoodbyeMessageEditView(baseGoodbyeMessage);
            return Menu(new MessageEditMenu(view, Context.Interaction), TimeSpan.FromMinutes(30));
        }
    }

    public partial async Task<IResult> SetPunishmentText(string? text)
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);

        var content = "Example punishment with custom text:";
        if (text is null)
        {
            if (string.IsNullOrWhiteSpace(guild.CustomPunishmentText))
                return Response("No custom punishment text has been specified on this server.").AsEphemeral();
        }
        else if (string.IsNullOrWhiteSpace(text))
        {
            guild.CustomPunishmentText = null;
            await db.SaveChangesAsync();
            return Response("Custom punishment text removed.");
        }
        else
        {
            guild.CustomPunishmentText = text;
            await db.SaveChangesAsync();
            content = $"Custom punishment text updated!\n{content}";
        }
        
        var fakePunishment = new Ban(Context.GuildId, UserSnapshot.FromUser(Context.Author), UserSnapshot.FromUser(Bot.CurrentUser),
            "Example punishment", null, null);
        fakePunishment = fakePunishment with { Guild = guild }; // hack to set Guild for FormatDmMessage

        var message = await fakePunishment.FormatDmMessageAsync<LocalInteractionMessageResponse>(Bot);
        message.WithContent(content);
        return Response(message);
    }

    public partial async Task<IResult> SetXpRate(int amount, TimeSpan interval)
    {
        if (interval < TimeSpan.FromSeconds(30))
            return Response("The minimum XP interval is 30 seconds.").AsEphemeral();
        
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        guild.CustomXpRate = amount;
        guild.CustomXpInterval = interval;
        await db.SaveChangesAsync();
        return Response($"Custom XP rate updated. Users will now be eligible to gain " +
                        $"{Markdown.Bold(amount)} XP every {Markdown.Bold(interval.Humanize(int.MaxValue, minUnit: TimeUnit.Second))}.");
    }

    public partial async Task<IResult> SetDefaultBanPruneDays(int pruneDays)
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        guild.DefaultBanPruneDays = pruneDays;
        await db.SaveChangesAsync();
        return Response(pruneDays > 0
            ? $"{mentions.GetMention("ban")} will now prune {"day".ToQuantity(pruneDays)} worth of messages by default if no amount is specified."
            : $"{mentions.GetMention("ban")} will not prune any messages by default if no amount is specified.");
    }

    public sealed partial class DemeritPointConfigModule(AdminDbContext db, SlashCommandMentionService mentions, DemeritPointDecayService decayService) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> SetWarningDefault(int demeritPoints)
        {
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.DefaultWarningDemeritPoints = demeritPoints;
            await db.SaveChangesAsync();
            return Response(demeritPoints > 0
                ? $"{mentions.GetMention("warn")} will now add {Markdown.Bold("demerit point".ToQuantity(demeritPoints))} automatically if no amount is specified."
                : $"{mentions.GetMention("warn")} will not add any demerit points by default if no amount is specified.");
        }

        public partial async Task<IResult> SetDecayInterval(TimeSpan? interval)
        {
            if (interval < TimeSpan.FromDays(1))
                return Response("The demerit point decay interval must be at least 24 hours (1 day).").AsEphemeral();
            
            var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
            guild.DemeritPointsDecayInterval = interval;
            await db.SaveChangesAsync();
            decayService.CancelCts();
            return Response(interval.HasValue
                ? $"Demerit points will now decay every {Markdown.Bold(interval.Value.Humanize(int.MaxValue, minUnit: TimeUnit.Day))}."
                : "Demerit points will no longer decay.");
        }
    }
}