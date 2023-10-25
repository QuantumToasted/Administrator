using Administrator.Core;
using Disqord;
using Microsoft.EntityFrameworkCore;
using Qommon;

using static System.Convert;

namespace Administrator.Database.Migration;

public static class DbContextExtensions
{
    public static async Task MigrateAsync(this OldAdminDbContext oldDb, AdminDbContext newDb, IDirectChannelRequester requester)
    {
        var globalUsers = await oldDb.GlobalUsers.ToListAsync();
        foreach (var oldUser in globalUsers)
        {
            var newUser = new Database.GlobalUser(ToUInt64(oldUser.Id))
            {
                TotalXp = oldUser.TotalXp
            };

            newDb.GlobalUsers.Add(newUser);
        }

        await newDb.SaveChangesAsync();

        var guildUsers = await oldDb.GuildUsers.ToListAsync();
        foreach (var oldUser in guildUsers)
        {
            var newUser = new Database.GuildUser(ToUInt64(oldUser.Id), ToUInt64(oldUser.GuildId))
            {
                TotalXp = oldUser.TotalXp
            };
            
            newDb.GuildUsers.Add(newUser);
        }

        await newDb.SaveChangesAsync();

        var guildConfigs = await oldDb.Guilds.ToListAsync();
        var channels = await oldDb.TextChannels.ToListAsync();
        var specialEmojis = await oldDb.SpecialEmojis.ToListAsync();
        foreach (var oldConfig in guildConfigs)
        {
            var newConfig = new Database.Guild(ToUInt64(oldConfig.Id));
            
            var oldSettings = (OldGuildSettings) oldConfig.Settings;
            if (oldSettings.HasFlag(OldGuildSettings.AutoPunishments))
                newConfig.Settings |= GuildSettings.AutomaticPunishmentDetection;
            if (oldSettings.HasFlag(OldGuildSettings.InviteFiltering))
                newConfig.Settings |= GuildSettings.FilterDiscordInvites;
            if (oldSettings.HasFlag(OldGuildSettings.XpTracking))
                newConfig.Settings |= GuildSettings.TrackServerXp;

            foreach (var oldChannel in channels)
            {
                var oldChannelSettings = (OldTextChannelSettings) oldChannel.Settings;
                if (!oldChannelSettings.HasFlag(OldTextChannelSettings.XpTracking))
                    newConfig.XpExemptChannelIds.Add(ToUInt64(oldChannel.Id));
            }

            if (!string.IsNullOrWhiteSpace(oldConfig.Greeting))
            {
                newConfig.GreetingMessage = JsonMessage.FromMessage(new LocalMessage()
                    .WithContent($"[Imported Greeting]:\n{oldConfig.Greeting}"));
            }

            newConfig.DmGreetingMessage = oldConfig.DmGreeting;
            
            if (!string.IsNullOrWhiteSpace(oldConfig.Goodbye))
            {
                newConfig.GoodbyeMessage = JsonMessage.FromMessage(new LocalMessage()
                    .WithContent($"[Imported Goodbye]:\n{oldConfig.Greeting}"));
            }
            
            foreach (var oldEmoji in specialEmojis)
            {
                var oldType = (OldEmojiType) oldEmoji.Type;
                var guildId = ToUInt64(oldEmoji.GuildId);
                if (guildId == newConfig.Id && oldType == OldEmojiType.LevelUp)
                {
                    newConfig.LevelUpEmoji = oldEmoji.Emoji ?? Database.Guild.DEFAULT_LEVEL_UP_EMOJI;
                }
            }

            newDb.Guilds.Add(newConfig);
        }

        await newDb.SaveChangesAsync();

        var punishments = await oldDb.Punishments.ToListAsync();
        foreach (var oldPunishment in punishments)
        {
            Database.Punishment punishment;
            
            switch (oldPunishment.Discriminator)
            {
                case "Kick":
                {
                    punishment = new Kick(ToUInt64(oldPunishment.GuildId),
                        ToUInt64(oldPunishment.TargetId),
                        oldPunishment.TargetName!,
                        ToUInt64(oldPunishment.ModeratorId),
                        oldPunishment.ModeratorName!,
                        oldPunishment.Reason)
                    {
                        Id = oldPunishment.Id,
                        CreatedAt = oldPunishment.CreatedAt
                    };

                    break;
                }
                case "Mute" when oldPunishment.ChannelId.HasValue: // block
                {
                    punishment = new Block(ToUInt64(oldPunishment.GuildId),
                        ToUInt64(oldPunishment.TargetId),
                        oldPunishment.TargetName!,
                        ToUInt64(oldPunishment.ModeratorId),
                        oldPunishment.ModeratorName!,
                        oldPunishment.Reason,
                        ToUInt64(oldPunishment.ChannelId),
                        oldPunishment.CreatedAt + oldPunishment.Duration,
                        oldPunishment.PreviousChannelAllowValue.HasValue
                            ? ToUInt64(oldPunishment.PreviousChannelAllowValue.Value)
                            : null,
                        oldPunishment.PreviousChannelDenyValue.HasValue
                            ? ToUInt64(oldPunishment.PreviousChannelDenyValue.Value)
                            : null)
                    {
                        Id = oldPunishment.Id,
                        CreatedAt = oldPunishment.CreatedAt,
                        RevokedAt = oldPunishment.RevokedAt,
                        RevocationReason = oldPunishment.RevocationReason,
                        RevokerId = oldPunishment.RevokerId.HasValue
                            ? ToUInt64(oldPunishment.RevokerId.Value)
                            : null,
                        RevokerName = oldPunishment.RevokerName,
                        AppealedAt = oldPunishment.AppealedAt,
                        AppealText = oldPunishment.AppealReason,
                    };
                    
                    break;
                }
                case "Mute":
                {
                    var timeout = new Timeout(ToUInt64(oldPunishment.GuildId),
                            ToUInt64(oldPunishment.TargetId),
                            oldPunishment.TargetName!,
                            ToUInt64(oldPunishment.ModeratorId),
                            oldPunishment.ModeratorName!,
                            oldPunishment.Reason,
                            DateTimeOffset.UtcNow) // temporary value
                        {
                            Id = oldPunishment.Id,
                            CreatedAt = oldPunishment.CreatedAt,
                            RevokedAt = oldPunishment.RevokedAt,
                            RevocationReason = oldPunishment.RevocationReason,
                            RevokerId = oldPunishment.RevokerId.HasValue
                                ? ToUInt64(oldPunishment.RevokerId.Value)
                                : null,
                            RevokerName = oldPunishment.RevokerName,
                            AppealedAt = oldPunishment.AppealedAt,
                            AppealText = oldPunishment.AppealReason,
                        };

                    DateTimeOffset? expires = oldPunishment.Duration.HasValue
                        ? new DateTimeOffset(oldPunishment.CreatedAt + oldPunishment.Duration.Value)
                        : null;
                    
                    if (!oldPunishment.RevokedAt.HasValue && 
                        (!expires.HasValue || expires.Value - DateTimeOffset.UtcNow > TimeSpan.FromDays(28))) // maximum timeout duration
                    {
                        timeout = timeout with
                        {
                            ExpiresAt = DateTimeOffset.UtcNow,
                            RevokedAt = DateTimeOffset.UtcNow,
                            RevocationReason = "Migration produced a mute with too long or permanent duration.",
                            RevokerId = timeout.ModeratorId,
                            RevokerName = timeout.ModeratorName
                        };
                    }

                    punishment = timeout;
                    break;
                }
                case "Warning":
                {
                    var warning = new Warning(ToUInt64(oldPunishment.GuildId),
                            ToUInt64(oldPunishment.TargetId),
                            oldPunishment.TargetName!,
                            ToUInt64(oldPunishment.ModeratorId),
                            oldPunishment.ModeratorName!,
                            oldPunishment.Reason) // temporary value
                        {
                            Id = oldPunishment.Id,
                            CreatedAt = oldPunishment.CreatedAt,
                            RevokedAt = oldPunishment.RevokedAt,
                            RevocationReason = oldPunishment.RevocationReason,
                            RevokerId = oldPunishment.RevokerId.HasValue
                                ? ToUInt64(oldPunishment.RevokerId.Value)
                                : null,
                            RevokerName = oldPunishment.RevokerName,
                            AppealedAt = oldPunishment.AppealedAt,
                            AppealText = oldPunishment.AppealReason,
                            AdditionalPunishmentId = oldPunishment.SecondaryPunishmentId
                        };

                    if (oldPunishment.LogMessageChannelId != decimal.Zero)
                    {
                        warning.LogChannelId = ToUInt64(oldPunishment.LogMessageChannelId);
                    }
                    
                    if (oldPunishment.LogMessageId != decimal.Zero)
                    {
                        warning.LogMessageId = ToUInt64(oldPunishment.LogMessageId);
                    }

                    punishment = warning;
                    break;
                }
                case "Ban":
                {
                    punishment = new Ban(ToUInt64(oldPunishment.GuildId),
                            ToUInt64(oldPunishment.TargetId),
                            oldPunishment.TargetName!,
                            ToUInt64(oldPunishment.ModeratorId),
                            oldPunishment.ModeratorName!,
                            oldPunishment.Reason,
                            7,
                            oldPunishment.CreatedAt + oldPunishment.Duration) // temporary value
                        {
                            Id = oldPunishment.Id,
                            CreatedAt = oldPunishment.CreatedAt,
                            RevokedAt = oldPunishment.RevokedAt,
                            RevocationReason = oldPunishment.RevocationReason,
                            RevokerId = oldPunishment.RevokerId.HasValue
                                ? ToUInt64(oldPunishment.RevokerId.Value)
                                : null,
                            RevokerName = oldPunishment.RevokerName,
                            AppealedAt = oldPunishment.AppealedAt,
                            AppealText = oldPunishment.AppealReason
                        };

                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            
            if (oldPunishment.Image is not null && oldPunishment.Format > 0)
            {
                var format = (OldImageFormat) oldPunishment.Format;
                punishment.Attachment = new Attachment(oldPunishment.Image,
                    $"attachment.{format.ToString().ToLower()}");
            }

            if (punishment is RevocablePunishment revocablePunishment)
            {
                revocablePunishment.AppealStatus = AppealStatus.Ignored;
            }

            newDb.Punishments.Add(punishment);
        }
        
        await newDb.SaveChangesAsync();

        var reminders = await oldDb.Reminders.ToListAsync();
        var dmChannelCache = new Dictionary<Snowflake, IDirectChannel>();
        foreach (var oldReminder in reminders)
        {
            var newReminder = new Database.Reminder(oldReminder.Text ?? "[imported reminder]",
                ToUInt64(oldReminder.AuthorId),
                default, // temporary value
                oldReminder.Ending,
                null,
                null)
            {
                Id = oldReminder.Id,
                CreatedAt = oldReminder.CreatedAt
            };

            if (oldReminder.ChannelId.HasValue)
            {
                newReminder = newReminder with { ChannelId = ToUInt64(oldReminder.ChannelId.Value) };
            }
            else
            {
                if (!dmChannelCache.TryGetValue(newReminder.AuthorId, out var dmChannel))
                {
                    try
                    {
                        dmChannel = await requester.FetchDirectChannelAsync(newReminder.AuthorId);
                        dmChannelCache[newReminder.AuthorId] = dmChannel;
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                newReminder = newReminder with { ChannelId = dmChannel.Id };
            }

            newDb.Reminders.Add(newReminder);
        }
        
        await newDb.SaveChangesAsync();

        var loggingChannels = await oldDb.LoggingChannels.ToListAsync();
        var addedChannels = new HashSet<(Snowflake GuildId, LogEventType Type)>();
        foreach (var oldChannel in loggingChannels)
        {
            var newChannel = new Database.LoggingChannel(ToUInt64(oldChannel.GuildId), 
                default, // temporary value
                ToUInt64(oldChannel.Id));

            var oldType = (OldLogType) oldChannel.Type;
            LogEventType? newType = oldType switch
            {
                OldLogType.Ban => LogEventType.Ban,
                OldLogType.Kick => LogEventType.Kick,
                OldLogType.Mute => LogEventType.Timeout,
                OldLogType.Warn => LogEventType.Warning,
                OldLogType.Revoke => LogEventType.Revoke,
                OldLogType.Appeal => LogEventType.Appeal,
                OldLogType.MessageDelete => LogEventType.MessageDelete,
                OldLogType.MessageUpdate => LogEventType.MessageUpdate,
                OldLogType.Join => LogEventType.Join,
                OldLogType.Leave => LogEventType.Leave,
                OldLogType.UsernameUpdate => LogEventType.NameUpdate,
                OldLogType.NicknameUpdate => LogEventType.NameUpdate,
                OldLogType.AvatarUpdate => LogEventType.AvatarUpdate,
                OldLogType.UserRoleUpdate => LogEventType.UserRoleUpdate,
                // OldLogType.Starboard => LogEventType.Starboard, // starboard deprecated
                OldLogType.Greeting => LogEventType.Greeting,
                OldLogType.Goodbye => LogEventType.Goodbye,
                OldLogType.BotAnnouncements => LogEventType.BotAnnouncements,
                OldLogType.Errors => LogEventType.Errors,
                _ => null
            };

            if (!newType.HasValue)
                continue;

            newChannel = newChannel with { EventType = newType.Value };

            // don't duplicate
            if (!addedChannels.Add((newChannel.GuildId, newChannel.EventType)))
                continue;

            newDb.LoggingChannels.Add(newChannel);
        }
        
        await newDb.SaveChangesAsync();

        var levelRewards = await oldDb.LevelRewards.ToListAsync();
        foreach (var oldReward in levelRewards)
        {
            var grantedRoleIds = !string.IsNullOrWhiteSpace(oldReward.AddedRoleIds)
                ? oldReward.AddedRoleIds
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(ulong.Parse).ToHashSet()
                : new HashSet<ulong>();
            
            var revokedRoleIds = !string.IsNullOrWhiteSpace(oldReward.RemovedRoleIds)
                ? oldReward.RemovedRoleIds
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(ulong.Parse).ToHashSet()
                : new HashSet<ulong>();

            var newReward = new RoleLevelReward(ToUInt64(oldReward.GuildId),
                oldReward.Tier,
                oldReward.Level)
            {
                GrantedRoleIds = grantedRoleIds,
                RevokedRoleIds = revokedRoleIds
            };

            newDb.LevelRewards.Add(newReward);
        }

        await newDb.SaveChangesAsync();

        var warningPunishments = await oldDb.WarningPunishments.ToListAsync();
        foreach (var oldWarningPunishment in warningPunishments)
        {
            var oldType = (OldPunishmentType) oldWarningPunishment.Type;
            var newWarningPunishment = new Database.WarningPunishment(
                ToUInt64(oldWarningPunishment.GuildId),
                oldWarningPunishment.Count,
                oldType switch
                {
                    OldPunishmentType.Mute => PunishmentType.Timeout,
                    OldPunishmentType.Kick => PunishmentType.Kick,
                    OldPunishmentType.Ban => PunishmentType.Ban,
                    _ => throw new ArgumentOutOfRangeException()
                },
                oldType == OldPunishmentType.Mute && oldWarningPunishment.Duration is null
                    ? TimeSpan.FromDays(28)
                    : oldWarningPunishment.Duration);

            newDb.WarningPunishments.Add(newWarningPunishment);
        }
        
        await newDb.SaveChangesAsync();
        
        var reactionRoles = await oldDb.ReactionRoles.ToListAsync();
        var positionDict = new Dictionary<Snowflake, (int CurrentRow, int CurrentPosition)>();
        foreach (var oldReactionRole in reactionRoles)
        {
            var messageId = ToUInt64(oldReactionRole.MessageId);
            var newButtonRole = new ButtonRole(ToUInt64(oldReactionRole.GuildId),
                ToUInt64(oldReactionRole.ChannelId),
                messageId,
                GetRow(),
                GetPosition(),
                oldReactionRole.Emoji!,
                null,
                ToUInt64(oldReactionRole.RoleId));

            newDb.ButtonRoles.Add(newButtonRole);
            continue;

            int GetRow()
            {
                if (!positionDict.TryGetValue(messageId, out var tuple))
                {
                    positionDict[messageId] = (1, 1);
                    return 1;
                }

                if (tuple.CurrentRow == 5)
                {
                    positionDict[messageId] = (tuple.CurrentRow + 1, 1);
                }
                else
                {
                    positionDict[messageId] = (tuple.CurrentRow + 1, tuple.CurrentPosition);
                }

                return tuple.CurrentRow + 1;
            }

            int GetPosition()
            {
                if (!positionDict.TryGetValue(messageId, out var tuple))
                {
                    positionDict[messageId] = (1, 1);
                    return 1;
                }

                Guard.IsLessThanOrEqualTo(tuple.CurrentPosition, 5);

                positionDict[messageId] = (tuple.CurrentRow + 1, tuple.CurrentPosition);
                return tuple.CurrentRow + 1;
            }
        }

        await newDb.SaveChangesAsync();
    }
}