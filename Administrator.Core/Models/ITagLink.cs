using Disqord;

namespace Administrator.Core;

public interface ITagLink : IKeyedEntity<int>, IGuildEntity
{
    public string From { get; }
    
    public string To { get; }
    
    public string? Label { get; }
    
    public LocalButtonComponentStyle Style { get; }
    
    public bool IsEphemeral { get; }
}