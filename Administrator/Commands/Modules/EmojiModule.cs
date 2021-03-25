using System;
using System.Collections.Generic;
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
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Disqord.Rest;
using Qmmands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Image = SixLabors.ImageSharp.Image;

namespace Administrator.Commands
{
    [Group("emoji", "em", "e")]
    public sealed class EmojiModule : DiscordModuleBase
    {
        public EmojiService EmojiService { get; set; }
        
        public Random Random { get; set; }
        
        public HttpClient Http { get; set; }

        [Command("", "info")]
        [RunMode(RunMode.Parallel)]
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
            var stream = await EmojiService.GetOrCreateDefaultEmojiAsync(mappedEmoji);
            return Response(new LocalMessageBuilder()
                .WithEmbed(new LocalEmbedBuilder()
                    .WithSuccessColor()
                    .WithTitle($"Info for emoji :{mappedEmoji.PrimaryName}\u200b:") // needed so the emoji doesn't render in the embed
                    .WithThumbnailUrl($"attachment://{mappedEmoji.PrimaryName}.png")
                    .AddField("Alternate names", string.Join('\n', mappedEmoji.NamesWithColons.Select(Markdown.Code)))
                    .WithFooter("This is a default Discord emoji."))
                .AddAttachment(new LocalAttachment(stream, $"{mappedEmoji.PrimaryName}.png"))
                .Build());
        }

        [RequireUserGuildPermissions(Permission.ManageEmojis)]
        [RequireBotGuildPermissions(Permission.ManageEmojis)]
        public sealed class EmojiManagementModule : DiscordGuildModuleBase
        {
            public HttpClient Http { get; set; }
            
            [Command("create", "add", "new"), RunMode(RunMode.Parallel)]
            public async Task<DiscordCommandResult> CreateEmojiAsync([Image, MaxSize(256, FileSize.KiB)] Upload upload,
                [Maximum(32), Regex(@"^[\w]+$")] string name = null)
            {
                using var _ = Context.Bot.BeginTyping(Context.ChannelId);
                name ??= upload.Filename.TrimTo(32);
                
                var isAnimated = upload.Filename.Equals("gif");
                if (Context.Guild.Emojis.Values.Count(x => x.IsAnimated && isAnimated) >= Context.Guild.GetEmojiLimit())
                    return Response($"You cannot create any more {(isAnimated ? "animated" : " ")} emojis on this server!");

                using (upload)
                {
                    var stream = await upload.GetStreamAsync();
                    var newEmoji = await Context.Guild.CreateEmojiAsync(name, stream);
                    return Response($"New emoji successfully created: {newEmoji}");
                }
            }

            [Command("clone", "steal", "copy")]
            public async Task<DiscordCommandResult> CloneEmojiAsync(ICustomEmoji emoji,
                [Maximum(32), Regex(@"^[\w]+$")] string newName = null)
            {
                newName ??= emoji.Name;
                
                if (Context.Guild.Emojis.Values.Count(x => x.IsAnimated && emoji.IsAnimated) >= Context.Guild.GetEmojiLimit())
                    return Response($"You cannot create any more {(emoji.IsAnimated ? "animated" : " ")} emojis on this server!");

                await using var stream = await Http.GetMemoryStreamAsync(Discord.Cdn.GetCustomEmojiUrl(emoji.Id, emoji.IsAnimated));
                var newEmoji = await Context.Guild.CreateEmojiAsync(newName, stream);
                return Response($"New emoji successfully cloned: {newEmoji}");
            }
        }

        [Group("big")]
        public sealed class BigEmojiModule : DiscordModuleBase
        {
            public AdminDbContext Database { get; set; }
            
            public HttpClient Http { get; set; }
            
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
                            customEmojiImage.Width < customEmojiImage.Height ? sizeMultiplier : 0,
                            new NearestNeighborResampler()));
                    
                    var customEmojiOutput = new MemoryStream();
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
                        mappedEmojiImage.Width < mappedEmojiImage.Height ? sizeMultiplier : 0,
                        new NearestNeighborResampler()));
                
                var mappedEmojiOutput = new MemoryStream();
                await mappedEmojiImage.SaveAsPngAsync(mappedEmojiOutput);
                mappedEmojiOutput.Seek(0, SeekOrigin.Begin);

                return Response(new LocalMessageBuilder()
                    .AddAttachment(new LocalAttachment(mappedEmojiOutput, $"{mappedEmoji.PrimaryName}.png"))
                    .Build());
            }

            [Command("list")]
            [Priority(1)]
            public async Task ListEmojisAsync([Minimum(1)] int page = 1)
            {
                var allEmojis = await Database.GetAllBigEmojisAsync();
                var approvedEmojis = allEmojis.OfType<ApprovedBigEmoji>().ToList();
                var interactivity = Context.Bot.GetRequiredExtension<InteractivityExtension>();

                if (approvedEmojis.Count == 0)
                {
                    await Response("No emojis have been added to the global whitelist.");
                    return;
                }

                var check = await new RequireBotOwnerAttribute().CheckAsync(Context);
                var menu = new EmojiListMenu(Context.Author.Id, approvedEmojis, page, check.IsSuccessful);
                await interactivity.StartMenuAsync(Context.ChannelId, menu);
            }

            [Command("list")]
            public async Task ListEmojisAsync([Regex(@"^[\w]+$")] string startingName)
            {
                var allEmojis = await Database.GetAllBigEmojisAsync();
                var approvedEmojis = allEmojis.OfType<ApprovedBigEmoji>().ToList();
                var interactivity = Context.Bot.GetRequiredExtension<InteractivityExtension>();

                if (approvedEmojis.Count == 0)
                {
                    await Response("No emojis have been added to the global whitelist.");
                    return;
                }

                var check = await new RequireBotOwnerAttribute().CheckAsync(Context);
                var menu = new EmojiListMenu(Context.Author.Id, approvedEmojis, startingName, check.IsSuccessful);
                await interactivity.StartMenuAsync(Context.ChannelId, menu);
            }

            [Group("request")]
            public sealed class BigEmojiRequestModule : DiscordModuleBase
            {
                private List<BigEmoji> _existingEmojis;
                
                public AdminDbContext Database { get; set; }

                protected override async ValueTask BeforeExecutedAsync()
                {
                    if (!Context.GuildId.HasValue)
                    {
                        _existingEmojis = new List<BigEmoji>();
                    }
                    else
                    {
                        var allEmojis = await Database.GetAllBigEmojisAsync();
                        _existingEmojis = allEmojis.Where(x => x.GuildId == Context.GuildId.Value).ToList();
                    }
                }

                [Command]
                [RequireUserGuildPermissions(Permission.ManageEmojis)]
                public async Task<DiscordCommandResult> RequestEmojisAsync(params IGuildEmoji[] emojis)
                {
                    if (emojis.Length == 0)
                    {
                        return Response("You haven't supplied any emojis to request!\n" +
                                        "If you wish to request all emojis from this server, try using " +
                                        $"{Markdown.Code($"{string.Join(' ', Context.Path)} request")} instead.");
                    }

                    var addableEmojis = new List<IGuildEmoji>();

                    foreach (var emoji in emojis)
                    {
                        if (emoji.GuildId != Context.GuildId) // ??????
                        {
                            return Response(
                                $"The emoji {emoji} is not from this server, so you are not allowed to request it, " +
                                "unless you use the command in that server.");
                        }

                        if (EmojiService.Names.ContainsKey(emoji.Name.ToLowerInvariant()))
                        {
                            return Response($"The emoji {emoji} has the same name as a default Discord emoji.\n" +
                                            "To request this emoji, rename it to something else.");
                        }

                        if (_existingEmojis.All(x => x.Id != emoji.Id))
                        {
                            addableEmojis.Add(emoji);
                        }
                    }
                    
                    if (addableEmojis.Count == 0)
                    {
                        return Response(
                            "All the emojis you are requesting have already been requested, approved, or denied.");
                    }

                    return await RequestEmojisAsync(addableEmojis);
                }

                [Command("all")]
                [RequireUserGuildPermissions(Permission.ManageEmojis)]
                public async Task<DiscordCommandResult> RequestAllEmojisAsync()
                {
                    var guild = Context.Bot.GetGuild(Context.GuildId!.Value);
                    if (guild.Emojis.Count == 0)
                        return Response("You haven't created any emojis on this server!");

                    var addableEmojis = new List<IGuildEmoji>();

                    foreach (var (id, emoji) in guild.Emojis)
                    {
                        if (EmojiService.Names.ContainsKey(emoji.Name.ToLowerInvariant()))
                        {
                            return Response($"The emoji {emoji} has the same name as a default Discord emoji.\n" +
                                            "To request this emoji, rename it to something else.");
                        }

                        if (_existingEmojis.All(x => x.Id != emoji.Id))
                        {
                            addableEmojis.Add(emoji);
                        }
                    }
                    
                    if (addableEmojis.Count == 0)
                    {
                        return Response(
                            "All the emojis you are requesting have already been requested, approved, or denied.");
                    }

                    return await RequestEmojisAsync(addableEmojis);
                }

                private async Task<DiscordCommandResult> RequestEmojisAsync(ICollection<IGuildEmoji> emojis)
                {
                    foreach (var emoji in emojis)
                    {
                        Database.BigEmojis.Add(RequestedBigEmoji.Create(emoji, Context.Author));
                    }

                    await Database.SaveChangesAsync();
                    return Response(
                        $"You've successfully put in a request for {emojis.Count} emoji(s) to be added to the whitelist.");
                }

                [Command("queue", "list")]
                [RequireBotOwner]
                public async Task ListRequestedEmojisAsync()
                {
                    var allEmojis = await Database.GetAllBigEmojisAsync();
                    var requestedEmojis = allEmojis.OfType<RequestedBigEmoji>().ToList();

                    if (requestedEmojis.Count == 0)
                    {
                        await Response("There are no emojis in the whitelist queue. Good work!");
                        return;
                    }

                    var menu = new EmojiRequestMenu(Context.Author.Id, requestedEmojis);
                    var interactivity = Context.Bot.GetRequiredExtension<InteractivityExtension>();
                    await interactivity.StartMenuAsync(Context.ChannelId, menu);
                }
            }
        }
    }
}