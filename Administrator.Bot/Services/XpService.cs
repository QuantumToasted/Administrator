using System.Text;
using Administrator.Core;
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
    public const int XP_INCREMENT_RATE = 50;
    
    public static readonly TimeSpan XpGainInterval = TimeSpan.FromMinutes(5);
    
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

        var guildConfig = await db.Guilds.GetValueOrDefault(guildId, g => new
        {
            g.Settings,
            g.XpExemptChannelIds,
            g.CustomXpRate,
            g.CustomXpInterval,
            g.LevelUpEmoji
        });

        // TODO: create a new guild config????
        if (guildConfig is null)
            throw new InvalidOperationException("Invalid guild object state.");

        var dbUser = await db.Users.GetOrCreateAsync(message.Author.Id);
        dbUser.IncrementXp(XP_INCREMENT_RATE, XpGainInterval, out var globalLeveledUp);
        
        if (globalLeveledUp)
        {
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(LocalEmoji.Unicode("🌐"));
                await Task.Delay(TimeSpan.FromSeconds(1));
                await message.AddReactionAsync(emojis.GetLevelEmoji(dbUser.GetTier(), dbUser.GetLevel()));
            });
        }

        if (guildConfig.Settings.HasFlag(GuildSettings.TrackServerXp) && !guildConfig.XpExemptChannelIds.Contains(e.ChannelId))
        {
            var dbMember = await db.Members.GetOrCreateAsync(guildId, message.Author.Id);
            dbMember.IncrementXp(guildConfig.CustomXpRate ?? XP_INCREMENT_RATE, 
                guildConfig.CustomXpInterval ?? XpGainInterval, out var guildLeveledUp);
            
            if (guildLeveledUp)
            {
                _ = Task.Run(async () =>
                {
                    await message.AddReactionAsync(LocalEmoji.FromString(guildConfig.LevelUpEmoji));
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await message.AddReactionAsync(emojis.GetLevelEmoji(dbMember.GetTier(), dbMember.GetLevel()));
                });

                if (await db.LevelRewards.FindAsync(guildId, dbMember.GetTier(), dbMember.GetLevel()) is { } levelReward)
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
                            .AppendNewline($"Congrats on leveling up to {Markdown.Bold($"Tier {dbMember.GetTier()}, Level {dbMember.GetLevel()}")}!");

                        var baseLength = contentBuilder.Length;
                        
                        if (levelReward.GrantedRoleIds.Length > 0)
                        {
                            var grantedRoles = new List<IRole>();
                            //var missingRoles = new List<Snowflake>();
                            foreach (var roleId in levelReward.GrantedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is not { } role)
                                {
                                    continue;
                                }
                                
                                grantedRoles.Add(role);

                                //missingRoles.Add(roleId);
                            }
                            
                            //missingRoles.ForEach(x => levelReward.GrantedRoleIds.Remove(x));

                            if (grantedRoles.Count > 0)
                            {
                                contentBuilder.Append($"You've been given the following {"role".ToQuantity(grantedRoles.Count)}: ")
                                    .AppendJoin(", ", grantedRoles.Select(x => Markdown.Bold(x.Name)))
                                    .AppendNewline();
                            }
                        }

                        if (levelReward.RevokedRoleIds.Length > 0)
                        {
                            var revokedRoles = new List<IRole>();
                            //var missingRoles = new List<Snowflake>();
                            foreach (var roleId in levelReward.RevokedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is not { } role)
                                {
                                    //missingRoles.Add(roleId);
                                    continue;
                                }
                                
                                revokedRoles.Add(role);
                            }
                            
                            //missingRoles.ForEach(x => levelReward.RevokedRoleIds.Remove(x));

                            if (revokedRoles.Count > 0)
                            {
                                contentBuilder.Append($"You've had the following {"role".ToQuantity(revokedRoles.Capacity)} removed: ")
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
            .ToListAsync();
            
        levelRewards = levelRewards
            .Where(x => x.Tier < member.GetTier() || (x.Tier == member.GetTier() && x.Level <= member.GetLevel()))
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.Level)
            .ToList();

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