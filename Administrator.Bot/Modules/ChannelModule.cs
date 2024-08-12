using System.Text;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Gateway;
using Disqord.Rest;
using Disqord.Rest.Api;
using Humanizer;
using Humanizer.Localisation;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("channel")]
[RequireInitialAuthorPermissions(Permissions.ManageChannels)]
[RequireBotPermissions(Permissions.ManageChannels)]
public sealed class ChannelModule(EmojiService emojiService) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("info")]
    [Description("Displays information about a channel.")]
    public IResult DisplayInfo(
        [Description("The channel to display information for.")]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel)
    {
        return Response(FormatChannelInfo(channel));
    }

    [SlashGroup("create")]
    public sealed class ChannelCreateModule : DiscordApplicationGuildModuleBase
    {
        [SlashCommand("text")]
        [Description("Creates a new text channel.")]
        public Task CreateTextChannelAsync()
            => Context.Interaction.Response().SendModalAsync(FormatModal(ChannelType.Text));

        [SlashCommand("voice")]
        [Description("Creates a new voice channel.")]
        public Task CreateVoiceChannelAsync()
            => Context.Interaction.Response().SendModalAsync(FormatModal(ChannelType.Voice));

        [SlashCommand("category")]
        [Description("Creates a new category channel.")]
        public Task CreateCategoryChannelAsync()
            => Context.Interaction.Response().SendModalAsync(FormatModal(ChannelType.Category));

        private static LocalInteractionModalResponse FormatModal(ChannelType type)
        {
            var modal = new LocalInteractionModalResponse()
                .WithTitle($"Create new {type.Humanize(LetterCasing.LowerCase)} channel")
                .WithCustomId($"Channel:Create:{type}")
                .AddComponent(LocalComponent.Row(
                    LocalComponent.TextInput("name", "Channel name", TextInputComponentStyle.Short)
                        .WithIsRequired()
                        .WithPlaceholder("Enter the name for the new channel...")));

            if (type == ChannelType.Text)
            {
                modal.AddComponent(LocalComponent.Row(LocalComponent.TextInput("topic", "Channel topic", TextInputComponentStyle.Paragraph).WithIsRequired(false)));
            }

            if (type != ChannelType.Category)
            {
                modal.AddComponent(LocalComponent.Row(LocalComponent.TextInput("categoryName", "Category Name", TextInputComponentStyle.Short).WithIsRequired(false)));
            }

            return modal;
        }
    }

    [SlashCommand("clone")]
    [Description("Clones an existing channel.")]
    public async Task<IResult> CloneAsync(
        [Description("The channel to clone.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel,
        [Description("The name of the new channel. Defaults to the original channel's name.")]
            string? name = null)
    {
        var guild = Context.Bot.GetGuild(Context.GuildId)!;
        if (channel.Type == ChannelType.Forum && !guild.GetFeatures().HasCommunity)
            return Response("Forum channels cannot be created on this server!").AsEphemeral();
        
        IGuildChannel? actualChannel = channel.Type switch
        {
            ChannelType.Forum => Bot.GetChannel(Context.GuildId, channel.Id) as IForumChannel,
            ChannelType.Text => Bot.GetChannel(Context.GuildId, channel.Id) as ITextChannel,
            ChannelType.Voice => Bot.GetChannel(Context.GuildId, channel.Id) as IVoiceChannel,
            ChannelType.Category => Bot.GetChannel(Context.GuildId, channel.Id) as ICategoryChannel,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        if (actualChannel is null)
            return Response("Unable to fetch information for that channel for cloning. Sorry about that!").AsEphemeral();

        name ??= actualChannel.Name;

        IGuildChannel newChannel = actualChannel switch
        {
            IForumChannel forumChannel => await Bot.CreateForumChannelAsync(Context.GuildId, name, x =>
            {
                x.Tags = forumChannel.Tags.Select(y => new LocalForumTag
                    {Emoji = LocalEmoji.FromEmoji(y.Emoji)!, IsModerated = y.IsModerated, Name = y.Name}).ToList();

                x.DefaultAutomaticArchiveDuration = forumChannel.DefaultAutomaticArchiveDuration;

                if (forumChannel.DefaultReactionEmoji is not null)
                    x.DefaultReactionEmoji = LocalEmoji.FromEmoji(forumChannel.DefaultReactionEmoji)!;

                if (forumChannel.Slowmode > TimeSpan.Zero)
                    x.Slowmode = forumChannel.Slowmode;
                
                if (!string.IsNullOrWhiteSpace(forumChannel.Topic))
                    x.Topic = forumChannel.Topic;

                if (forumChannel.DefaultThreadSlowmode > TimeSpan.Zero)
                    x.DefaultThreadSlowmode = forumChannel.DefaultThreadSlowmode;

                x.IsAgeRestricted = forumChannel.IsAgeRestricted;

                if (forumChannel.CategoryId.HasValue)
                    x.CategoryId = forumChannel.CategoryId.Value;

                //x.Flags = forumChannel.Flags;

                x.Overwrites = forumChannel.Overwrites.Select(LocalOverwrite.CreateFrom).ToList();
            }),
            ITextChannel textChannel => await Bot.CreateTextChannelAsync(Context.GuildId, name, x =>
            {
                if (!string.IsNullOrWhiteSpace(textChannel.Topic))
                    x.Topic = textChannel.Topic;

                x.DefaultAutomaticArchiveDuration = textChannel.DefaultAutomaticArchiveDuration;

                x.IsAgeRestricted = textChannel.IsAgeRestricted;

                x.IsNews = textChannel.IsNews;

                if (textChannel.Slowmode > TimeSpan.Zero)
                    x.Slowmode = textChannel.Slowmode;

                if (textChannel.CategoryId.HasValue)
                    x.CategoryId = textChannel.CategoryId.Value;

                //x.Flags = textChannel.Flags;

                x.Overwrites = textChannel.Overwrites.Select(LocalOverwrite.CreateFrom).ToList();
            }),
            IVoiceChannel voiceChannel => await Bot.CreateVoiceChannelAsync(Context.GuildId, name, x =>
            {
                if (voiceChannel.MemberLimit > 0)
                    x.MemberLimit = voiceChannel.MemberLimit;

                x.Bitrate = voiceChannel.Bitrate;

                x.IsAgeRestricted = voiceChannel.IsAgeRestricted;
                
                // voice channel regions are normally set automatically anyway, right??
                //if (!string.IsNullOrWhiteSpace(voiceChannel.Region))
                //    x.Region = voiceChannel.Region

                if (voiceChannel.Slowmode > TimeSpan.Zero)
                    x.Slowmode = voiceChannel.Slowmode;

                x.VideoQualityMode = voiceChannel.VideoQualityMode;

                if (voiceChannel.CategoryId.HasValue)
                    x.CategoryId = voiceChannel.CategoryId.Value;

                //x.Flags = voiceChannel.Flags;

                x.Overwrites = voiceChannel.Overwrites.Select(LocalOverwrite.CreateFrom).ToList();
            }),
            ICategoryChannel categoryChannel => await Bot.CreateCategoryChannelAsync(Context.GuildId, name, x =>
            {
                //x.Flags = categoryChannel.Flags;
                x.Overwrites = categoryChannel.Overwrites.Select(LocalOverwrite.CreateFrom).ToList();
            }),
            _ => throw new ArgumentOutOfRangeException()
        };

        await Bot.ReorderChannelsAsync(Context.GuildId, new Dictionary<Snowflake, int>
            {[newChannel.Id] = actualChannel.Position});

        return Response($"{actualChannel.Mention} has been cloned into the new channel {newChannel.Mention}.");
    }

    [SlashCommand("modify")]
    [Description("Modifies an existing channel.")]
    public async Task<IResult> ModifyAsync(
        [Description("The channel to modify.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel)
    {
        IGuildChannel? actualChannel = channel.Type switch
        {
            ChannelType.Forum => Bot.GetChannel(Context.GuildId, channel.Id) as IForumChannel,
            ChannelType.Text => Bot.GetChannel(Context.GuildId, channel.Id) as ITextChannel,
            ChannelType.Voice => Bot.GetChannel(Context.GuildId, channel.Id) as IVoiceChannel,
            ChannelType.Category => Bot.GetChannel(Context.GuildId, channel.Id) as ICategoryChannel,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (actualChannel is null)
            return Response("Unable to fetch information for that channel for cloning. Sorry about that!").AsEphemeral();

        var modal = new LocalInteractionModalResponse()
            .WithTitle($"Modify {actualChannel.Type.Humanize(LetterCasing.LowerCase)} channel {actualChannel.Name}".Truncate(Discord.Limits.Interaction.Modal.MaxTitleLength))
            .WithCustomId($"Channel:Modify:{actualChannel.Type}:{actualChannel.Id}")
            .AddComponent(LocalComponent.Row(LocalComponent.TextInput("name", "Channel Name", TextInputComponentStyle.Short).WithIsRequired().WithPrefilledValue(actualChannel.Name)));

        if (actualChannel is ITopicChannel channelWithTopic)
        {
            var topicComponent = LocalComponent.TextInput("topic", "Topic", TextInputComponentStyle.Paragraph).WithIsRequired(false);

            if (!string.IsNullOrWhiteSpace(channelWithTopic.Topic))
                topicComponent.WithPrefilledValue(channelWithTopic.Topic);

            modal.AddComponent(LocalComponent.Row(topicComponent));
        }

        if (actualChannel is ICategorizableGuildChannel channelWithCategory)
        {
            var categoryComponent = LocalComponent.TextInput("categoryName", "Category Name", TextInputComponentStyle.Short).WithIsRequired(false);

            if (channelWithCategory.CategoryId is { } categoryId && Bot.GetChannel(Context.GuildId, categoryId) is { } categoryChannel)
                categoryComponent.WithPrefilledValue(categoryChannel.Name);

            modal.AddComponent(LocalComponent.Row(categoryComponent));
        }

        await Context.Interaction.Response().SendModalAsync(modal);
        return default!;
    }

    [SlashCommand("move")]
    [Description("Moves a channel above or below another channel.")]
    public async Task<IResult> MoveAsync(
        [Description("The channel to move above/below target-channel.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channelToMove,
        [Description("Whether to move channel-to-move above or below target-channel.")]
            MoveDirection direction,
        [Description("The channel to move channel-to-move above/below.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel targetChannel)
    {
        if (channelToMove.Id == targetChannel.Id)
            return Response("You can't move a channel above or below itself!").AsEphemeral();
        
        IGuildChannel? actualChannelToMove = channelToMove.Type switch
        {
            ChannelType.Forum => Bot.GetChannel(Context.GuildId, channelToMove.Id) as IForumChannel,
            ChannelType.Text => Bot.GetChannel(Context.GuildId, channelToMove.Id) as ITextChannel,
            ChannelType.Voice => Bot.GetChannel(Context.GuildId, channelToMove.Id) as IVoiceChannel,
            ChannelType.Category => Bot.GetChannel(Context.GuildId, channelToMove.Id) as ICategoryChannel,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        IGuildChannel? actualTargetChannel = targetChannel.Type switch
        {
            ChannelType.Forum => Bot.GetChannel(Context.GuildId, targetChannel.Id) as IForumChannel,
            ChannelType.Text => Bot.GetChannel(Context.GuildId, targetChannel.Id) as ITextChannel,
            ChannelType.Voice => Bot.GetChannel(Context.GuildId, targetChannel.Id) as IVoiceChannel,
            ChannelType.Category => Bot.GetChannel(Context.GuildId, targetChannel.Id) as ICategoryChannel,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (actualTargetChannel is null)
            return Response("Unable to fetch information for `target-channel`'s position. Sorry about that!").AsEphemeral();

        if ((actualChannelToMove as ICategorizableGuildChannel)?.CategoryId != (actualTargetChannel as ICategorizableGuildChannel)?.CategoryId)
            return Response("You can't move a channel in a different category above or below another channel!").AsEphemeral();

        if (actualTargetChannel.Position == 0 && direction == MoveDirection.Above)
        {
            await Bot.ReorderChannelsAsync(Context.GuildId, new Dictionary<Snowflake, int>
            {
                [channelToMove.Id] = 1, [targetChannel.Id] = 0
            });
        }
        else
        {
            await Bot.ReorderChannelsAsync(Context.GuildId, new Dictionary<Snowflake, int>
            {
                [channelToMove.Id] = actualTargetChannel.Position + (direction == MoveDirection.Above ? -1 : 1) // channels are ordered opposite of roles: 0 = top
            });
        }

        return Response($"{Mention.Channel(channelToMove.Id)} has been moved {direction.Humanize(LetterCasing.LowerCase)} {Mention.Channel(targetChannel.Id)}.");
    }

    [SlashCommand("delete")]
    [Description("Deletes an existing channel permanently.")]
    public async Task DeleteAsync(
        [Description("The channel to delete.")]
        [ChannelTypes(ChannelType.Forum, ChannelType.Text, ChannelType.Voice, ChannelType.Category)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel channel)
    {
        var view = new AdminPromptView(
                $"Are you sure you want to delete the channel {Mention.Channel(channel.Id)}?\n" +
                $"This action is {Markdown.Bold("IRREVERSIBLE")}.", FormatChannelInfo(channel))
            .OnConfirm($"Channel {channel.Name} ({channel.Id}) deleted.");

        await View(view);
        if (view.Result)
            await channel.DeleteAsync();
    }

    [SlashCommand("slowmode")]
    [Description("Modifies a channel's slowmode (how often users can send messages).")]
    public async Task<IResult> ModifySlowmodeAsync(
        [Description("The channel having its slowmode modified. Defaults to the current channel.")]
        [ChannelTypes(ChannelType.Text, ChannelType.Voice, ChannelType.PrivateThread, ChannelType.PublicThread)]
        [AuthorCanViewChannel]
        [BotCanViewChannel]
            IChannel? channel = null,
        [Description("The duration between messages. Supply nothing to disable slowmode.")]
            TimeSpan? duration = null)
    {
        channel ??= Bot.GetChannel(Context.GuildId, Context.ChannelId)!;

        try
        {
            await Bot.ApiClient.ModifyChannelAsync(channel.Id, new ModifyChannelJsonRestRequestContent
            {
                RateLimitPerUser = duration.HasValue
                    ? (int)duration.Value.TotalSeconds
                    : 0
            });

            return Response(duration.HasValue
                ? $"{Mention.Channel(channel.Id)}'s slowmode has been updated. Users will be able to send a message every " +
                  $"{duration.Value.Humanize(int.MaxValue, minUnit: TimeUnit.Second)}."
                : $"{Mention.Channel(channel.Id)}'s slowmode has been removed.");
        }
        catch (Exception ex)
        {
            return Response($"Failed to set the channel's slowmode. The below error may help?\n{Markdown.CodeBlock(ex.Message)}").AsEphemeral();
        }
    }

    private LocalEmbed FormatChannelInfo(IChannel basicChannel)
    {
        var embed = new LocalEmbed()
            .WithTitle($"Information for {basicChannel.Type.Humanize(LetterCasing.LowerCase)} channel {basicChannel.Name}");

        if (Bot.GetChannel(Context.GuildId, basicChannel.Id) is not { } channel)
        {
            return embed.WithCollectorsColor()
                .WithDescription("Could not fetch detailed information for this channel. Sorry!");
        }

        embed.WithUnusualColor()
            .AddField("ID", channel.Id.ToString(), true)
            .AddField("Created", Markdown.Timestamp(channel.CreatedAt(), Markdown.TimestampFormat.RelativeTime), true);

        if (channel is IMentionableEntity mentionable)
            embed.AddField("Mention", mentionable.Mention, true);

        if (channel is ITopicChannel { Topic: { } topic })
            embed.WithDescription(Markdown.Italics(topic.Truncate(Discord.Limits.Message.Embed.MaxDescriptionLength - 5)));

        if (channel is ISlowmodeChannel slowmodeChannel)
            embed.AddField("Slowmode", slowmodeChannel.Slowmode == TimeSpan.Zero ? "(no slowmode)" : $"1 message every {slowmodeChannel.Slowmode.Humanize(int.MaxValue, maxUnit: TimeUnit.Hour)}", true);

        if (channel is IAgeRestrictableChannel ageRestrictableChannel)
            embed.AddField("Age restricted?", ageRestrictableChannel.IsAgeRestricted ? emojiService.Names["white_check_mark"] : emojiService.Names["x"], true);

        if (channel is ICategorizableGuildChannel {CategoryId: { } categoryId} && Bot.GetChannel(Context.GuildId, categoryId) is { } category)
            embed.AddField("Category", category.Name);

        switch (channel)
        {
            case CachedCategoryChannel categoryChannel:
            {
                var channelsInCategory = Bot.GetChannels(Context.GuildId).Values
                    .OfType<ICategorizableGuildChannel>()
                    .Where(x => x.CategoryId == categoryChannel.Id).ToList();

                var field = new LocalEmbedField()
                    .WithName($"Channels inside this category ({channelsInCategory.Count})");

                field = channelsInCategory.Count > 0
                    ? field.WithValue(new StringBuilder().AppendJoinTruncated(", ",
                        channelsInCategory.Select(x => x.Mention), Discord.Limits.Message.Embed.Field.MaxValueLength))
                    : field.WithBlankValue();

                embed.AddField(field);
                break;
            }
            case CachedForumChannel forumChannel:
            {
                embed.AddField("Tags",
                    new StringBuilder().AppendJoinTruncated(", ", forumChannel.Tags.Select(x => $"{x.Emoji}{x.Name}"),
                        Discord.Limits.Message.Embed.Field.MaxValueLength));
                break;
            }
            case CachedThreadChannel threadChannel:
            {
                embed.AddField("Parent channel", Mention.Channel(threadChannel.ChannelId))
                    .AddField("Creator", Mention.User(threadChannel.CreatorId));
                break;
            }
            case CachedVoiceChannel voiceChannel:
            {
                embed.AddField("Bitrate", $"{voiceChannel.Bitrate / 1000}kbps", true);

                var membersInVoiceChannel = Bot.GetVoiceStates(Context.GuildId).Values
                    .Where(x => x.ChannelId == voiceChannel.Id).ToList();

                var actualMemberLimit = voiceChannel.MemberLimit == 0 ? $"{voiceChannel.MemberLimit}" : "∞";
                var field = new LocalEmbedField()
                    .WithName($"Connected members ({membersInVoiceChannel.Count}/{actualMemberLimit})");

                field = membersInVoiceChannel.Count > 0
                    ? field.WithValue(new StringBuilder().AppendJoinTruncated(", ",
                        membersInVoiceChannel.Select(x =>
                            Bot.GetMember(Context.GuildId, x.MemberId)?.Tag ?? Mention.User(x.MemberId)),
                        Discord.Limits.Message.Embed.Field.MaxValueLength))
                    : field.WithBlankValue();

                embed.AddField(field);
                break;
            }
        }

        // prevent a 400 by just...removing the last field until there's room
        while (embed.Length >= Discord.Limits.Message.MaxEmbeddedContentLength)
        {
            embed.Fields.Value.RemoveAt(embed.Fields.Value.Count - 1);
        }

        return embed;
    }
}