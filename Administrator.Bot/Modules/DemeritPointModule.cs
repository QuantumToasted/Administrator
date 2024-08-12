using System.Text;
using Administrator.Core;
using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Application;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Qmmands;
using Timeout = Administrator.Database.Timeout;

namespace Administrator.Bot;

[SlashGroup("demerit-points")]
[RequireInitialAuthorPermissions(Permissions.ModerateMembers)]
public sealed class DemeritPointsModule(AdminDbContext db, PunishmentService punishments, DemeritPointDecayService decayService) : DiscordApplicationGuildModuleBase
{
    [SlashCommand("view")]
    [Description("Views a user's current demerit points.")]
    public async Task<IResult> ViewAsync(
        [Description("The user to view demerit points for.")] 
            IUser user)
    {
        var member = await db.Members.GetOrCreateAsync(Context.GuildId, user.Id);
        var responseBuilder = new StringBuilder()
            .AppendNewline($"{Mention.User(user.Id)} is currently at {Markdown.Bold("demerit point".ToQuantity(member.DemeritPoints))}.");
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        
        if (member.LastDemeritPointDecay.HasValue && guild.DemeritPointsDecayInterval.HasValue)
        {
            var nextDecay = member.LastDemeritPointDecay.Value + guild.DemeritPointsDecayInterval.Value;
            responseBuilder.AppendNewline($"Their next decay will occur {Markdown.Timestamp(nextDecay, Markdown.TimestampFormat.RelativeTime)}.");
        }
        
        return Response(responseBuilder.ToString());
    }
    
    [SlashCommand("set")]
    [Description("Sets a user's demerit points to a specified value.")]
    public Task SetAsync(
        [Description("The user to alter demerit points for.")]
        [RequireHierarchyIfMember]
            IUser user,
        [Description("The number of demerit points")]
        [Minimum(0)] 
            int demeritPoints,
        [Description("Automatically apply any punishments reached for the new demerit point total. Default: False")]
            bool applyPunishments = false)
    {
        return ModifyDemeritPointsAsync(user, _ => demeritPoints, applyPunishments);
    }

    [SlashCommand("add")]
    [Description("Adds (or subtracts) demerit points from a user.")]
    public async Task AddAsync(
        [Description("The user to alter demerit points for.")]
        [RequireHierarchyIfMember]
            IUser user,
        [Description("The number of demerit points to add (or subtract if negative).")]
        [Range(-1000, 1000)] 
            int demeritPoints, 
        [Description("Automatically apply any punishments reached for the new demerit point total. Default: False")]
            bool applyPunishments = false)
    {
        if (demeritPoints == 0)
        {
            await Response("You cannot add 0 demerit points to someone!").AsEphemeral();
            return;
        }

        if (demeritPoints < 0 && applyPunishments)
            applyPunishments = false;
        
        await ModifyDemeritPointsAsync(user, x => Math.Max(0, x + demeritPoints), applyPunishments);
    }

    private async Task ModifyDemeritPointsAsync(IUser user, Func<int, int> transformation, bool applyPunishments)
    {
        var member = await db.Members.GetOrCreateAsync(Context.GuildId, user.Id);
        var oldDemeritPoints = member.DemeritPoints;
        var newDemeritPoints = transformation.Invoke(oldDemeritPoints);

        var view = new AdminPromptView($"{user.Mention}'s demerit points are currently {Markdown.Bold(oldDemeritPoints)}, " +
                                       $"and will be set to {Markdown.Bold(newDemeritPoints)}.\n" +
                                       (applyPunishments ? "Punishments will be automatically applied where applicable." : string.Empty))
            .OnConfirm($"{user.Mention}'s demerit points have been set to {Markdown.Bold(newDemeritPoints)} (from {Markdown.Bold(oldDemeritPoints)}).");

        await View(view);
        if (view.Result)
        {
            member.DemeritPoints = newDemeritPoints;
            if (newDemeritPoints == 0)
                member.LastDemeritPointDecay = null;
        
            await db.SaveChangesAsync();
            decayService.CancelCts();

            if (applyPunishments)
                await ApplyPunishmentsAsync(user, oldDemeritPoints, newDemeritPoints);
        }
    }

    private async Task ApplyPunishmentsAsync(IUser user, int oldDemeritPoints, int newDemeritPoints)
    {
        var guild = await db.Guilds.GetOrCreateAsync(Context.GuildId);
        var automaticPunishments = await db.AutomaticPunishments.Where(x => x.GuildId == Context.GuildId)
            .OrderBy(x => x.DemeritPoints)
            .ToListAsync();
        
        if (newDemeritPoints > 0 && newDemeritPoints > oldDemeritPoints && 
            automaticPunishments.FirstOrDefault(x => x.DemeritPoints >= newDemeritPoints && x.DemeritPoints >= oldDemeritPoints) is { } demeritPointPunishment)
        {
            var expiresAt = DateTimeOffset.UtcNow + demeritPointPunishment.PunishmentDuration;
            Punishment punishmentToApply = demeritPointPunishment.PunishmentType switch
            {
                PunishmentType.Timeout => new Timeout(Context.GuildId, UserSnapshot.FromUser(user), UserSnapshot.FromUser(Context.Author),
                    "Automatic timeout: Demerit points set manually.", expiresAt!.Value),
                PunishmentType.Kick => new Kick(Context.GuildId, UserSnapshot.FromUser(user), UserSnapshot.FromUser(Context.Author),
                    "Automatic kick: Demerit points set manually."),
                PunishmentType.Ban => new Ban(Context.GuildId, UserSnapshot.FromUser(user), UserSnapshot.FromUser(Context.Author),
                    "Automatic ban: Demerit points set manually.", guild.DefaultBanPruneDays, expiresAt),
                _ => throw new ArgumentOutOfRangeException()
            };

            await punishments.ProcessPunishmentAsync(punishmentToApply, null);
        }
    }
}