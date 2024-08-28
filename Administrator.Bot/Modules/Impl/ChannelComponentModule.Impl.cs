using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public sealed partial class ChannelComponentModule : DiscordComponentGuildModuleBase
{
    public partial Task<IResult> CreateTextChannel(string name, string? topic, string? categoryName)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        return CreateChannel(Bot.CreateTextChannelAsync(Context.GuildId, name, x =>
        {
            x.Topic = topic ?? Optional<string>.Empty;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
        }));
    }

    public partial Task<IResult> CreateVoiceChannel(string name, string? categoryName)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        return CreateChannel(Bot.CreateVoiceChannelAsync(Context.GuildId, name, x =>
        {
            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
        }));
    }

    public partial Task<IResult> CreateCategoryChannel(string name)
        => CreateChannel(Bot.CreateCategoryChannelAsync(Context.GuildId, name));

    public partial Task<IResult> ModifyTextChannel(Snowflake channelId, string name, string? topic, string? categoryName)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannel(Bot.ModifyTextChannelAsync(channelId, x =>
        {
            x.Name = name;

            if (!string.IsNullOrWhiteSpace(topic))
                x.Topic = topic;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
            else if (channel?.CategoryId.HasValue == true && string.IsNullOrWhiteSpace(categoryName))
                x.CategoryId = null;
        }));
    }

    public partial Task<IResult> ModifyVoiceChannel(Snowflake channelId, string name, string? categoryName)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());
        
        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannel(Bot.ModifyVoiceChannelAsync(channelId, x =>
        {
            x.Name = name;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
            else if (channel?.CategoryId.HasValue == true && string.IsNullOrWhiteSpace(categoryName))
                x.CategoryId = null;
        }));
    }

    public partial Task<IResult> ModifyCategoryChannel(Snowflake channelId, string name)
    {
        return ModifyChannel(Bot.ModifyCategoryChannelAsync(channelId, x =>
        {
            x.Name = name;
        }));
    }

    public partial Task<IResult> ModifyForumChannel(Snowflake channelId, string name, string? topic, string? categoryName)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannel(Bot.ModifyForumChannelAsync(channelId, x =>
        {
            x.Name = name;

            if (!string.IsNullOrWhiteSpace(topic))
                x.Topic = topic;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
            else if (channel?.CategoryId.HasValue == true && string.IsNullOrWhiteSpace(categoryName))
                x.CategoryId = null;
        }));
    }

    private async Task<IResult> CreateChannel<TChannel>(Task<TChannel> createTask)
        where TChannel : IGuildChannel
    {
        var channel = await createTask;
        return Response($"New {channel.Type.Humanize(LetterCasing.LowerCase)} channel {channel.Mention} has been created!");
    }

    private async Task<IResult> ModifyChannel<TChannel>(Task<TChannel> modifyTask)
        where TChannel : IGuildChannel
    {
        TChannel modifiedChannel;
        
        try
        {
            modifiedChannel = await modifyTask;
        }
        catch (MaximumRateLimitDelayExceededException ex)
        {
            return Response($"Modifying this channel has been rate-limited. Try again " +
                            $"{Markdown.Timestamp(DateTimeOffset.UtcNow + ex.Delay, Markdown.TimestampFormat.RelativeTime)}.");
        }
        
        return Response($"Channel {modifiedChannel.Mention} modified.");
    }

    private bool? HasValidCategoryName(string? categoryName, out Snowflake? categoryId)
    {
        categoryId = null;

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            categoryId = Bot.GetChannels(Context.GuildId).Values.FirstOrDefault(y => y.Name.Equals(categoryName))?.Id;
            return categoryId.HasValue;
        }

        return null;
    }
}