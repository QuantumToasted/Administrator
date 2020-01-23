using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Common.LocalizedEmbed;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Rest;
using Humanizer.Localisation;
using Qmmands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Administrator.Commands.Emoji
{
    [Name("Emoji")]
    [Group("emoji", "e")]
    public class EmojiCommands : AdminModuleBase
    {
        public HttpClient Http { get; set; }

        public PaginationService Pagination { get; set; }

        [Command("", "info")]
        public async ValueTask<AdminCommandResult> GetEmojiInfoAsync([RequireCustomEmoji] IEmoji emoji)
        {
            var customEmoji = (LocalCustomEmoji) emoji;
            var ownedGuild = Context.Client.Guilds.Values.FirstOrDefault(x => x.Emojis.ContainsKey(customEmoji.Id));
            var builder = new LocalizedEmbedBuilder(this)
                .WithSuccessColor()
                .WithThumbnailUrl(customEmoji.GetUrl())
                .WithLocalizedTitle("emoji_info_title", customEmoji.Name)
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("info_id")
                    .WithValue(customEmoji.Id))
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("info_created")
                    .WithValue(string.Join('\n', customEmoji.Id.CreatedAt.ToString("G", Context.Language.Culture),
                        (DateTimeOffset.UtcNow - customEmoji.Id.CreatedAt).HumanizeFormatted(Localization,
                            Context.Language, TimeUnit.Second, true))))
                .AddField(new LocalizedFieldBuilder(this)
                    .WithLocalizedName("emoji_info_animated")
                    .WithValue(customEmoji.IsAnimated ? EmojiTools.Checkmark : EmojiTools.X));

            if (ownedGuild is { })
            {
                if (!Context.IsPrivate)
                {
                    var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                    if (guild.BlacklistedEmojiGuilds.Contains(ownedGuild.Id))
                    {
                        builder.WithFooter(new LocalizedFooterBuilder(this)
                            .WithLocalizedText("emoji_info_owner_blacklisted", ownedGuild.Format(false, false))
                            .WithIconUrl(ownedGuild.GetIconUrl()));
                    }
                    else
                    {
                        builder.WithFooter(new LocalizedFooterBuilder(this)
                            .WithLocalizedText("emoji_info_owner", ownedGuild.Format(false, false))
                            .WithIconUrl(ownedGuild.GetIconUrl()));
                    }
                }
                else
                {
                    builder.WithFooter(new LocalizedFooterBuilder(this)
                        .WithLocalizedText("emoji_info_owner", ownedGuild.Format(false, false))
                        .WithIconUrl(ownedGuild.GetIconUrl()));
                }
            }

            return CommandSuccess(embed: builder.Build());
        }

        [Command("big"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> GetBigEmojiAsync(IEmoji emoji)
        {
            var size = 256;
            if (!Context.IsPrivate)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);
                if (emoji is LocalCustomEmoji customEmoji && 
                    Context.Client.Guilds.Values.FirstOrDefault(x => x.Emojis.ContainsKey(customEmoji.Id)) is { } emojiGuild &&
                    guild.BlacklistedEmojiGuilds.Contains(emojiGuild.Id))
                {
                    return CommandSuccess(); // TODO: Silent failure?
                }

                size = guild.BigEmojiSize;
            }

            using var _ = Context.Channel.Typing();
            var url = EmojiTools.GetUrl(emoji);

            using var response = await Http.GetAsync(url);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return CommandErrorLocalized("emojiparser_notfound");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();

            using var image = Image.Load<Rgba32>(stream);
            image.Mutate(x => x.Resize(size, size * image.Height / image.Width));

            var output = new MemoryStream();
            string extension;
            if ((emoji as LocalCustomEmoji)?.IsAnimated == true)
            {
                image.SaveAsGif(output);
                extension = "gif";
            }
            else
            {
                image.SaveAsPng(output);
                extension = "png";
            }

            output.Seek(0, SeekOrigin.Begin);

            return CommandSuccess(attachment: new LocalAttachment(output, $"emoji.{extension}"));
        }

        [RequireUserPermissions(Permission.ManageEmojis)]
        [RequireBotPermissions(Permission.ManageEmojis)]
        public sealed class ManageEmojiCommands : EmojiCommands
        {
            [Command("create")]
            public async ValueTask<AdminCommandResult> CreateEmojiAsync(
                [Remainder, Alphanumeric, MustBe(StringLength.ShorterThan, 32)]
                string name = null)
            {
                if (!(Context.Message.Attachments.FirstOrDefault(x => x.FileName.HasImageExtension(out _)) is { }
                    attachment))
                    return CommandErrorLocalized("emoji_create_noimage");

                if (!attachment.FileName.HasImageExtension(out var format))
                    throw new Exception(); // what the fuck

                if (Context.Guild.Emojis.Values.Count(x =>
                        format == ImageFormat.Gif && x.IsAnimated || !x.IsAnimated) == Context.Guild.GetEmojiLimit())
                {
                    return CommandErrorLocalized(format == ImageFormat.Gif
                        ? "emoji_maximum_animated"
                        : "emoji_maximum");
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = string.Join('.', attachment.FileName.Split('.')[..^2]);
                    var builder = new StringBuilder();
                    foreach (var c in name)
                    {
                        if (char.IsLetterOrDigit(c))
                            builder.Append(c);
                    }

                    name = builder.ToString().TrimTo(32);
                }

                using var response = await Http.GetAsync(attachment.Url);
                await using var stream = await response.Content.ReadAsStreamAsync();

                try
                {
                    var newEmoji = await Context.Guild.CreateEmojiAsync(stream, name);
                    return CommandSuccessLocalized("emoji_create_success", args: newEmoji.MessageFormat);
                }
                catch (DiscordHttpException ex) when (ex.JsonErrorCode == JsonErrorCode.InvalidFormBody)
                {
                    return CommandErrorLocalized("emoji_image_too_large");
                }
            }

            [Command("clone", "steal")]
            public async ValueTask<AdminCommandResult> CloneEmojiAsync([RequireCustomEmoji] IEmoji emoji,
                [Remainder, Alphanumeric, MustBe(StringLength.ShorterThan, 32)]
                string newName = null)
            {
                var customEmoji = (LocalCustomEmoji) emoji;

                if (Context.Guild.Emojis.Values.Count(x => customEmoji.IsAnimated) ==
                    Context.Guild.GetEmojiLimit())
                    return CommandErrorLocalized(customEmoji.IsAnimated ? "emoji_maximum" : "emoji_maximum_animated");

                if (Context.Guild.Emojis.Values.Any(x => x.Id == customEmoji.Id))
                    return CommandErrorLocalized("emoji_clone_exists");

                using var response = await Http.GetAsync(customEmoji.GetUrl());
                await using var stream = await response.Content.ReadAsStreamAsync();

                try
                {
                    var newEmoji = await Context.Guild.CreateEmojiAsync(stream, newName ?? customEmoji.Name);
                    return CommandSuccessLocalized("emoji_create_success", args: newEmoji.MessageFormat);
                }
                catch (DiscordHttpException ex) when (ex.JsonErrorCode == JsonErrorCode.InvalidFormBody)
                {
                    return CommandErrorLocalized("emoji_image_too_large");
                }
            }

            [Command("blacklist")]
            public async ValueTask<AdminCommandResult> BlacklistEmojiGuildAsync(ulong guildId)
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);

                var removed = guild.BlacklistedEmojiGuilds.Remove(guildId);
                if (!removed)
                {
                    guild.BlacklistedEmojiGuilds.Add(guildId);
                }

                Context.Database.Guilds.Update(guild);
                await Context.Database.SaveChangesAsync();

                return CommandSuccessLocalized(removed ? "emoji_blacklist_remove" : "emoji_blacklist_add",
                    args: new object[] {guildId, Markdown.Code($"{Context.Prefix}e big")});
            }

            [Command("blacklist")]
            public async ValueTask<AdminCommandResult> GetEmojiBlacklistAsync()
            {
                var guild = await Context.Database.GetOrCreateGuildAsync(Context.Guild.Id);

                if (guild.BlacklistedEmojiGuilds.Count == 0)
                    return CommandErrorLocalized("emoji_blacklist_none");

                var dict = guild.BlacklistedEmojiGuilds.ToDictionary(x => x,
                    x => Context.Client.GetGuild(x)?.Format(false) ?? $"??? (`{x}`)");

                var pages = DefaultPaginator.GeneratePages(guild.BlacklistedEmojiGuilds,
                    lineFunc: x => dict[x], builderFunc: () => new LocalizedEmbedBuilder(this)
                        .WithSuccessColor()
                        .WithLocalizedTitle("emoji_blacklist"));

                if (pages.Count > 1)
                {
                    await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                    return CommandSuccess();
                }

                return CommandSuccess(embed: pages[0].Embed);
            }
        }
    }
}