using Laylua;
using Microsoft.Extensions.DependencyInjection;

namespace Administrator.Bot;

public class DiscordHttpLibrary(AdminLuaContext context) : DiscordLuaLibrary<DiscordHttpLibrary>(context)
{
    private readonly HttpClient _http = context.CommandContext.Bot.Services.GetRequiredService<HttpClient>();
    
    public override string Name => "http";
    
    protected override IEnumerable<string> EnumerateGlobals(Lua lua)
    {
        using var httpTable = lua.CreateTable();
        
        httpTable.SetValue("get", Get);
        yield return lua.SetStringGlobal("http", httpTable);
    }

    private async Task<string?> Get(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        try
        {
            using var response = await _http.SendAsync(request, CancellationToken);
            var responseString = await response.Content.ReadAsStringAsync();

            return responseString;
        }
        catch // TODO: return error response in some way?
        {
            return null;
        }
    }
}