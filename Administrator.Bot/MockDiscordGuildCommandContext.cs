using System.Globalization;
using Disqord;
using Disqord.Bot;
using Disqord.Bot.Commands;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public class MockDiscordGuildCommandContext(DiscordBotBase bot, Snowflake guildId, Snowflake channelId, IUser author) : IDiscordCommandContext
{
    public Snowflake GuildId => guildId;
    
    public IUser Author => author;
    
    public Snowflake ChannelId => channelId;
    
    public DiscordBotBase Bot => bot;

    public IServiceProvider Services => bot.Services;

    public CancellationToken CancellationToken => bot.StoppingToken;

    ValueTask ICommandContext.ResetAsync() => throw new InvalidOperationException();
    CultureInfo ICommandContext.Locale => throw new InvalidOperationException();
    ICommandExecutionStep? ICommandContext.ExecutionStep { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
    ICommand? ICommandContext.Command { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
    IDictionary<IParameter, MultiString>? ICommandContext.RawArguments { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
    IDictionary<IParameter, object?>? ICommandContext.Arguments { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
    IModuleBase? ICommandContext.ModuleBase { get => throw new InvalidOperationException(); set => throw new InvalidOperationException(); }
    CultureInfo? IDiscordCommandContext.GuildLocale => throw new InvalidOperationException();
    Snowflake? IDiscordCommandContext.GuildId => GuildId;
}