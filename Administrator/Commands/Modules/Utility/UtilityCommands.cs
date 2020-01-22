using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Extensions;
using Disqord;
using Disqord.Rest;
using Newtonsoft.Json;
using Qmmands;

namespace Administrator.Commands.Utility
{
    [Name("Utility")]
    [RequireContext(ContextType.Guild)]
    public sealed class UtilityCommands : AdminModuleBase
    {
        public HttpClient Http { get; set; }

        public Random Random { get; set; }

        [Command("send", "say")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public ValueTask<AdminCommandResult> SendMessage([Remainder] string text = null)
            => SendMessageAsync((CachedTextChannel) Context.Channel, text);

        [Command("send", "say")]
        [RequireUserPermissions(Permission.ManageMessages)]
        [Priority(1)]
        public async ValueTask<AdminCommandResult> SendMessageAsync(CachedTextChannel channel, [Remainder] string text = null)
        {
            if (string.IsNullOrWhiteSpace(text) && Context.Message.Attachments.Count == 0)
                return CommandErrorLocalized("utility_send_empty");

            if (!string.IsNullOrWhiteSpace(text))
                text = await text.FormatPlaceHoldersAsync(Context, random: Random);

            var file = new MemoryStream();
            var filename = string.Empty;
            if (Context.Message.Attachments.FirstOrDefault() is { } attachment)
            {
                await using var stream = await Http.GetStreamAsync(attachment.Url);
                await stream.CopyToAsync(file);
                file.Seek(0, SeekOrigin.Begin);
                filename = attachment.FileName;
            }

            if (JsonEmbed.TryParse(text, out var embed))
            {
                await channel.SendMessageAsync(
                    !string.IsNullOrWhiteSpace(filename) ? new LocalAttachment(file, filename) : null, embed.Text,
                    embed: embed.ToLocalEmbed());
                return CommandSuccess();
            }

            if (!string.IsNullOrWhiteSpace(filename))
                await channel.SendMessageAsync(new LocalAttachment(file, filename), text);
            else await channel.SendMessageAsync(text);
            return CommandSuccess();
        }

        [Command("edit")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public ValueTask<AdminCommandResult> EditMessage(RestUserMessage jumpLink, [Remainder] string text)
            => EditMessageAsync((CachedTextChannel)Context.Channel, jumpLink.Id, text);

        [Command("edit")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public ValueTask<AdminCommandResult> EditMessage(ulong messageId, [Remainder] string text)
            => EditMessageAsync((CachedTextChannel) Context.Channel, messageId, text);

        [Command("edit")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public async ValueTask<AdminCommandResult> EditMessageAsync(CachedTextChannel channel, ulong messageId,
            [Remainder] string text)
        {
            if (!(await channel.GetMessageAsync(messageId) is RestUserMessage message))
                return CommandErrorLocalized("utility_edit_nomessage");

            if (message.Author.Id != Context.Client.CurrentUser.Id)
                return CommandErrorLocalized("utility_edit_nonowner");

            text = await text.FormatPlaceHoldersAsync(Context, random: Random);

            await message.ModifyAsync(x =>
            {
                if (JsonEmbed.TryParse(text, out var embed))
                {
                    x.Content = embed.Text;
                    x.Embed = embed.ToLocalEmbed();
                    return;
                }

                x.Content = text;
            });

            await Context.Message.AddReactionAsync(EmojiTools.Checkmark);
            return CommandSuccess();
        }

        [Command("deconstruct")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public ValueTask<AdminCommandResult> DeconstructMessage(ulong messageId)
            => DeconstructMessageAsync((CachedTextChannel) Context.Channel, messageId);

        [Command("deconstruct")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public async ValueTask<AdminCommandResult> DeconstructMessageAsync(CachedTextChannel channel, ulong messageId)
        {
            if (!(await channel.GetMessageAsync(messageId) is RestUserMessage message))
                return CommandErrorLocalized("utility_deconstruct_nomessage");

            return await DeconstructMessageAsync(message);
        }

        [Command("deconstruct")]
        [RequireUserPermissions(Permission.ManageMessages)]
        public async ValueTask<AdminCommandResult> DeconstructMessageAsync(RestUserMessage jumpLink)
        {
            JsonEmbed embed;
            if (jumpLink.Embeds.FirstOrDefault() is { } messageEmbed)
            {
                embed = new JsonEmbed(jumpLink.Content, LocalEmbedBuilder.FromEmbed(messageEmbed).Build());
            }
            else
            {
                embed = new JsonEmbed(jumpLink.Content, null);
            }

            if (string.IsNullOrWhiteSpace(embed.Text))
            {
                embed = new JsonEmbed(null, embed.ToLocalEmbed()); // useless extra step
            }

            var json = JsonConvert.SerializeObject(embed, Formatting.None,
                new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});

            var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream, leaveOpen: true);

            await writer.WriteAsync(json);
            await writer.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);

            return CommandSuccess(attachment: new LocalAttachment(stream, $"{jumpLink.Id}.json"));
        }

        [Command("quote")]
        public ValueTask<AdminCommandResult> Quote(ulong messageId)
            => QuoteAsync((CachedTextChannel) Context.Channel, messageId);

        [Command("quote")]
        public async ValueTask<AdminCommandResult> QuoteAsync(CachedTextChannel channel, ulong messageId)
        {
            if (!(await channel.GetMessageAsync(messageId) is RestUserMessage message))
                return CommandErrorLocalized("utility_quote_nomessage");

            return Quote(message);
        }

        [Command("quote")]
        public AdminCommandResult Quote(RestUserMessage message)
        {
            var channel = Context.Guild.GetTextChannel(message.ChannelId);
            var content = !string.IsNullOrWhiteSpace(message.Content)
                ? message.Content
                : message.Embeds.FirstOrDefault()?.Description;
            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithAuthor(Localize("highlight_trigger_author", message.Author.Tag.Sanitize(), channel.Tag), message.Author.GetAvatarUrl())
                .WithDescription($"{content}\n\n" + Markdown.Link(Localize("info_jumpmessage"),
                                     $"https://discordapp.com/channels/{Context.Guild.Id}/{channel.Id}/{message.Id}"))
                .WithImageUrl(message.Attachments.FirstOrDefault(x => x.FileName.HasImageExtension(out _))?.Url ??
                              message.Embeds.FirstOrDefault()?.Image?.Url)
                .Build());
        }
    }
}