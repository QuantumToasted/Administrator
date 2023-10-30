using System.Runtime.CompilerServices;
using Disqord;
using Humanizer;
using Laylua;
using Qommon;

namespace Administrator.Bot;

public abstract class DiscordLuaLibraryBase : LuaLibrary
{
    private readonly List<string> _globals = new();

    private readonly HashSet<string> _invokedFunctions = new();

    private protected Lua _lua = null!;

    public override IReadOnlyList<string> Globals => _globals.AsReadOnly();

    protected abstract IEnumerable<string> RegisterGlobals(Lua lua);

    protected void RunWait(Func<Task> task, [CallerMemberName] string? memberName = null)
    {
        Guard.IsNotNullOrWhiteSpace(memberName);

        if (!_invokedFunctions.Add(memberName))
            throw new Exception($"{memberName.Camelize()} rate-limit exceeded for this action");
        
        task.Invoke().GetAwaiter().GetResult();
    }

    protected T RunWait<T>(Func<Task<T>> task, [CallerMemberName] string? memberName = null)
    {
        Guard.IsNotNullOrWhiteSpace(memberName);

        if (!_invokedFunctions.Add(memberName))
            throw new Exception($"{memberName.Camelize()} rate-limit exceeded for this action");
        
        return task.Invoke().GetAwaiter().GetResult();
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
        
        // TODO: attachments
        return localMessage;
    }

    protected sealed override void Open(Lua lua, bool leaveOnStack)
    {
        _lua = lua;
        _globals.AddRange(RegisterGlobals(lua));
    }

    protected sealed override void Close(Lua lua)
    { }
}