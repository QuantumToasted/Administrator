using Administrator.Database;
using Disqord.Bot;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Administrator.Bot.Jobs;

public sealed class PunishmentExpiryJob<TPunishment>(ILogger<PunishmentExpiryJob<TPunishment>> logger, AdminDbContext db, 
    PunishmentService punishmentService, DiscordBotBase bot) : IAdminJob<PunishmentExpiryJob<TPunishment>, TPunishment>
    where TPunishment : RevocablePunishment, IExpiringDbEntity
{
    public ILogger Logger { get; } = logger;
    
    public async ValueTask<TPunishment> GetEntity(IJobExecutionContext context, int punishmentId)
    {
        var punishment = await db.Punishments.OfType<TPunishment>().FirstAsync(x => x.Id == punishmentId);
        return punishment;
    }

    public async ValueTask Execute(IJobExecutionContext context, TPunishment punishment)
    {
        var name = punishment.FormatPunishmentName(LetterCasing.Sentence);
        
        try
        {
            await punishmentService.RevokePunishmentAsync(punishment.GuildId, punishment.Id, bot.CurrentUser, $"{name} expired.", false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to revoke expiring {Name} {Id}.", punishment.FormatPunishmentName(LetterCasing.LowerCase), punishment.Id);
        }
    }
}