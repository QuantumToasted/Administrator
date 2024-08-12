using Laylua;

namespace Administrator.Bot;

public class DiscordHttpLibrary(HttpClient http, CancellationToken cancellationToken) : DiscordLuaLibraryBase(cancellationToken)
{
    public override string Name => "http";
    
    protected override IEnumerable<string> RegisterGlobals(Lua lua)
    {
        using var httpTable = lua.CreateTable();
        
        httpTable.SetValue("get", Get);
        yield return lua.SetStringGlobal("http", httpTable);
    }

    public string? Get(string url/*, LuaTable? headers = null*/)
    {
        return RunWait(async ct =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            /*
            if (headers is not null)
            {
                foreach (var (k, v) in headers)
                {
                    try
                    {
                        var name = k.Value!.ToString()!;
                        Guard.IsNotNullOrWhiteSpace(name);

                        var value = v.Value as string;
                        Guard.IsNotNullOrWhiteSpace(value);

                        request.Headers.Add(name, value);
                    }
                    catch
                    {
                        // ignore invalid headers
                    }
                }
            }
            */

            try
            {
                using var response = await http.SendAsync(request, ct);
                var responseString = await response.Content.ReadAsStringAsync(ct);

                return responseString;
            }
            catch // TODO: return error response in some way?
            {
                return null;
            }
        });
    }
}