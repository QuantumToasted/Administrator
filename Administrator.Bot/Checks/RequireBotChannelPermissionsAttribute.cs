﻿using Disqord;
using Disqord.Bot.Commands;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Bot;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RequireBotChannelPermissionsAttribute(Permissions requiredPermissions) : DiscordGuildParameterCheckAttribute
{
    public override bool CanCheck(IParameter parameter, object? value)
        => value is IChannel;

    public override ValueTask<IResult> CheckAsync(IDiscordGuildCommandContext context, IParameter parameter, object? argument)
    {
        var channel = (IChannel)argument!;

        if (context.Bot.GetChannel(context.GuildId, channel.Id) is not IGuildChannel guildChannel)
            return Results.Failure("The provided channel is not a channel in this server (or the cache does not contain it).");

        var permissions = context.Bot.GetCurrentMember(context.GuildId)!.CalculateChannelPermissions(guildChannel);
        
        return permissions.HasFlag(requiredPermissions)
            ? Results.Success
            : Results.Failure($"The bot lacks the necessary permissions ({requiredPermissions & ~permissions}) in the channel {guildChannel.Mention} to execute this.");
    }
}