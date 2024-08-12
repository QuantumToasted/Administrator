using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Disqord;
using Humanizer;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public abstract class DiscordLuaLibraryBase(CancellationToken cancellationToken) : LuaLibrary
{
    private const int MAX_COMBINED_FUNCTION_CALLS = 25;
    private const int MAX_FUNCTION_CALLS = 5;
    
    private readonly List<string> _globals = [];
    private readonly ConcurrentDictionary<string, int> _calledFunctions = new();
    
    public CancellationToken CancellationToken { get; } = cancellationToken;

    public override IReadOnlyList<string> Globals => _globals.AsReadOnly();

    protected abstract IEnumerable<string> RegisterGlobals(Lua lua);

    public void RunWait(Func<CancellationToken, Task> task, bool ignoreCallLimit = false, [CallerMemberName] string? memberName = null)
    {
        Guard.IsNotNullOrWhiteSpace(memberName);

        if (!ignoreCallLimit)
        {
            if (!TryRateLimit(memberName, out var error))
                throw new Exception(error);
        }
        
        task.Invoke(CancellationToken).GetAwaiter().GetResult();
    }

    public T RunWait<T>(Func<CancellationToken, Task<T>> task, bool ignoreCallLimit = false, [CallerMemberName] string? memberName = null)
    {
        Guard.IsNotNullOrWhiteSpace(memberName);

        if (!ignoreCallLimit)
        {
            if (!TryRateLimit(memberName, out var error))
                throw new Exception(error);
        }
        
        return task.Invoke(CancellationToken).GetAwaiter().GetResult();
    }
    
    public static TMessage ConvertMessage<TMessage>(LuaTable msg)
            where TMessage : LocalMessageBase, new()
    {
        var localMessage = new TMessage();

        if (msg.TryGetValue<string, string>("content", out var content))
        {
            Guard.IsNotNullOrWhiteSpace(content);
            localMessage.WithContent(content);
        }

        if (msg.TryGetValue<string, LuaTable>("embed", out var embed))
        {
            var localEmbed = new LocalEmbed();

            if (embed.TryGetValue<string, string>("title", out var title))
            {
                Guard.IsNotNullOrWhiteSpace(title);
                localEmbed.WithTitle(title);
            }

            if (embed.TryGetValue<string, string>("description", out var description))
            {
                Guard.IsNotNullOrWhiteSpace(description);
                localEmbed.WithDescription(description);
            }

            if (embed.TryGetValue<string, int>("color", out var color))
            {
                localEmbed.WithColor(color);
            }

            if (embed.TryGetValue<string, string>("thumbnail", out var thumbnailUrl))
            {
                Guard.IsNotNullOrWhiteSpace(thumbnailUrl);
                localEmbed.WithThumbnailUrl(thumbnailUrl);
            }

            if (embed.TryGetValue<string, string>("image", out var imageUrl))
            {
                Guard.IsNotNullOrWhiteSpace(imageUrl);
                localEmbed.WithImageUrl(imageUrl);
            }

            if (embed.TryGetValue<string, LuaTable>("author", out var author))
            {
                var localAuthor = new LocalEmbedAuthor();
                
                var name = author.GetValue<string, string>("name");
                Guard.IsNotNullOrWhiteSpace(name);
                localAuthor.WithName(name);

                if (author.TryGetValue<string, string>("icon", out var iconUrl))
                {
                    Guard.IsNotNullOrWhiteSpace(iconUrl);
                    localAuthor.WithIconUrl(iconUrl);
                }

                if (author.TryGetValue<string, string>("url", out var url))
                {
                    Guard.IsNotNullOrWhiteSpace(url);
                    localAuthor.WithUrl(url);
                }

                localEmbed.WithAuthor(localAuthor);
            }

            if (embed.TryGetValue<string, LuaTable>("fields", out var fields))
            {
                for (var i = 1; i <= fields.Count(); i++)
                {
                    var localField = new LocalEmbedField();

                    var field = fields.GetValue<int, LuaTable>(i);
                    
                    Guard.IsGreaterThanOrEqualTo(field.Count(), 2);
                    Guard.IsLessThanOrEqualTo(field.Count(), 3);
                    
                    var name = field.GetValue<int, string>(1);
                    Guard.IsNotNullOrEmpty(name);
                    localField.WithName(name);
                    
                    var value = field.GetValue<int, string>(2);
                    Guard.IsNotNullOrEmpty(value);
                    localField.WithValue(value);

                    if (field.TryGetValue<int, bool>(3, out var isInline))
                        localField.WithIsInline(isInline);

                    localEmbed.AddField(localField);
                }
            }
            
            if (embed.TryGetValue<string, LuaTable>("footer", out var footer))
            {
                var localFooter = new LocalEmbedFooter();
                
                var name = footer.GetValue<string, string>("text");
                Guard.IsNotNullOrWhiteSpace(name);
                localFooter.WithText(name);

                if (footer.TryGetValue<string, string>("icon", out var iconUrl))
                {
                    Guard.IsNotNullOrWhiteSpace(iconUrl);
                    localFooter.WithIconUrl(iconUrl);
                }

                localEmbed.WithFooter(localFooter);
            }

            localMessage.AddEmbed(localEmbed);
        }

        if (localMessage is LocalInteractionMessageResponse response && msg.TryGetValue<string, bool>("ephemeral", out var isEphemeral))
        {
            response.WithIsEphemeral(isEphemeral);
        }
        
        // TODO: support attachments somehow?
        return localMessage;
    }

    protected sealed override void Open(Lua lua, bool leaveOnStack)
    {
        _globals.AddRange(RegisterGlobals(lua));
    }

    protected sealed override void Close(Lua lua)
    { }

    private bool TryRateLimit(string memberName, [NotNullWhen(false)] out string? error)
    {
        var calls = _calledFunctions.GetOrAdd(memberName, 0);

        var maxFunctionCalls = MAX_FUNCTION_CALLS;

        if (memberName is "Get") // http calls
            maxFunctionCalls = 1;

        if (calls > maxFunctionCalls)
        {
            error = $"Maximum call count of {maxFunctionCalls} exceeded for function '{memberName.Camelize()}'";
            return false;
        }

        if (_calledFunctions.Values.Sum() > MAX_COMBINED_FUNCTION_CALLS)
        {
            error = $"Maximum total function call count {MAX_COMBINED_FUNCTION_CALLS} exceeded.";
            return false;
        }
        
        _calledFunctions[memberName] = calls + 1;
        error = null;
        return true;
    }
}