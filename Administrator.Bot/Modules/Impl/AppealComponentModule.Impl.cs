using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class AppealComponentModule(AdminDbContext db, PunishmentService punishments) : DiscordComponentModuleBase
{
    public IUserMessage Message => (Context.Interaction as IComponentInteraction)!.Message;

    public partial async Task CreateAppealModal(int id)
    {
        var punishment = (RevocablePunishment) await db.Punishments.FirstAsync(x => x.Id == id);
        if (!punishment.CanBeAppealed(out var appealAfter))
        {
            await Response(
                $"You are trying to appeal too quickly!\n" +
                $"The punishment {punishment} can be appealed {Markdown.Timestamp(appealAfter.Value, Markdown.TimestampFormat.RelativeTime)}.").AsEphemeral();
            return;
        }
        
        var modal = new LocalInteractionModalResponse()
            .WithCustomId($"Appeal:{Message.Id}:{id}")
            .WithTitle($"Appealing {punishment.GetType().Name.Humanize(LetterCasing.LowerCase)} #{id}")
            .AddComponent(new LocalRowComponent()
                .AddComponent(new LocalTextInputComponent()
                    .WithCustomId("appeal")
                    .WithLabel("Your appeal")
                    .WithStyle(TextInputComponentStyle.Paragraph)
                    .WithMinimumInputLength(50)
                    .WithMaximumInputLength(Discord.Limits.Rest.MaxAuditLogReasonLength)
                    .WithPlaceholder("Enter your punishment appeal here...")));

        await Context.Interaction.Response().SendModalAsync(modal);
    }

    public partial async Task<IResult> Appeal(Snowflake messageId, int id, string appeal)
    {
        var result = await punishments.AppealPunishmentAsync(Context.AuthorId, id, appeal);
        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        await Bot.ModifyMessageAsync(Context.ChannelId, messageId, x => x.Components = new List<LocalRowComponent>());
        return Response($"Your {result.Value.FormatPunishmentName(LetterCasing.LowerCase)} {result.Value} has had its appeal sent or updated.")
            .AsEphemeral();
    }

    public partial async Task<IResult> Accept(int id)
    {
        var result = await punishments.RevokePunishmentAsync(Context.GuildId!.Value, id, Context.Author, "User's appeal was accepted.", true);

        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithHauntedColor()
                    .WithFooter($"Appeal accepted by {Context.Author.Tag}", (Context.Author as IMember)?.GetGuildAvatarUrl() ?? Context.Author.GetAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Appeal has been accepted, and punishment {result.Value} has been revoked.").AsEphemeral();
    }

    public partial async Task<IResult> NeedsInfo(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId!.Value).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.NeedsInfo;
        await db.SaveChangesAsync();

        var dmMessage = punishment.FormatAppealInfoNeededMessage<LocalMessage>(Bot);
        await Bot.TrySendDirectMessageAsync(punishment.Target.Id, dmMessage);

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithStrangeColor()
                    .WithFooter($"More information requested by {Context.Author.Tag}", (Context.Author as IMember)?.GetGuildAvatarUrl() ?? Context.Author.GetAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"More information has been requested for punishment {Markdown.Code($"[#{id}]")}.").AsEphemeral();
    }

    public partial async Task<IResult> Reject(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId!.Value).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.Rejected;
        await db.SaveChangesAsync();

        var dmMessage = punishment.FormatAppealRejectionMessage<LocalMessage>(Bot);
        await Bot.TrySendDirectMessageAsync(punishment.Target.Id, dmMessage);

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithCollectorsColor()
                    .WithFooter($"Rejected by {Context.Author.Tag}", (Context.Author as IMember)?.GetGuildAvatarUrl() ?? Context.Author.GetAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Punishment {Markdown.Code($"[#{id}]")}'s appeal has been rejected.").AsEphemeral();
    }

    public partial async Task<IResult> Ignore(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId!.Value).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.Ignored;
        await db.SaveChangesAsync();
        
        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithDecoratedColor()
                    .WithFooter($"Ignored by {Context.Author.Tag}", (Context.Author as IMember)?.GetGuildAvatarUrl() ?? Context.Author.GetAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Punishment {Markdown.Code($"[#{id}]")}'s appeal has been ignored.").AsEphemeral();
    }
}