using System.Linq.Expressions;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

[SlashGroup("punishments")]
[RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
public sealed class PunishmentsModule(AdminDbContext db, PunishmentService punishments) : DiscordApplicationGuildModuleBase
{
    [SlashGroup("for")]
    public sealed class PunishmentsForModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        [SlashCommand("target")]
        [Description("Lists all punishments with a specific user as the target.")]
        public async Task<IResult> ListUserPunishmentsAsync(
            [Description("The user who was the target of the punishment(s).")]
                IUser user)
        {
            var pages = await GeneratePagesAsync(db, Context, $"Punishments for {user.Tag}", 
                x => x.Target.Id == (ulong) user.Id);
            
            return pages.Count switch
            {
                0 => Response("No punishments were found matching the criteria you specified."),
                1 => Response(pages[0].Embeds.Value[0]),
                _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
            };
        }
    }

    [SlashGroup("from")]
    public sealed class PunishmentsFromModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        [SlashCommand("moderator")]
        [Description("Lists all punishments with a specific user as the moderator.")]
        public async Task<IResult> ListModeratorPunishmentsAsync(
            [Description("The user who was the moderator of the punishment(s).")]
                IUser user)
        {
            var pages = await GeneratePagesAsync(db, Context, $"Punishments created by moderator {user.Tag}", 
                x => x.Moderator.Id == user.Id.RawValue);
            
            return pages.Count switch
            {
                0 => Response("No punishments were found matching the criteria you specified."),
                1 => Response(pages[0].Embeds.Value[0]),
                _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
            };
        }
    }

    [SlashCommand("case")]
    [Description("Views detailed information about a single punishment case in this server.")]
    public async Task<IResult> ShowCaseAsync(
        [Description("The ID of the punishment to view.")]
            int id)
    {
        if (await db.Punishments.Include(x => x.Attachment).FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.Id == id) is not { } punishment)
            return Response($"No punishment could be found with the ID {id}");
        
        var response = await punishment.FormatPunishmentCaseMessageAsync<LocalInteractionMessageResponse>(Bot);
        return Response(response);
    }
    
    [AutoComplete("case")]
    public Task AutoCompleteAllPunishmentsAsync(AutoComplete<int> id)
        => id.IsFocused ? punishments.AutoCompletePunishmentsAsync<Punishment>(Context.GuildId, id) : Task.CompletedTask;

    public static async Task<List<Page>> GeneratePagesAsync(AdminDbContext db, IDiscordCommandContext context, string embedTitle, Expression<Func<Punishment, bool>>? filterFunc, Snowflake? guildId = null)
    {
        guildId ??= context.GuildId!.Value; // context.GuildId is guaranteed to have a value by the command
        var guildPunishments = db.Punishments.Where(x => x.GuildId == guildId.Value).OrderByDescending(x => x.Id);

        List<Punishment> punishments;
        if (filterFunc is not null)
        {
            punishments = await guildPunishments.Where(filterFunc).ToListAsync();
        }
        else
        {
            punishments = await guildPunishments.ToListAsync();
        }

        return punishments.Chunk(5)
            .Select(x =>
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor()
                    .WithTitle(embedTitle)
                    .WithFields(x.Select(y => y.FormatPunishmentListEmbedField(context.Bot)));

                return new Page().AddEmbed(embed);
            }).ToList();
    }
}