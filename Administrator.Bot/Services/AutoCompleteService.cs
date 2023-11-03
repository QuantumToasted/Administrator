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
        
        if (Formatters.GetValueOrDefault(typeof(TModel)) is not IAutoCompleteFormatter<TModel, TAutoCompleteValue> formatter)
            throw new Exception($"Auto-complete formatter not registered for types [{typeof(TModel)},{typeof(TAutoCompleteValue)}]!");

        if (collection.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            AddRange(autoComplete, collection, formatter);
            return;
        }

        var comparisonDict = new Dictionary<string, TModel>();
        foreach (var modelWithValues in collection.Select(x => new { Model = x, Values = formatter.ComparisonSelector.Invoke(x) }))
        {
            foreach (var value in modelWithValues.Values)
            {
                comparisonDict.Add(value, modelWithValues.Model);
            }
        }
        
        if (comparisonDict.FirstOrDefault(x => x.Key.Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            Add(autoComplete, exactMatch, formatter);
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value).ToList();
        if (closeMatches.Count > 0)
        {
            AddRange(autoComplete, closeMatches, formatter);
        }
    }

    private void Add<TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, TModel model, IAutoCompleteFormatter<TModel, TAutoCompleteValue> formatter)
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        var names = formatter.FormatAutoCompleteNames(client, model)
            .Select(x => x.Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength))
            .Take(Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount);
        
        var value = formatter.FormatAutoCompleteValue(client, model);

        autoComplete.Choices!.AddRange(names.ToDictionary(x => x.Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength), _ => value));
    }

    private void AddRange<TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, IEnumerable<TModel> enumerable, IAutoCompleteFormatter<TModel, TAutoCompleteValue> formatter)
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        const int maxChoices = Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount;
        
        foreach (var model in enumerable)
        {
            var names = formatter.FormatAutoCompleteNames(client, model)
                .Select(x => x.Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength))
                .Take(Math.Max(maxChoices - autoComplete.Choices!.Count, 0));
        
            var value = formatter.FormatAutoCompleteValue(client, model);
            autoComplete.Choices!.AddRange(names.ToDictionary(x => x.Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength), _ => value));

            if (autoComplete.Choices.Count == maxChoices)
                break;
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