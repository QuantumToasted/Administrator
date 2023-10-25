using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Bot.Hosting;
using Microsoft.Extensions.Logging;
using Qommon;

namespace Administrator.Bot;

public sealed class AutoCompleteService : DiscordBotService
{
    private readonly Dictionary<Type, IAutoCompleteFormatter> _formatters = new();

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var type in GetType().Assembly.GetTypes().Where(x => typeof(IAutoCompleteFormatter).IsAssignableFrom(x) && !x.IsInterface))
        {
            var interfaceType = type.GetInterfaces().Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAutoCompleteFormatter<,>));
            
            _formatters[interfaceType.GenericTypeArguments[0]] = (IAutoCompleteFormatter) Activator.CreateInstance(type)!;
        }
        
        Logger.LogInformation("Registered {Count} auto-complete formatters.", _formatters.Count);
        
        return base.StartAsync(cancellationToken);
    }

    public void AutoComplete<TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, ICollection<TModel> collection)
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        Guard.IsTrue(autoComplete.IsFocused); // Enforce checking in the autocomplete method to reduce db/cache stress
        
        if (!_formatters.TryGetValue(typeof(TModel), out var formatter))
            throw new Exception($"Auto-complete formatter not registered for type {typeof(TModel)}!");

        if (collection.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            autoComplete.Choices.AddRange(collection.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => formatter.FormatAutoCompleteName(Bot, x), x => (TAutoCompleteValue) formatter.FormatAutoCompleteValue(Bot, x)));
            
            return;
        }

        var comparisonDict = collection.ToDictionary(x => formatter.ComparisonSelector.Invoke(x));
        if (comparisonDict.FirstOrDefault(x => x.Key.Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            autoComplete.Choices.Add(formatter.FormatAutoCompleteName(Bot, exactMatch), (TAutoCompleteValue) formatter.FormatAutoCompleteValue(Bot, exactMatch));
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value).ToList();
        if (closeMatches.Count > 0)
        {
            autoComplete.Choices.AddRange(closeMatches.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => formatter.FormatAutoCompleteName(Bot, x), x => (TAutoCompleteValue) formatter.FormatAutoCompleteValue(Bot, x)));
        }
    }
}