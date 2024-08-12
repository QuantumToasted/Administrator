using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

[ScopedService]
public sealed class AutoCompleteService(ICommandContextAccessor contextAccessor)
{
    private static readonly Dictionary<Type, IAutoCompleteFormatter> Formatters = new();

    public void AutoComplete<TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, ICollection<TModel> collection)
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        AutoComplete<IDiscordCommandContext, TModel, TAutoCompleteValue>(autoComplete, collection);
        
        /*
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
        */
    }

    private void AutoComplete<TContext, TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, 
        //IAutoCompleteFormatter<TContext, TModel, TAutoCompleteValue> formatter,
        ICollection<TModel> collection)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        Guard.IsTrue(autoComplete.IsFocused); // Enforce checking in the autocomplete method to reduce db/cache stress
        
        if (Formatters.GetValueOrDefault(typeof(TModel)) is not IAutoCompleteFormatter<TContext, TModel, TAutoCompleteValue> formatter)
            throw new Exception($"Auto-complete formatter not registered for types [{typeof(TContext)},{typeof(TModel)},{typeof(TAutoCompleteValue)}]!");
        
        var context = (TContext) contextAccessor.Context;
        if (collection.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            AddRange(autoComplete, collection, formatter);
            return;
        }

        var comparisonDict = new Dictionary<string, TModel>();
        foreach (var modelWithValues in collection.Select(m => new { Model = m, Values = formatter.ComparisonSelector.Invoke(context, m) }))
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

    private void Add<TContext, TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, TModel model, IAutoCompleteFormatter<TContext, TModel, TAutoCompleteValue> formatter)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        var context = (TContext) contextAccessor.Context;
        var name = formatter.FormatAutoCompleteName(context, model).Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength);
        var value = formatter.FormatAutoCompleteValue(context, model);

        autoComplete.Choices!.TryAdd(name, value);
    }

    private void AddRange<TContext, TModel, TAutoCompleteValue>(AutoComplete<TAutoCompleteValue> autoComplete, IEnumerable<TModel> enumerable, IAutoCompleteFormatter<TContext, TModel, TAutoCompleteValue> formatter)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        const int maxChoices = Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount;

        var context = (TContext) contextAccessor.Context;
        foreach (var model in enumerable)
        {
            var name = formatter.FormatAutoCompleteName(context, model).Truncate(Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength);
            var value = formatter.FormatAutoCompleteValue(context, model);

            autoComplete.Choices!.TryAdd(name, value);
            //autoComplete.Choices!.Add(name, value);
            
            if (autoComplete.Choices!.Count == maxChoices)
                break;
        }
    }

    static AutoCompleteService()
    {
        foreach (var type in typeof(AutoCompleteService).Assembly.GetTypes().Where(x => typeof(IAutoCompleteFormatter).IsAssignableFrom(x) && !x.IsInterface))
        {
            var interfaces = type.GetInterfaces();
            var interfaceType = interfaces.Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAutoCompleteFormatter<,,>));
            
            Formatters[interfaceType.GenericTypeArguments[1]] = (IAutoCompleteFormatter) Activator.CreateInstance(type)!;
        }
    }
}