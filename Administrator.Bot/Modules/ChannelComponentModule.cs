using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Gateway;
using Disqord.Rest;
using Humanizer;
using Qommon;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

public sealed class ChannelComponentModule : DiscordComponentGuildModuleBase
{
    [ModalCommand("Channel:Create:Text")]
    public Task<IResult> CreateTextChannelAsync(string name, string? topic = null, string? categoryName = null)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        return CreateChannelAsync(Bot.CreateTextChannelAsync(Context.GuildId, name, x =>
        {
            x.Topic = topic ?? Optional<string>.Empty;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
        }));
    }

    [ModalCommand("Channel:Create:Voice")]
    public Task<IResult> CreateVoiceChannelAsync(string name, string? categoryName = null)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        return CreateChannelAsync(Bot.CreateVoiceChannelAsync(Context.GuildId, name, x =>
        {
            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
        }));
    }

    [ModalCommand("Channel:Create:Category")]
    public Task<IResult> CreateCategoryChannelAsync(string name)
        => CreateChannelAsync(Bot.CreateCategoryChannelAsync(Context.GuildId, name));

    [ModalCommand("Channel:Modify:Text:*")]
    public Task<IResult> ModifyTextChannelAsync(Snowflake channelId, string name, string? topic = null, string? categoryName = null)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannelAsync(Bot.ModifyTextChannelAsync(channelId, x =>
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

    [ModalCommand("Channel:Modify:Voice:*")]
    public Task<IResult> ModifyVoiceChannelAsync(Snowflake channelId, string name, string? categoryName = null)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());
        
        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannelAsync(Bot.ModifyVoiceChannelAsync(channelId, x =>
        {
            x.Name = name;

            if (categoryId.HasValue)
                x.CategoryId = categoryId.Value;
            else if (channel?.CategoryId.HasValue == true && string.IsNullOrWhiteSpace(categoryName))
                x.CategoryId = null;
        }));
    }

    [ModalCommand("Channel:Modify:Category:*")]
    public Task<IResult> ModifyCategoryChannelAsync(Snowflake channelId, string name)
    {
        return ModifyChannelAsync(Bot.ModifyCategoryChannelAsync(channelId, x =>
        {
            x.Name = name;
        }));
    }

    [ModalCommand("Channel:Modify:Forum:*")]
    public Task<IResult> ModifyForumChannelAsync(Snowflake channelId, string name, string? topic = null, string? categoryName = null)
    {
        if (HasValidCategoryName(categoryName, out var categoryId) == false)
            return Task.FromResult<IResult>(Response($"No category channel was found with the name \"{categoryName}\".").AsEphemeral());

        var channel = Bot.GetChannel(Context.GuildId, channelId) as ICategorizableGuildChannel;
        return ModifyChannelAsync(Bot.ModifyForumChannelAsync(channelId, x =>
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

    private async Task<IResult> CreateChannelAsync<TChannel>(Task<TChannel> createTask)
        where TChannel : IGuildChannel
    {
        var channel = await createTask;
        return Response($"New {channel.Type.Humanize(LetterCasing.LowerCase)} channel {channel.Mention} has been created!");
    }

    private async Task<IResult> ModifyChannelAsync<TChannel>(Task<TChannel> modifyTask)
        where TChannel : IGuildChannel
    {
        var modifiedChannel = await modifyTask;
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