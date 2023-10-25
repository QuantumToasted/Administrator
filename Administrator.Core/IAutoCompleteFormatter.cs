using Disqord;

namespace Administrator.Core;

public interface IAutoCompleteFormatter
{
    string FormatAutoCompleteName(IClient client, object model);

    object FormatAutoCompleteValue(IClient client, object model);
    
    Func<object, string> ComparisonSelector { get; }
}

public interface IAutoCompleteFormatter<TModel, out TAutoCompleteValue> : IAutoCompleteFormatter
    where TModel : notnull
    where TAutoCompleteValue : notnull
{
    string FormatAutoCompleteName(IClient client, TModel model);

    TAutoCompleteValue FormatAutoCompleteValue(IClient client, TModel model);
    
    new Func<TModel, string> ComparisonSelector { get; }

    string IAutoCompleteFormatter.FormatAutoCompleteName(IClient client, object model) => FormatAutoCompleteName(client, (TModel)model);
    object IAutoCompleteFormatter.FormatAutoCompleteValue(IClient client, object model) => FormatAutoCompleteValue(client, (TModel)model);
    Func<object, string> IAutoCompleteFormatter.ComparisonSelector => x => ComparisonSelector.Invoke((TModel)x);
}