using System.Linq.Expressions;
using System.Text;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class PunishmentsModule(AdminDbContext db, PunishmentService punishments) : DiscordApplicationGuildModuleBase
{
    public partial async Task<IResult> DemeritPoints(IUser user)
    {
        var demeritPoints = await db.Punishments.GetCurrentDemeritPointsAsync(Context.GuildId, user.Id);
        var warnings = await db.Punishments.OfType<Warning>()
            .Where(x => x.GuildId == Context.GuildId && x.Target.Id == user.Id.RawValue)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        var responseBuilder =
            new StringBuilder($"{user.Mention} has {"demerit point".ToQuantity(demeritPoints)} across {"warning".ToQuantity(warnings.Count)}.")
                .AppendNewline()
                .AppendJoinTruncated("\n", warnings.Select(x => $"{x} - {x.DemeritPointsRemaining}/{x.DemeritPoints}"), 1000);

        var member = await db.Members.GetOrCreateAsync(Context.GuildId, user.Id);

        if (member.NextDemeritPointDecay.HasValue)
        {
            responseBuilder.AppendNewline()
                .AppendNewline($"Their next demerit point decay will occur " +
                               $"{Markdown.Timestamp(member.NextDemeritPointDecay.Value, Markdown.TimestampFormat.RelativeTime)}.");
        }

        return Response(responseBuilder.ToString());
    }
    
    public sealed partial class PunishmentsForModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> Target(IUser user)
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

    public sealed partial class PunishmentsFromModule(AdminDbContext db) : DiscordApplicationGuildModuleBase
    {
        public partial async Task<IResult> Moderator(IUser user)
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

    public partial async Task<IResult> Case(int id)
    {
        if (await db.Punishments.Include(x => x.Attachment).FirstOrDefaultAsync(x => x.GuildId == Context.GuildId && x.Id == id) is not { } punishment)
            return Response($"No punishment could be found with the ID {id}");
        
        var response = await punishment.FormatPunishmentCaseMessageAsync<LocalInteractionMessageResponse>(Bot);
        return Response(response);
    }
    
    public partial Task AutoCompleteAllPunishments(AutoComplete<int> id)
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