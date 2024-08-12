namespace Administrator.Bot;

public sealed class LowercaseAttribute() : MutateStringAttribute(static x => x.ToLowerInvariant());