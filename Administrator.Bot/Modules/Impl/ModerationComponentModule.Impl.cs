using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class ModerationComponentModule(PunishmentService punishments, AdminDbContext db) : DiscordComponentGuildModuleBase
{
    public partial async Task<IResult> Ban(Snowflake userId, string? reason, TimeSpan? duration, int? messagePruneDays)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were banning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.BanAsync(Context.GuildId, target, Context.Author, reason, messagePruneDays, Context.Interaction.CreatedAt() + duration, null));
    }

    public partial async Task<IResult> Warn(Snowflake userId, string? reason, [Range(0, 50)] int? demeritPoints)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were warning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.WarnAsync(Context.GuildId, target, Context.Author, reason, demeritPoints, null));
    }
    
    public partial async Task<IResult> Kick(Snowflake userId, string? reason)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were banning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.KickAsync(Context.GuildId, target, Context.Author, reason, null));
    }
    
    public partial async Task<IResult> Timeout(Snowflake userId, TimeSpan duration, string? reason)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were timing out.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.TimeoutAsync(Context.GuildId, target, Context.Author, reason, Context.Interaction.CreatedAt() + duration, null));
    }
}