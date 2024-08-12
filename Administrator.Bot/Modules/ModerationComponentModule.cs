using Administrator.Database;
using Disqord;
using Disqord.Bot.Commands.Components;
using Qmmands;

namespace Administrator.Bot;

public sealed class ModerationComponentModule(PunishmentService punishments, AdminDbContext db) : DiscordComponentGuildModuleBase
{
    [ModalCommand("Ban:*")]
    public async Task<IResult> BanAsync(Snowflake userId, string? reason = null, TimeSpan? duration = null, int? messagePruneDays = null)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were banning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.BanAsync(Context.GuildId, target, Context.Author, reason, messagePruneDays, Context.Interaction.CreatedAt() + duration, null));
    }

    [ModalCommand("Warning:*")]
    public async Task<IResult> WarnAsync(Snowflake userId, string? reason = null, [Range(0, 50)] int? demeritPoints = null)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were warning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.WarnAsync(Context.GuildId, target, Context.Author, reason, demeritPoints, null));
    }
    
    [ModalCommand("Kick:*")]
    public async Task<IResult> KickAsync(Snowflake userId, string? reason = null)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were banning.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.KickAsync(Context.GuildId, target, Context.Author, reason, null));
    }
    
    [ModalCommand("Timeout:*")]
    public async Task<IResult> BanAsync(Snowflake userId, TimeSpan duration, string? reason = null)
    {
        var target = await Bot.GetOrFetchUserAsync(userId);
        if (target is null)
            return Response("Failed to grab information about the user you were timing out.").AsEphemeral();
        
        return await ModerationModule.PunishAsync(Context, () => 
            punishments.TimeoutAsync(Context.GuildId, target, Context.Author, reason, Context.Interaction.CreatedAt() + duration, null));
    }
}