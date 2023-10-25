using Qmmands;

namespace Administrator.Core;

public interface IPlaceholderFormatter
{
    ValueTask<string> ReplacePlaceholdersAsync(string str, ICommandContext? context);
}