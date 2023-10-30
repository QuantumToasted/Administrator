using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qommon;

namespace Administrator.Bot;

[ScopedService]
public sealed class AutoCompleteService(IClient client)
{
    private static readonly Dictionary<Type, IAutoCompleteFormatter> Formatters = new();

    public void AutoComplete<TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, ICollection<TModel> collection)
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        Guard.IsTrue(autoComplete.IsFocused); // Enforce checking in the autocomplete method to reduce db/cache stress
        
        if (!Formatters.TryGetValue(typeof(TModel), out var formatter))
            throw new Exception($"Auto-complete formatter not registered for type {typeof(TModel)}!");

        if (collection.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            autoComplete.Choices.AddRange(collection.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => formatter.FormatAutoCompleteName(client, x), x => (TAutoCompleteValue) formatter.FormatAutoCompleteValue(client, x)));
            
            return;
        }

        var comparisonDict = collection.ToDictionary(x => formatter.ComparisonSelector.Invoke(x));
        if (comparisonDict.FirstOrDefault(x => x.Key.Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            autoComplete.Choices.Add(formatter.FormatAutoCompleteName(client, exactMatch), (TAutoCompleteValue) formatter.FormatAutoCompleteValue(client, exactMatch));
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value).ToList();
        if (closeMatches.Count > 0)
        {
            autoComplete.Choices.AddRange(closeMatches.Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount)
                .ToDictionary(x => formatter.FormatAutoCompleteName(client, x), x => (TAutoCompleteValue) formatter.FormatAutoCompleteValue(client, x)));
        }
    }

    static AutoCompleteService()
    {
        foreach (var type in typeof(AutoCompleteService).Assembly.GetTypes().Where(x => typeof(IAutoCompleteFormatter).IsAssignableFrom(x) && !x.IsInterface))
        {
            var interfaceType = type.GetInterfaces().Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAutoCompleteFormatter<,>));
            
            Formatters[interfaceType.GenericTypeArguments[0]] = (IAutoCompleteFormatter) Activator.CreateInstance(type)!;
        }
    }
}