using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using IResult = Qmmands.IResult;

namespace Administrator.Bot;

public sealed class AppealComponentModule(AdminDbContext db, PunishmentService punishmentService) : DiscordComponentGuildModuleBase
{
    public IUserMessage Message => (Context.Interaction as IComponentInteraction)!.Message;

    [ButtonCommand("Appeal:Accept:*")]
    public async Task<IResult> AcceptAsync(int id)
    {
        var result = await punishmentService.RevokePunishmentAsync(id, Context.Author, "User's appeal was accepted.", true);

        if (!result.IsSuccessful)
            return Response(result.ErrorMessage).AsEphemeral();

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithHauntedColor()
                    .WithFooter($"Appeal accepted by {Context.Author.Tag}", Context.Author.GetGuildAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Appeal has been accepted, and punishment {result.Value.FormatKey()} has been revoked.").AsEphemeral();
    }

    [ButtonCommand("Appeal:NeedsInfo:*")]
    public async Task<IResult> NeedsInfoAsync(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.NeedsInfo;
        await db.SaveChangesAsync();

        var dmMessage = punishment.FormatAppealInfoNeededMessage<LocalMessage>(Bot);
        await Bot.TrySendDirectMessageAsync(punishment.Target.Id, dmMessage);

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithStrangeColor()
                    .WithFooter($"More information requested by {Context.Author.Tag}", Context.Author.GetGuildAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"More information has been requested for punishment {Markdown.Code($"[#{id}]")}.").AsEphemeral();
    }

    [ButtonCommand("Appeal:Reject:*")]
    public async Task<IResult> RejectAsync(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.Rejected;
        await db.SaveChangesAsync();

        var dmMessage = punishment.FormatAppealRejectionMessage<LocalMessage>(Bot);
        await Bot.TrySendDirectMessageAsync(punishment.Target.Id, dmMessage);

        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithCollectorsColor()
                    .WithFooter($"Rejected by {Context.Author.Tag}", Context.Author.GetGuildAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Punishment {Markdown.Code($"[#{id}]")}'s appeal has been rejected.").AsEphemeral();
    }

    [ButtonCommand("Appeal:Ignore:*")]
    public async Task<IResult> IgnoreAsync(int id)
    {
        var punishment = await db.Punishments.OfType<RevocablePunishment>().Where(x => x.GuildId == Context.GuildId).SingleAsync(x => x.Id == id);
        punishment.AppealStatus = AppealStatus.Ignored;
        await db.SaveChangesAsync();
        
        await Message.ModifyAsync(x =>
        {
            x.Embeds = new[]
            {
                LocalEmbed.CreateFrom(Message.Embeds[0]).WithDecoratedColor()
                    .WithFooter($"Ignored by {Context.Author.Tag}", Context.Author.GetGuildAvatarUrl())
            };

            x.Components = new List<LocalRowComponent>();
        });

        return Response($"Punishment {Markdown.Code($"[#{id}]")}'s appeal has been ignored.").AsEphemeral();
    }
}