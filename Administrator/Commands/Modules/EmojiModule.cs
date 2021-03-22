using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace Administrator.Commands
{
    [Group("emoji", "em", "e")]
    public class EmojiModule : DiscordModuleBase
    {
        public EmojiService EmojiService { get; set; }
        
        public Random Random { get; set; }
        
        public HttpClient Http { get; set; }

        [Command("", "info")]
        public async Task<DiscordCommandResult> GetEmojiInfoAsync(IEmoji emoji)
        {
            if (emoji is ICustomEmoji customEmoji)
            {
                return Response(new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle($"Info for custom emoji :{customEmoji.Name}:")
                    .WithThumbnailUrl(Discord.Cdn.GetCustomEmojiUrl(customEmoji.Id,
                        customEmoji.IsAnimated)) // TODO: replace this with .GetUrl() or similar when impl'd
                    .AddField("ID", customEmoji.Id.ToString())
                    .AddField("Created", customEmoji.CreatedAt.FormatAsCreated()));
            }

            var mappedEmoji = (MappedEmoji) emoji;
            await using var stream = await EmojiService.GetOrCreateDefaultEmojiAsync(mappedEmoji);
            return Response(new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle($"Info for emoji :{mappedEmoji.PrimaryName}:")
                    .WithThumbnailUrl($"attachment://{mappedEmoji.PrimaryName}.png")
                    .AddField("Alternate names", string.Join('\n', mappedEmoji.NamesWithColons.Select(Markdown.Code)))
                    .WithFooter("This is a default Discord emoji."))
                .AddAttachment(new LocalAttachment(stream, $"{mappedEmoji.PrimaryName}.png"))
                .Build());
        }

        [RequireUserGuildPermissions(Permission.ManageEmojis)]
        [RequireBotGuildPermissions(Permission.ManageEmojis)]
        public sealed class EmojiManagementModule : EmojiModule
        {
            public HttpClient Http { get; set; }

            [Command("create", "add", "new"), RunMode(RunMode.Parallel)]
            [RequireGuild]
            public async Task<DiscordCommandResult> CreateEmojiAsync([Image, MaxSize(256, FileSize.KiB)] Upload upload,
                [Maximum(32), Regex(@"[\w]+")] string name = null)
            {
                using var _ = Context.Bot.BeginTyping(Context.ChannelId);
                name ??= upload.Filename.TrimTo(32);
                
                var isAnimated = upload.Filename.Equals("gif");
                var guild = Context.Bot.GetGuild(Context.GuildId!.Value);
                if (guild.Emojis.Values.Count(x => x.IsAnimated && isAnimated) >= guild.GetEmojiLimit())
                    return Response($"You cannot create any more {(isAnimated ? "animated" : " ")} emojis on this server!");

                using (upload)
                {
                    var stream = await upload.GetStreamAsync();
                    var newEmoji = await guild.CreateEmojiAsync(name, stream);
                    return Response($"New emoji successfully created: {newEmoji}");
                }
            }

            [Command("clone", "steal", "copy")]
            public async Task<DiscordCommandResult> CloneEmojiAsync(ICustomEmoji emoji,
                [Maximum(32), Regex(@"[\w]+")] string newName = null)
            {
                newName ??= emoji.Name;
                
                var guild = Context.Bot.GetGuild(Context.GuildId!.Value);
                if (guild.Emojis.Values.Count(x => x.IsAnimated && emoji.IsAnimated) >= guild.GetEmojiLimit())
                    return Response($"You cannot create any more {(emoji.IsAnimated ? "animated" : " ")} emojis on this server!");

                await using var stream = await Http.GetMemoryStreamAsync(Discord.Cdn.GetCustomEmojiUrl(emoji.Id, emoji.IsAnimated));
                var newEmoji = await guild.CreateEmojiAsync(newName, stream);
                return Response($"New emoji successfully cloned: {newEmoji}");
            }
        }

        [Group("big")]
        public sealed class BigEmojiModule : EmojiModule
        {
            public HttpClient Http { get; set; }
            
            public AdminDbContext Database { get; set; }
            
            public EmojiService EmojiService { get; set; }
            
            [Command, RunMode(RunMode.Parallel)]
            public async Task<DiscordCommandResult> GetBigEmojiAsync(IEmoji emoji)
            {
                using var _ = Context.Bot.BeginTyping(Context.ChannelId);

                var sizeMultiplier = 100;
                if (Context.GuildId.HasValue && await Database.FindAsync<Guild>(Context.GuildId.Value) is { } guild)
                {
                    sizeMultiplier = guild.BigEmojiSizeMultiplier;
                }

                if (emoji is ICustomEmoji customEmoji)
                {
                    var upload = new Upload(Discord.Cdn.GetCustomEmojiUrl(customEmoji.Id, customEmoji.IsAnimated)); // TODO: Replace with GetUrl()
                    await using var customEmojiStream = await Http.GetMemoryStreamAsync(upload.Uri);
                    using var customEmojiImage = Image.Load<Rgba32>(customEmojiStream);
                    customEmojiImage.Mutate(x =>
                        x.Resize(customEmojiImage.Width < customEmojiImage.Height ? 0 : sizeMultiplier,
                            customEmojiImage.Width < customEmojiImage.Height ? sizeMultiplier : 0));
                    
                    await using var customEmojiOutput = new MemoryStream();
                    if (customEmoji.IsAnimated)
                    {
                        await customEmojiImage.SaveAsGifAsync(customEmojiOutput);
                    }
                    else
                    {
                        await customEmojiImage.SaveAsPngAsync(customEmojiOutput);
                    }

                    customEmojiOutput.Seek(0, SeekOrigin.Begin);

                    return Response(new LocalMessageBuilder()
                        .AddAttachment(new LocalAttachment(customEmojiOutput, 
                            $"{customEmoji.Id}.{(customEmoji.IsAnimated ? "gif" : "png")}"))
                        .Build());
                }
                
                var mappedEmoji = (MappedEmoji) emoji;
                await using var mappedEmojiStream = await EmojiService.GetOrCreateDefaultEmojiAsync(mappedEmoji);
                using var mappedEmojiImage = Image.Load<Rgba32>(mappedEmojiStream);
                mappedEmojiImage.Mutate(x =>
                    x.Resize(mappedEmojiImage.Width < mappedEmojiImage.Height ? 0 : sizeMultiplier,
                        mappedEmojiImage.Width < mappedEmojiImage.Height ? sizeMultiplier : 0));
                
                await using var mappedEmojiOutput = new MemoryStream();
                await mappedEmojiImage.SaveAsPngAsync(mappedEmojiOutput);
                mappedEmojiOutput.Seek(0, SeekOrigin.Begin);

                return Response(new LocalMessageBuilder()
                    .AddAttachment(new LocalAttachment(mappedEmojiOutput, $"{mappedEmoji.PrimaryName}.png"))
                    .Build());
            }
        }
    }
}