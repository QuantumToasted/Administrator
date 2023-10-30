using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public static class LuaExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this LuaTable table, TKey key)
        where TKey : notnull
        where TValue : class
    {
        return table.TryGetValue<TKey, TValue>(key, out var value) ? value : null;
    }
    
    public static string SetStringGlobal<T>(this Lua lua, string key, T value)
    {
        lua.SetGlobal(key, value);
        return key;
    }
    
    public static void OpenDiscordLibraries(this Lua lua, IDiscordApplicationGuildCommandContext context)
    {
        lua.OpenLibrary(new DiscordCommandContextLibrary(context));
        lua.OpenLibrary(new DiscordEnumLuaLibrary(context.Bot));
        lua.OpenLibrary(new DiscordRestLuaLibrary(context.Bot, context.GuildId));
    }

    public static LuaTable ConvertEntity<TEntity>(this Lua lua, TEntity entity)
        where TEntity : class, ISnowflakeEntity
    {
        var bot = (DiscordBotBase) entity.Client;
        
        var table = lua.CreateTable();
        
        table.SetValue("id", (long) entity.Id.RawValue);
        table.SetValue("created", entity.CreatedAt().ToUnixTimeSeconds());

        if (entity is INamableEntity { Name: var name })
            table.SetValue("name", name);

        if (entity is ITaggableEntity { Tag: var tag })
            table.SetValue("tag", tag);

        if (entity is IMentionableEntity { Mention: var mention })
            table.SetValue("mention", mention);
        
        switch (entity)
        {
            case IChannel channel:
            {
                table.SetValue("type", channel.Type);

                if (channel is IGuildChannel { Position: var position })
                    table.SetValue("position", position);

                if (channel is ITopicChannel { Topic: { } topic })
                    table.SetValue("topic", topic);

                if (channel is ISlowmodeChannel { Slowmode: var slowmode } && slowmode > TimeSpan.MinValue)
                    table.SetValue("slowmode", (long) slowmode.TotalSeconds);

                if (channel is IAgeRestrictableChannel { IsAgeRestricted: var isAgeRestricted })
                    table.SetValue("ageRestricted", isAgeRestricted);

                if (channel is IThreadChannel { ChannelId: var channelId })
                    table.SetValue("parentId", (long) (ulong) channelId);

                if (channel is ICategorizableGuildChannel { CategoryId: { } categoryId })
                    table.SetValue("categoryId", (long) (ulong) categoryId);

                if (channel is ICategoryChannel categoryChannel)
                {
                    var channelIdsInCategory = bot.GetChannels(categoryChannel.GuildId).Values.OfType<ICategorizableGuildChannel>()
                        .Where(x => x.CategoryId == categoryChannel.Id)
                        .Select(x => x.Id)
                        .ToList();

                    if (channelIdsInCategory.Count > 0)
                    {
                        using (var channels = lua.CreateTable())
                        {
                            for (var i = 0; i < channelIdsInCategory.Count; i++)
                            {
                                channels.SetValue(i + 1, (long) (ulong) channelIdsInCategory[i]);
                            }
                        
                            table.SetValue("channels", channels);
                        }
                    }
                }

                break;
            }
            case IUser user:
            {
                table.SetValue("isBot", user.IsBot);
            
                // no nesting so we can set guild avatar below
                var avatar = lua.CreateTable();
                avatar.SetValue("global", user.GetAvatarUrl(CdnAssetFormat.Automatic));

                if (user is IMember member)
                {
                    avatar.SetValue("guild", member.GetGuildAvatarUrl(CdnAssetFormat.Automatic));
                
                    if (!string.IsNullOrWhiteSpace(member.Nick))
                        table.SetValue("nick", member.Nick);
                
                    if (member.RoleIds.Count > 0)
                    {
                        using (var roles = lua.CreateTable())
                        {
                            for (var i = 0; i < member.RoleIds.Count; i++)
                            {
                                roles.SetValue(i + 1, (long) (ulong) member.RoleIds[i]);
                            }
                        
                            table.SetValue("roles", roles);
                        }
                    }
                
                    table.SetValue("joined", (member.JoinedAt.GetValueOrNullable() ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds());
                    table.SetValue("permissions", (long) (ulong) member.CalculateGuildPermissions());
                }
            
                table.SetValue("avatar", avatar);
                break;
            }
            case IRole role:
            {
                table.SetValue("permissions", (long) (ulong) role.Permissions);
            
                if (role.Color is { } color)
                    table.SetValue("color", color.RawValue);

                var icon = role.GetIconUrl(CdnAssetFormat.Automatic);
                if (!string.IsNullOrWhiteSpace(icon))
                    table.SetValue("icon", icon);
                break;
            }
            case IGuild guild:
            {
                table.SetValue("ownerId", (long) (ulong) guild.OwnerId);
            
                if (!string.IsNullOrWhiteSpace(guild.IconHash))
                    table.SetValue("icon", guild.GetIconUrl(CdnAssetFormat.Automatic));
            
                table.SetValue("members", guild.GetMembers().Count);
                break;
            }
            default:
                throw new NotImplementedException("This entity type hasn't been implemented yet for conversion.");
        }

        return table;
    }
}