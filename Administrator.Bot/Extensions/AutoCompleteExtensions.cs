using System.Reflection;
using Administrator.Bot.AutoComplete;
using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands.Application;
using Qmmands;
using Qommon;

namespace Administrator.Bot;

public static class AutoCompleteExtensions
{
    private static readonly Dictionary<Type, Delegate> NameDelegates = new();
    private static readonly Dictionary<Type, Delegate> ValueDelegates = new();
    private static readonly Dictionary<Type, Delegate> ComparisonDelegates = new();
    
    public static void AutoComplete<TContext, TModel, TAutoCompleteValue>(this AutoComplete<TAutoCompleteValue> autoComplete, TContext context, ICollection<TModel> collection)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        Guard.IsTrue(autoComplete.IsFocused); // Enforce checking in the autocomplete method to reduce db/cache stress
        
        if (collection.Count == 0)
            return;

        if (string.IsNullOrWhiteSpace(autoComplete.RawArgument))
        {
            autoComplete.AddRange(context, collection);
            return;
        }

        var comparisonDict = new Dictionary<string, TModel>();
        foreach (var modelWithValues in collection.Select(m => new { Model = m, Values = FormatComparisonValues(context, m) }))
        {
            foreach (var value in modelWithValues.Values)
            {
                comparisonDict.Add(value, modelWithValues.Model);
            }
        }
        
        if (comparisonDict.FirstOrDefault(x => x.Key.Equals(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase)) is TModel exactMatch)
        {
            autoComplete.Add(context, exactMatch);
            return;
        }

        var closeMatches = comparisonDict.Where(x => x.Key.Contains(autoComplete.RawArgument, StringComparison.InvariantCultureIgnoreCase))
            .Select(x => x.Value).ToList();
			
        if (closeMatches.Count > 0)
        {
            autoComplete.AddRange(context, closeMatches);
        }
    }

    private static void Add<TContext, TModel, TAutoCompleteValue>(this AutoComplete<TAutoCompleteValue> autoComplete, TContext context, TModel model)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        const int maxNameLength = Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength;
        
        var name = FormatAutoCompleteName(context, model).Truncate(maxNameLength);
        var value = FormatAutoCompleteValue<TContext, TModel, TAutoCompleteValue>(context, model);

        autoComplete.Choices!.TryAdd(name, value);
    }

    private static void AddRange<TContext, TModel, TAutoCompleteValue>(this AutoComplete<TAutoCompleteValue> autoComplete, TContext context, IEnumerable<TModel> enumerable)
        where TContext : ICommandContext
        where TModel : notnull
        where TAutoCompleteValue : notnull
    {
        const int maxChoices = Discord.Limits.ApplicationCommand.Option.MaxChoiceAmount;
        const int maxNameLength = Discord.Limits.ApplicationCommand.Option.Choice.MaxNameLength;

        foreach (var model in enumerable)
        {
            var name = FormatAutoCompleteName(context, model).Truncate(maxNameLength);
            var value = FormatAutoCompleteValue<TContext, TModel, TAutoCompleteValue>(context, model);

            autoComplete.Choices!.TryAdd(name, value);
            
            if (autoComplete.Choices!.Count == maxChoices)
                break;
        }
    }

    private static string FormatAutoCompleteName<TContext, TModel>(TContext context, TModel model)
        => (string)NameDelegates[typeof(TModel)].DynamicInvoke(context, model)!;

    private static TAutoCompleteValue FormatAutoCompleteValue<TContext, TModel, TAutoCompleteValue>(TContext context, TModel model)
        => (TAutoCompleteValue)ValueDelegates[typeof(TModel)].DynamicInvoke(context, model)!;

    private static string[] FormatComparisonValues<TContext, TModel>(TContext context, TModel model)
        => (string[])ComparisonDelegates[typeof(TModel)].DynamicInvoke(context, model)!;
    
    static AutoCompleteExtensions()
    {
        var types = typeof(HighlightAutoCompleteFormatter).Assembly.GetTypes();
        var implementingTypes = types.Where(x =>
        {
            return x.GetInterfaces().Where(y => y.IsGenericType).Any(y => y.GetGenericTypeDefinition() == typeof(IAutoCompleteFormatter<,,>));
        });
        
        var nonInterfaceTypes = implementingTypes.Where(x => !x.IsInterface);

        const string nameMethodName = nameof(IAutoCompleteFormatter<ICommandContext, object, object>.FormatAutoCompleteName);
        const string valueMethodName = nameof(IAutoCompleteFormatter<ICommandContext, object, object>.FormatAutoCompleteValue);
        const string comparisonMethodName = nameof(IAutoCompleteFormatter<ICommandContext, object, object>.FormatComparisonValues);
        
        foreach (var type in nonInterfaceTypes)
        {
            var implementedInterface = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAutoCompleteFormatter<,,>));

            var generics = implementedInterface.GetGenericArguments();
            //var contextType = generics[0];
            var modelType = generics[1];
            //var valueType = generics[2];

            var nameMethod = type.GetMethod(nameMethodName, BindingFlags.Static | BindingFlags.Public)!;
            var nameMethodSignatureTypes = GetDelegateTypes(nameMethod);
            var nameMethodDelegate = nameMethod.CreateDelegate(typeof(Func<,,>).MakeGenericType([..nameMethodSignatureTypes]));
            NameDelegates[modelType] = nameMethodDelegate;
                
            var valueMethod = type.GetMethod(valueMethodName, BindingFlags.Static | BindingFlags.Public)!;
            var valueMethodSignatureTypes = GetDelegateTypes(valueMethod);
            var valueMethodDelegate = valueMethod.CreateDelegate(typeof(Func<,,>).MakeGenericType([..valueMethodSignatureTypes]));
            ValueDelegates[modelType] = valueMethodDelegate;
                
            var comparisonMethod = type.GetMethod(comparisonMethodName, BindingFlags.Static | BindingFlags.Public)!;
            var comparisonMethodSignatureTypes = GetDelegateTypes(comparisonMethod);
            var comparisonMethodDelegate = comparisonMethod.CreateDelegate(typeof(Func<,,>).MakeGenericType([..comparisonMethodSignatureTypes]));
            ComparisonDelegates[modelType] = comparisonMethodDelegate;
        }

        static Type[] GetDelegateTypes(MethodInfo method) => [.. method.GetParameters().Select(x => x.ParameterType), method.ReturnType];
    }
}