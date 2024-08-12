using Disqord.Bot.Commands;
using Qmmands;

namespace Administrator.Bot;

public sealed class MutatedStringTypeParser : DiscordTypeParser<string>
{
    public override ValueTask<ITypeParserResult<string>> ParseAsync(IDiscordCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var stringValue = new string(value.Span);
        foreach (var attribute in parameter.CustomAttributes.OfType<MutateStringAttribute>())
        {
            stringValue = attribute.Mutation.Invoke(stringValue);
        }

        return Success(stringValue);
    }
}