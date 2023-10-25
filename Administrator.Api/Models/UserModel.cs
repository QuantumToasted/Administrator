using System.Text.Json.Serialization;

namespace Administrator.Api;

public sealed record UserModel(
    [property: JsonPropertyName("id")]
        ulong Id, 
    [property: JsonPropertyName("tag")]
        string Tag);