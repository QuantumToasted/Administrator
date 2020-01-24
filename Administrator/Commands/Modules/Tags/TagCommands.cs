using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Administrator.Common;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Humanizer.Localisation;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Commands.Tags
{
    [Name("Tags")]
    [Group("tag")]
    [RequireContext(ContextType.Guild)]
    public sealed class TagCommands : AdminModuleBase
    {
        public HttpClient Http { get; set; }

        public PaginationService Pagination { get; set; }

        [Command("list")]
        public async ValueTask<AdminCommandResult> ListPersonalTagsAsync()
        {
            var tags = await Context.Database.Tags.Where(x =>
                    x.GuildId == Context.Guild.Id && x.AuthorId == Context.User.Id)
                .ToListAsync();

            if (tags.Count == 0)
                return CommandErrorLocalized("tag_list_none");

            var pages = DefaultPaginator.GeneratePages(tags, lineFunc: tag => tag.Name,
                builderFunc: () => new LocalEmbedBuilder().WithSuccessColor()
                    .WithTitle(Localize("tag_list_title", Context.Guild.Name.Sanitize())));

            if (pages.Count > 1)
            {
                await Pagination.SendPaginatorAsync(Context.Channel, new DefaultPaginator(pages, 0), pages[0]);
                return CommandSuccess();
            }

            return CommandSuccess(embed: pages[0].Embed);
        }

        [Command, RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> ShowTagAsync([Remainder, Lowercase] string name)
        {
            if (!(await Context.Database.Tags.FindAsync(Context.Guild.Id.RawValue, name) is { } tag))
                return CommandErrorLocalized("tag_notfound");

            tag.Uses++;
            Context.Database.Tags.Update(tag);
            await Context.Database.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(tag.Response))
                tag.Response = await tag.Response.FormatPlaceHoldersAsync(Context);

            if (JsonEmbed.TryParse(tag.Response ?? string.Empty, out var embed))
            {
                return CommandSuccess(embed.Text, embed.ToLocalEmbed(),
                    tag.Format != ImageFormat.Default
                        ? new LocalAttachment(tag.Image, $"attachment.{tag.Format.ToString().ToLower()}")
                        : null);
            }

            return CommandSuccess(tag.Response, attachment: tag.Format != ImageFormat.Default
                ? new LocalAttachment(tag.Image, $"attachment.{tag.Format.ToString().ToLower()}")
                : null);
        }

        [Command("create")]
        public async ValueTask<AdminCommandResult> CreateTagAsync([Lowercase, MustBe(StringLength.ShorterThan, 50), Replace("\n", "")] string name, 
            [Remainder] string response = null)
        {
            if (await Context.Database.Tags.FindAsync(Context.Guild.Id.RawValue, name) is { })
                return CommandErrorLocalized("tag_exists");

            if (string.IsNullOrWhiteSpace(response) &&
                !Context.Message.Attachments.Any(x => x.FileName.HasImageExtension(out _)))
                return CommandErrorLocalized("tag_noresponse");

            var image = new MemoryStream();
            var format = ImageFormat.Default;
            if (Context.Message.Attachments.FirstOrDefault() is { } attachment &&
                attachment.FileName.HasImageExtension(out format))
            {
                await using var stream = await Http.GetStreamAsync(attachment.Url);
                await stream.CopyToAsync(image);
                image.Seek(0, SeekOrigin.Begin);
            }

            Context.Database.Tags.Add(new Tag(Context.Guild.Id, Context.User.Id, name, response, image, format));
            await Context.Database.SaveChangesAsync();
            return CommandSuccessLocalized("tag_created", args: name);
        }

        [Command("info")]
        public async ValueTask<AdminCommandResult> ViewTagInfoAsync([Remainder, Lowercase] string name)
        {
            if (!(await Context.Database.Tags.FindAsync(Context.Guild.Id.RawValue, name) is { } tag))
                return CommandErrorLocalized("tag_notfound");

            var author = await Context.Client.GetOrDownloadUserAsync(tag.AuthorId);

            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithDescription(tag.Name)
                .AddField(Localize("info_owner"), author.Format(false), true)
                .AddField(Localize("tag_info_uses"), tag.Uses)
                .AddField(Localize("info_created"), string.Join('\n', tag.CreatedAt.ToString("g", Context.Language.Culture),
                    (DateTimeOffset.UtcNow - tag.CreatedAt).HumanizeFormatted(Localization, Context.Language,
                        TimeUnit.Second, true))).Build());
        }

        [Command("delete")]
        public async ValueTask<AdminCommandResult> DeleteTagAsync([Remainder, Lowercase] string name)
        {
            if (!(await Context.Database.Tags.FindAsync(Context.Guild.Id.RawValue, name) is { } tag))
                return CommandErrorLocalized("tag_notfound");

            if (tag.AuthorId != Context.User.Id && (Context.User as CachedMember)?.Permissions.ManageMessages != true)
                return CommandErrorLocalized("tag_nopermission");

            Context.Database.Tags.Remove(tag);
            await Context.Database.SaveChangesAsync();

            return CommandSuccessLocalized("tag_deleted");
        }

        [Command("search"), RunMode(RunMode.Parallel)]
        public async ValueTask<AdminCommandResult> SearchTagsAsync([Remainder, Lowercase] string name)
        {
            var tags = await Context.Database.Tags.Where(x => x.GuildId == Context.Guild.Id)
                .ToDictionaryAsync(x => x, x => x.Name.GetLevenshteinDistanceTo(name));

            tags = tags.Where(x => x.Value < name.Length)
                .OrderBy(x => x.Value)
                .Take(10)
                .ToDictionary(x => x.Key, x => x.Value);

            if (tags.Count == 0)
                return CommandErrorLocalized("tag_search_none");

            return CommandSuccess(embed: new LocalEmbedBuilder()
                .WithSuccessColor()
                .WithTitle(Localize("tag_search", name))
                .WithDescription(string.Join('\n', tags.Select(x => x.Key.Name))).Build());
        }
    }
}