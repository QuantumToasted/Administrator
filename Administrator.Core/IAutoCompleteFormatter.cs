using Qmmands;

namespace Administrator.Core;

public interface IAutoCompleteFormatter; // marker interface b/c open generics

public interface IAutoCompleteFormatter<in TContext, in TModel, out TAutoCompleteValue> : IAutoCompleteFormatter
    where TContext : ICommandContext
    where TModel : notnull
    where TAutoCompleteValue : notnull
{
    string FormatAutoCompleteName(TContext context, TModel model);

    TAutoCompleteValue FormatAutoCompleteValue(TContext context, TModel model);
    
    Func<TContext, TModel, string[]> ComparisonSelector { get; }
}

public interface IAutoCompleteFormatter<in TModel, out TAutoCompleteValue> : IAutoCompleteFormatter<ICommandContext, TModel, TAutoCompleteValue>
    where TModel : notnull
    where TAutoCompleteValue : notnull
{
    string FormatAutoCompleteName(TModel model);

    TAutoCompleteValue FormatAutoCompleteValue(TModel model);
    
    new Func<TModel, string[]> ComparisonSelector { get; }

    string IAutoCompleteFormatter<ICommandContext, TModel, TAutoCompleteValue>.FormatAutoCompleteName(ICommandContext context, TModel model)
        => FormatAutoCompleteName(model);

    TAutoCompleteValue IAutoCompleteFormatter<ICommandContext, TModel, TAutoCompleteValue>.FormatAutoCompleteValue(ICommandContext context, TModel model)
        => FormatAutoCompleteValue(model);

    Func<ICommandContext, TModel, string[]> IAutoCompleteFormatter<ICommandContext, TModel, TAutoCompleteValue>.ComparisonSelector
        => (_, model) => ComparisonSelector.Invoke(model);
}