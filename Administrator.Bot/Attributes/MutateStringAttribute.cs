using Qmmands.Text;

namespace Administrator.Bot;

public abstract class MutateStringAttribute(Func<string, string> mutation) : CustomTypeParserAttribute(typeof(MutateStringAttribute))
{
    public Func<string, string> Mutation { get; } = mutation;
}