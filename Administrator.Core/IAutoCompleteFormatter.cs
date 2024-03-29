﻿using Disqord;

namespace Administrator.Core;

public interface IAutoCompleteFormatter
{
    string[] FormatAutoCompleteNames(IClient client, object model);

    object FormatAutoCompleteValue(IClient client, object model);
    
    Func<object, string[]> ComparisonSelector { get; }
}

public interface IAutoCompleteFormatter<in TModel, out TAutoCompleteValue> : IAutoCompleteFormatter
    where TModel : notnull
    where TAutoCompleteValue : notnull
{
    string[] FormatAutoCompleteNames(IClient client, TModel model);

    TAutoCompleteValue FormatAutoCompleteValue(IClient client, TModel model);
    
    new Func<TModel, string[]> ComparisonSelector { get; }

    string[] IAutoCompleteFormatter.FormatAutoCompleteNames(IClient client, object model) => FormatAutoCompleteNames(client, (TModel)model);
    object IAutoCompleteFormatter.FormatAutoCompleteValue(IClient client, object model) => FormatAutoCompleteValue(client, (TModel)model);
    Func<object, string[]> IAutoCompleteFormatter.ComparisonSelector => x => ComparisonSelector.Invoke((TModel)x);
}