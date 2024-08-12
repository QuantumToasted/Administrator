using System.Text.Encodings.Web;
using System.Text.Json;
using Laylua;

namespace Administrator.Bot;

public sealed class DiscordJsonLibrary(CancellationToken cancellationToken) : DiscordLuaLibraryBase(cancellationToken)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping //JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    
    private Lua _lua = null!;
    
    public override string Name => "json";
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        _lua = lua;
        using var jsonTable = lua.CreateTable();
        
        jsonTable.SetValue("serialize", Serialize);
        jsonTable.SetValue("deserialize", Deserialize);
        yield return lua.SetStringGlobal("json", jsonTable);
    }

    public string Serialize(object value)
    {
        if (value is LuaTable table)
        {
            var converted = ConvertTable(table);
            return JsonSerializer.Serialize(converted, Options);
        }

        return JsonSerializer.Serialize(value, Options);

        static object ConvertTable(LuaTable table)
        {
            var isArray = true;
            var currentIndex = 1L;
            var dict = new Dictionary<object, object?>();
            foreach (var (key, value) in table)
            {
                if (isArray && (key.Value is not long i || i != currentIndex++))
                    isArray = false;

                var keyName = key.Value!.ToString()!;

                if (value.Value is LuaTable subTable)
                {
                    dict[keyName] = ConvertTable(subTable);
                }
                else
                {
                    dict[keyName] = value.Value;
                }
            }

            if (isArray)
                return dict.Values.ToArray();

            return dict;
        }
    }

    public LuaTable? Deserialize(string json)
    {
        JsonElement? root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(json);
            if (!root.HasValue)
                return null;
        }
        catch
        {
            return null;
        }

        var table = _lua.CreateTable();

        if (root.Value.ValueKind == JsonValueKind.Array)
        {
            var index = 1;
            foreach (var elem in root.Value.EnumerateArray())
            {
                table.SetValue(index++, GetElementValue(elem, _lua));
            }
        }
        else
        {
            foreach (var elem in root.Value.EnumerateObject())
            {
                table.SetValue(elem.Name, GetElementValue(elem.Value, _lua));
            }
        }
        
        return table;

        static object? GetElementValue(JsonElement element, Lua lua)
        {
            switch (element.ValueKind)
            {
                //case JsonValueKind.Undefined:
                //    break;
                case JsonValueKind.Object:
                {
                    var table = lua.CreateTable();
                    foreach (var elem in element.EnumerateObject())
                    {
                        table.SetValue(elem.Name, GetElementValue(elem.Value, lua));
                    }

                    return table;
                }
                case JsonValueKind.Array:
                {
                    var table = lua.CreateTable();
                    var index = 1; // I love lua!
                    foreach (var elem in element.EnumerateArray())
                    {
                        table.SetValue(index++, GetElementValue(elem, lua));
                    }

                    return table;
                }
                case JsonValueKind.String:
                {
                    return element.GetString();
                }
                case JsonValueKind.Number:
                {
                    return element.TryGetInt64(out var longValue)
                        ? longValue
                        : element.GetDecimal();
                }
                case JsonValueKind.True:
                {
                    return true;
                }
                case JsonValueKind.False:
                {
                    return false;
                }
                case JsonValueKind.Null:
                {
                    return null;
                }
                case JsonValueKind.Undefined:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}