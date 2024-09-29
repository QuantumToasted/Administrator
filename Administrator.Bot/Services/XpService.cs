using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class XpService(EmojiService emojis) : DiscordBotService
{
#if !MIGRATING
    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId || 
            e.Message is not IGatewayUserMessage message ||
            message.Author.IsBot || string.IsNullOrEmpty(message.Content))
        {
            return;
        }

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var guildConfig = await db.Guilds.GetOrCreateAsync(guildId);

        var dbUser = await db.Users.GetOrCreateAsync(message.Author.Id);
        dbUser.IncrementXp(UserBase.XP_INCREMENT_RATE, UserBase.XpGainInterval, out var globalLeveledUp);
        
        if (globalLeveledUp)
        {
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(LocalEmoji.Unicode("🌐"));
                await Task.Delay(TimeSpan.FromSeconds(1));
                await message.AddReactionAsync(emojis.GetLevelEmoji(dbUser.Tier, dbUser.Level));
            });
        }

        if (guildConfig.HasSetting(GuildSettings.TrackServerXp) && !guildConfig.XpExemptChannelIds.Contains(e.ChannelId))
        {
            var dbMember = await db.Members.GetOrCreateAsync(guildId, message.Author.Id);
            dbMember.IncrementXp(guildConfig.CustomXpRate ?? UserBase.XP_INCREMENT_RATE, 
                guildConfig.CustomXpInterval ?? UserBase.XpGainInterval, out var guildLeveledUp);
            
            if (guildLeveledUp)
            {
                _ = Task.Run(async () =>
                {
                    await message.AddReactionAsync(LocalEmoji.FromString(guildConfig.LevelUpEmoji));
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await message.AddReactionAsync(emojis.GetLevelEmoji(dbMember.Tier, dbMember.Level));
                });

                if (await db.LevelRewards.FindAsync(guildId, dbMember.Tier, dbMember.Level) is { } levelReward)
                {
                    var member = e.Member ?? message.Author as IMember ??
                        await Bot.GetOrFetchMemberAsync(guildId, message.Author.Id);

                    if (member is null)
                    {
                        Logger.LogWarning("Member {MemberId} in guild {GuildId} was null when trying to apply a role level reward.",
                            message.Author.Id.RawValue, guildId.RawValue);
                    }
                    else
                    {
                        var contentBuilder = new StringBuilder()
                            .AppendNewline($"Congrats on leveling up to {Markdown.Bold($"Tier {dbMember.Tier}, Level {dbMember.Level}")}!");

                        var baseLength = contentBuilder.Length;
                        
                        if (levelReward.GrantedRoleIds.Count > 0)
                        {
                            var grantedRoles = new List<IRole>();
                            var missingRoles = new List<Snowflake>();
                            foreach (var roleId in levelReward.GrantedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is { } role)
                                {
                                    grantedRoles.Add(role);
                                    continue;
                                }

                                missingRoles.Add(roleId);
                            }
                            
                            missingRoles.ForEach(x => levelReward.GrantedRoleIds.Remove(x));

                            if (grantedRoles.Count > 0)
                            {
                                contentBuilder.Append($"You've been given the following {"role".ToQuantity(levelReward.GrantedRoleIds.Count)}: ")
                                    .AppendJoin(", ", grantedRoles.Select(x => Markdown.Bold(x.Name)))
                                    .AppendNewline();
                            }
                        }

                        if (levelReward.RevokedRoleIds.Count > 0)
                        {
                            var revokedRoles = new List<IRole>();
                            var missingRoles = new List<Snowflake>();
                            foreach (var roleId in levelReward.RevokedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is { } role)
                                {
                                    revokedRoles.Add(role);
                                    continue;
                                }

                                missingRoles.Add(roleId);
                            }
                            
                            missingRoles.ForEach(x => levelReward.RevokedRoleIds.Remove(x));

                            if (revokedRoles.Count > 0)
                            {
                                contentBuilder.Append($"You've had the following {"role".ToQuantity(levelReward.GrantedRoleIds.Count)} removed: ")
                                    .AppendJoin(", ", revokedRoles.Select(x => Markdown.Bold(x.Name)));
                            }
                        }

                        try
                        {
                            await levelReward.ApplyAsync(member);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Failed to apply role level reward for tier {Tier}, level {Level} to member {MemberId} in guild {GuildId}.",
                                levelReward.Tier, levelReward.Level, message.Author.Id.RawValue, guildId.RawValue);

                            contentBuilder.AppendNewline()
                                .AppendNewline("One or more of these roles failed to be added or removed. Contact the server admins to resolve this issue.");
                        }

                        if (contentBuilder.Length > baseLength)
                            _ = message.Author.SendMessageAsync(new LocalMessage().WithContent(contentBuilder.ToString()));
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }
#endif

    protected override async ValueTask OnMemberJoined(MemberJoinedEventArgs e)
    {
        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);
        if (await db.Members.FirstOrDefaultAsync(x => x.GuildId == e.GuildId && x.UserId == e.MemberId) is not { TotalXp: > 0 } member)
            return;
        
        var levelRewards = await db.LevelRewards.Where(x => x.GuildId == e.GuildId)
            .Where(x => x.Tier < member.Tier || (x.Tier == member.Tier && x.Level <= member.Level))
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.Level)
            .ToListAsync();

        var roleIds = e.Member.RoleIds.ToHashSet();
        foreach (var levelReward in levelRewards)
        {
            foreach (var roleId in levelReward.RevokedRoleIds)
            {
                roleIds.Remove(roleId);
            }

            foreach (var roleId in levelReward.GrantedRoleIds)
            {
                roleIds.Add(roleId);
            }
        }

        await Bot.ModifyMemberAsync(e.GuildId, e.MemberId, x => x.RoleIds = roleIds);
    }
}