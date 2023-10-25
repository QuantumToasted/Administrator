using System.Text;
using System.Text.RegularExpressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Administrator.Bot;

public sealed class XpService(IConfiguration configuration) : DiscordBotService
{
    private static readonly Regex LevelEmojiNameRegex = new(@"tier_(?<tier>[\d]+)_level_(?<level>[\d]+)", RegexOptions.Compiled);

    private readonly Dictionary<(int Tier, int Level), Snowflake> _levelEmojis = new();

    private readonly IList<ulong> _levelEmojiGuilds = configuration.GetSection("LevelEmojiGuilds").Get<List<ulong>>() ?? new List<ulong>();

    public LocalCustomEmoji GetLevelEmoji(int tier, int level)
        => LocalEmoji.Custom(_levelEmojis.TryGetValue((tier, level), out var id) ? id : 729577737156558918); // <:LEVELNOTFOUND:729577737156558918>

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Bot.WaitUntilReadyAsync(stoppingToken);

        foreach (var guildId in _levelEmojiGuilds)
        {
            if (Bot.GetGuild(guildId) is not { } guild)
            {
                Logger.LogWarning("Emoji guild {GuildId} was not found! Emojis not loaded.", guildId);
                continue;
            }

            foreach (var emoji in guild.Emojis.Values)
            {
                if (!LevelEmojiNameRegex.IsMatch(emoji.Name, out var match))
                    continue;

                var tier = int.Parse(match.Groups["tier"].Value);
                var level = int.Parse(match.Groups["level"].Value);

                _levelEmojis[(tier, level)] = emoji.Id;
            }
        }

        Logger.LogInformation("Loaded {Emojis}.", "level-up emoji".ToQuantity(_levelEmojis.Count));
    }

    protected override async ValueTask OnMessageReceived(BotMessageReceivedEventArgs e)
    {
        if (e.GuildId is not { } guildId || 
            e.Message is not IGatewayUserMessage message ||
            message.Author.IsBot || string.IsNullOrEmpty(message.Content))
        {
            return;
        }

        await using var scope = Bot.Services.CreateAsyncScopeWithDatabase(out var db);

        var guildConfig = await db.GetOrCreateGuildConfigAsync(guildId);

        var globalUser = await db.GetOrCreateGlobalUserAsync(message.Author.Id);
        globalUser = globalUser.IncrementXp(User.XP_INCREMENT_RATE, User.XpGainInterval, out var globalLeveledUp);
        
        if (globalLeveledUp)
        {
            _ = Task.Run(async () =>
            {
                await message.AddReactionAsync(LocalEmoji.Unicode("🌐"));
                await message.AddReactionAsync(GetLevelEmoji(globalUser.Tier, globalUser.Level));
            });
        }

        if (guildConfig.HasSetting(GuildSettings.TrackServerXp) && !guildConfig.XpExemptChannelIds.Contains(e.ChannelId))
        {
            var guildUser = await db.GetOrCreateGuildUserAsync(guildId, message.Author.Id);
            guildUser = guildUser.IncrementXp(guildConfig.CustomXpRate ?? User.XP_INCREMENT_RATE, guildConfig.CustomXpInterval ?? User.XpGainInterval, out var guildLeveledUp);
            
            if (guildLeveledUp)
            {
                _ = Task.Run(async () =>
                {
                    await message.AddReactionAsync(LocalEmoji.FromString(guildConfig.LevelUpEmoji));
                    await message.AddReactionAsync(GetLevelEmoji(globalUser.Tier, globalUser.Level));
                });

                if (await db.LevelRewards.FindAsync(guildId, guildUser.Tier, guildUser.Level) is { } levelReward)
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
                        .AppendNewline(
                            $"Congrats on leveling up to {Markdown.Bold($"Tier {guildUser.Tier}, Level {guildUser.Level}")}!");

                        if (levelReward.GrantedRoleIds.Count > 0)
                        {
                            contentBuilder.Append($"You've been given the following {"role".ToQuantity(levelReward.GrantedRoleIds.Count)}: ");
                            var grantedRoles = new List<IRole>();
                            foreach (var roleId in levelReward.GrantedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is { } role)
                                {
                                    grantedRoles.Add(role);
                                    continue;
                                }

                                levelReward.GrantedRoleIds.Remove(roleId);
                            }

                            contentBuilder.AppendJoin(", ", grantedRoles.Select(x => Markdown.Bold(x.Name)))
                                .AppendNewline();
                        }

                        if (levelReward.RevokedRoleIds.Count > 0)
                        {
                            contentBuilder.Append($"You've had the following {"role".ToQuantity(levelReward.GrantedRoleIds.Count)} removed: ");
                            var revokedRoles = new List<IRole>();
                            foreach (var roleId in levelReward.RevokedRoleIds)
                            {
                                if (Bot.GetRole(guildId, roleId) is { } role)
                                {
                                    revokedRoles.Add(role);
                                    continue;
                                }

                                levelReward.RevokedRoleIds.Remove(roleId);
                            }

                            contentBuilder.AppendJoin(", ", revokedRoles.Select(x => Markdown.Bold(x.Name)));
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

                        _ = message.Author.SendMessageAsync(new LocalMessage().WithContent(contentBuilder.ToString()));
                    }
                }
            }
        }

        await db.SaveChangesAsync();
    }
}