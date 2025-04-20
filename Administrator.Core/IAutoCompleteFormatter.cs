using Qmmands;

namespace Administrator.Core;

public interface IAutoCompleteFormatter<in TContext, in TModel, out TAutoCompleteValue>
    where TContext : ICommandContext
    where TModel : notnull
    where TAutoCompleteValue : notnull
{
    static abstract string FormatAutoCompleteName(TContext context, TModel model);
    static abstract TAutoCompleteValue FormatAutoCompleteValue(TContext context, TModel model);
    static abstract string[] FormatComparisonValues(TContext context, TModel model);
}

public interface IAutoCompleteFormatter<in TModel, out TAutoCompleteValue> : IAutoCompleteFormatter<ICommandContext, TModel, TAutoCompleteValue>
    where TModel : notnull
    where TAutoCompleteValue : notnull;