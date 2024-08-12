using Humanizer;
using Laylua.Marshaling;

namespace Administrator.Bot;

public sealed class CamelCaseUserDataNamingPolicy : UserDataNamingPolicy
{
    public override string ConvertName(string name)
        => name.Camelize();

    public static readonly CamelCaseUserDataNamingPolicy Instance = new();
}