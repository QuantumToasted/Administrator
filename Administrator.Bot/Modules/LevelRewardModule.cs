using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("level-reward")]
[RequireInitialAuthorPermissions(Permissions.ManageGuild)]
public sealed class LevelRewardModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("list")]
    [Description("Lists all level rewards in this server.")]
    public async Task<IResult> ListAsync()
    {
        var levelRewards = await db.LevelRewards.Where(x => x.GuildId == Context.GuildId)
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.Level)
            .ToListAsync();

        var guild = Bot.GetGuild(Context.GuildId)!;
        var pages = levelRewards.Chunk(10)
            .Select(chunk =>
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle($"All level rewards in {guild.Name}");

                foreach (var reward in chunk)
                {
                    embed.AddField(FormatField(reward));
                }

                return new Page().AddEmbed(embed);
            }).ToList();
        
        return pages.Count switch
        {
            0 => Response("No level rewards have been configured on this server!").AsEphemeral(),
            1 => Response(pages[0].Embeds.Value[0]),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
        };
    }

    [SlashCommand("clear")]
    [Description("Clears all level rewards in this server.")]
    public async Task ClearAsync(
        [Description("If set, only clears level rewards for this tier.")]
        [Minimum(1)]
            int? tier = null)
    {
        var levelRewards = await db.LevelRewards.Where(x => x.GuildId == Context.GuildId)
            .ToListAsync();

        if (tier.HasValue)
            levelRewards = levelRewards.Where(x => x.Tier == tier.Value).ToList();

        if (levelRewards.Count == 0)
        {
            await Response(tier.HasValue
                ? $"No level rewards have been configured for Tier {Markdown.Bold(tier.Value)}!"
                : "No level rewards have been created for this server!").AsEphemeral();

            return;
        }

        var embed = new LocalEmbed()
            .WithUnusualColor();

        const int maxFields = Discord.Limits.Message.Embed.MaxFieldAmount;
        foreach (var reward in levelRewards.Take(maxFields - 1))
        {
            embed.AddField(FormatField(reward));
        }

        if (levelRewards.Count >= maxFields)
            embed.AddField(new LocalEmbedField().WithName($"{levelRewards.Count - maxFields + 1} more...").WithBlankValue());
        
        var view = new AdminPromptView($"{Markdown.Bold("level reward".ToQuantity(levelRewards.Count))} will be removed.\n\n" +
                                       Markdown.Bold("This action CANNOT be undone."), embed)
            .OnConfirm(tier.HasValue
                ? $"Level rewards cleared for Tier {Markdown.Bold(tier.Value)}!"
                : "All level rewards cleared!");

        await View(view);

        if (view.Result)
        {
            db.LevelRewards.RemoveRange(levelRewards);
            await db.SaveChangesAsync();
        }
    }
    
    [SlashCommand("add")]
    [Description("Adds a new (or replaces an existing) role level reward for this server.")]
    public async Task AddAsync(
        [Description("The tier this reward is for. (Tiers reset every 150 levels.)")]
        [Minimum(1)]
            int tier,
        [Description("The level (2-150) this reward is for.")]
        [Range(2, 150)]
            int level,
        [Name("added-roles")]
        [Description("A comma-separated list of roles to give to the member upon leveling up.")]
            string? grantedRoleStr = null,
        [Name("removed-roles")]
        [Description("A comma-separated list of roles to remove the member upon leveling up.")]
            string? revokedRoleStr = null)
    {
        if (string.IsNullOrWhiteSpace(grantedRoleStr) && string.IsNullOrWhiteSpace(revokedRoleStr))
        {
            await Response("A role level reward must have added or removed roles, but not neither.").AsEphemeral();
            return;
        }

        var grantedRoles = new HashSet<IRole>();
        if (!string.IsNullOrWhiteSpace(grantedRoleStr))
        {
            foreach (var text in grantedRoleStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                IRole? role;
                if (Mention.TryParseRole(text, out var roleId) ||
                    Snowflake.TryParse(text, out roleId))
                {
                    role = Bot.GetRole(Context.GuildId, roleId);
                }
                else
                {
                    role = Bot.GetRoles(Context.GuildId).Values.FirstOrDefault(x => x.Name.Equals(text));
                }

                if (role is null)
                {
                    await Response($"\"{text}\" was not a valid role name, ID, or mention.");
                    return;
                }

                if (!role.CanBeGrantedOrRevoked())
                {
                    await Response($"{role.Mention} is a booster role, or otherwise cannot be granted/revoked to/from users.");
                    return;
                }

                grantedRoles.Add(role);
            }
        }

        var revokedRoles = new HashSet<IRole>();
        if (!string.IsNullOrWhiteSpace(revokedRoleStr))
        {
            foreach (var text in revokedRoleStr.Split(',',
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                IRole? role;
                if (Mention.TryParseRole(text, out var roleId) ||
                    Snowflake.TryParse(text, out roleId))
                {
                    role = Bot.GetRole(Context.GuildId, roleId);
                }
                else
                {
                    role = Bot.GetRoles(Context.GuildId).Values.FirstOrDefault(x => x.Name.Equals(text));
                }

                if (role is null)
                {
                    await Response($"\"{text}\" was not a valid role name, ID, or mention.");
                    return;
                }

                if (!role.CanBeGrantedOrRevoked())
                {
                    await Response($"{role.Mention} is a booster role, or otherwise cannot be granted/revoked to/from users.");
                    return;
                }

                revokedRoles.Add(role);
            }
        }

        if (await db.LevelRewards.FindAsync(Context.GuildId, tier, level) is { } levelReward)
        {
            levelReward.GrantedRoleIds = grantedRoles.Select(x => x.Id).Distinct().ToList();
            levelReward.RevokedRoleIds = revokedRoles.Select(x => x.Id).Distinct().ToList();
        }
        else
        {
            levelReward = new RoleLevelReward(Context.GuildId, tier, level)
            {
                GrantedRoleIds = grantedRoles.Select(x => x.Id).Distinct().ToList(),
                RevokedRoleIds = revokedRoles.Select(x => x.Id).Distinct().ToList()
            };
            
            db.LevelRewards.Add(levelReward);
        }

        await db.SaveChangesAsync();

        var responseBuilder = new StringBuilder().AppendNewline($"Role level reward created/updated for Tier {tier}, Level {level}.");
        if (grantedRoles.Count > 0)
        {
            responseBuilder.AppendNewline(Markdown.Bold($"Added {"role".ToQuantity(grantedRoles.Count)}:"))
                .AppendJoin(", ", grantedRoles.Select(x => x.Mention))
                .AppendNewline();
        }

        if (revokedRoles.Count > 0)
        {
            responseBuilder.AppendNewline(Markdown.Bold($"Removed {"role".ToQuantity(revokedRoles.Count)}:"))
                .AppendJoin(", ", revokedRoles.Select(x => x.Mention))
                .AppendNewline();
        }

        var members = Bot.GetMembers(Context.GuildId).Values
            .ToList();

        var dbMembers = await db.Members.Where(x => x.GuildId == Context.GuildId)
            .ToListAsync();

        dbMembers = dbMembers.Where(x => x.Tier >= tier && x.Level >= level).ToList();

        var memberXp = dbMembers.ToDictionary(x => x.UserId);

        members = members.Where(x => NeedsApplied(x, memberXp.GetValueOrDefault(x.Id), grantedRoles, revokedRoles))
            .ToList();

        if (members.Count == 0)
        {
            await Response(responseBuilder.ToString());
            return;
        }

        responseBuilder.AppendNewline("Would you like to apply this new level reward retroactively?")
            .AppendNewline($"{"user".ToQuantity(members.Count)} will be affected.");

        var view = new AdminPromptView(responseBuilder.ToString()).OnConfirm("Applying level reward now, this may take awhile...");
        await View(view);

        if (!view.Result)
            return;

        await Response();

        var appliedCount = 0;
        var failedCount = 0;

        _ = Task.Run(async () =>
        {
            foreach (var member in members)
            {
                try
                {
                    await levelReward.ApplyAsync(member);
                    appliedCount++;
                }
                catch
                {
                    failedCount++;
                }
            }
            
            responseBuilder.Clear()
                .AppendNewline("All done!")
                .AppendNewline($"Successful level reward assignments: {appliedCount}")
                .AppendNewline($"Failed level reward assignments: {failedCount}")
                .AppendNewline("(If the failed count is unusually high, you may need to check your " +
                               "role settings or reconfigure this level reward.)");

            await Response(responseBuilder.ToString());
        });

        static bool NeedsApplied(IMember member, Member? xp, IEnumerable<IRole> grantedRoles, IEnumerable<IRole> revokedRoles)
        {
            if (xp is null)
                return false;

            var newRoleIds = member.RoleIds.Except(revokedRoles.Select(x => x.Id))
                .Concat(grantedRoles.Select(x => x.Id))
                .ToList();

            return newRoleIds.Count != member.RoleIds.Count;
        }
    }

    [SlashCommand("remove")]
    [Description("Removes an existing role level reward from this server.")]
    public async Task RemoveAsync(
        [Description("The tier this role level reward is for.")]
        [Minimum(1)]
            int tier,
        [Description("The level (2-150) in the tier this role level reward is for.")]
        [Range(2, 150)]
            int level)
    {
        if (await db.LevelRewards.FindAsync(Context.GuildId, tier, level) is not { } levelReward)
        {
            await Response($"No role level reward exists for Tier {tier}, Level {level}.").AsEphemeral();
            return;
        }

        db.LevelRewards.Remove(levelReward);
        await db.SaveChangesAsync();

        var members = Bot.GetMembers(Context.GuildId).Values
            .ToList();

        var dbMembers = await db.Members.Where(x => x.GuildId == Context.GuildId)
            .ToListAsync();

        dbMembers = dbMembers.Where(x => x.Tier <= tier || x.Level <= level).ToList();

        var memberXp = dbMembers.ToDictionary(x => x.UserId);
        
        members = members.Where(x => NeedsRevoked(x, memberXp.GetValueOrDefault(x.Id), levelReward.GrantedRoleIds, levelReward.RevokedRoleIds))
            .ToList();

        var responseBuilder = new StringBuilder($"Role level reward for tier {tier}, level {level} removed.");
        if (members.Count == 0)
        {
            await Response(responseBuilder.ToString());
            return;
        }

        responseBuilder.AppendNewline()
            .AppendNewline("Would you like to revoke this new level reward retroactively?")
            .AppendNewline($"{"user".ToQuantity(members.Count)} will be affected.");

        var view = new AdminPromptView(responseBuilder.ToString()).OnConfirm("Revoking level reward now, this may take awhile...");;
        await View(view);

        if (!view.Result)
            return;

        var revokedCount = 0;
        var failedCount = 0;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            
            foreach (var member in members)
            {
                try
                {
                    await levelReward.RevokeAsync(member);
                    revokedCount++;
                }
                catch
                {
                    failedCount++;
                }
            }
            
            responseBuilder.Clear()
                .AppendNewline("All done!")
                .AppendNewline($"Successful level reward revocations: {revokedCount}")
                .AppendNewline($"Failed level reward revocations: {failedCount}")
                .AppendNewline("(If the failed count is unusually high, you may need to check your role settings.)");

            await Response(responseBuilder.ToString());
        });

        static bool NeedsRevoked(IMember member, Member? xp, IEnumerable<Snowflake> grantedRoleIds, IEnumerable<Snowflake> revokedRoleIds)
        {
            if (xp is null)
                return false;

            var newRoleIds = member.RoleIds.Except(grantedRoleIds)
                .Concat(revokedRoleIds)
                .ToList();

            return newRoleIds.Count != member.RoleIds.Count;
        }
    }

    [AutoComplete("remove")]
    public async Task AutoCompleteLevelRewardAsync(
        AutoComplete<int> tier,
        AutoComplete<int> level)
    {
        var levelRewards = await db.LevelRewards.Where(x => x.GuildId == Context.GuildId)
            .OrderBy(x => x.Tier)
            .ThenBy(x => x.Level)
            .ToListAsync();

        if (tier.IsFocused)
        {
            var validTiers = levelRewards.Select(x => x.Tier).Distinct().ToList();
            if (validTiers.Count == 0)
            {
                tier.Choices.Add("No level rewards set up!", 1);
                return;
            }

            if (int.TryParse(tier.RawArgument, out var t))
            {
                tier.Choices.Add(validTiers.Contains(t) 
                    ? $"Tier {t}" 
                    : $"No level rewards for Tier {t}", t);

                return;
            }

            tier.Choices.AddRange(validTiers.Take(25).ToDictionary(x => $"Tier {x}"));
        }

        if (level.IsFocused)
        {
            if (tier.Argument.HasValue)
            {
                levelRewards = levelRewards.Where(x => x.Tier == tier.Argument.Value).ToList();

                var validLevels = levelRewards.ToDictionary(x => x.Level, x =>
                {
                    var builder = new StringBuilder($"Tier {x.Tier}, Level {x.Level} - ");
                    
                    if (x.GrantedRoleIds.Count > 0)
                        builder.Append($"{"role".ToQuantity(x.GrantedRoleIds.Count)} added");

                    if (x.GrantedRoleIds.Count > 0 && x.RevokedRoleIds.Count > 0)
                        builder.Append(", ");

                    if (x.RevokedRoleIds.Count > 0)
                        builder.Append($"{"role".ToQuantity(x.RevokedRoleIds.Count)} removed");

                    return builder.ToString();
                });

                if (int.TryParse(level.RawArgument, out var l))
                {
                    level.Choices.Add(validLevels.TryGetValue(l, out var str)
                        ? str
                        : $"No level rewards for Tier {l}", l);

                    return;
                }

                level.Choices.AddRange(validLevels.ToDictionary(x => x.Value, x => x.Key));

                return;
            }

            level.Choices.Add($"Fill out \"{tier}\" first!", 1);
        }
    }

    private static LocalEmbedField FormatField(RoleLevelReward reward)
    {
        var valueBuilder = new StringBuilder();

        if (reward.GrantedRoleIds.Count > 0)
        {
            valueBuilder.Append($"{"granted role".ToQuantity(reward.GrantedRoleIds.Count)}: ")
                .AppendJoinTruncated(", ", reward.GrantedRoleIds.Select(Mention.Role), Discord.Limits.Message.Embed.Field.MaxValueLength / 3)
                .AppendNewline();
        }
                    
        if (reward.RevokedRoleIds.Count > 0)
        {
            valueBuilder.Append($"{"revoked role".ToQuantity(reward.RevokedRoleIds.Count)}: ")
                .AppendJoinTruncated(", ", reward.RevokedRoleIds.Select(Mention.Role), Discord.Limits.Message.Embed.Field.MaxValueLength / 3)
                .AppendNewline();
        }

        return new LocalEmbedField().WithName($"Tier {reward.Tier}, Level {reward.Level}")
            .WithValue(valueBuilder.Length > 0 ? valueBuilder.ToString() : "None");
    }
}