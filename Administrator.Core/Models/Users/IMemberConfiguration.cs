using Disqord;

namespace Administrator.Core;

public interface IMemberConfiguration : IGuildEntity
{
    string Blurb { get; }
    
    DateTimeOffset? NextDemeritPointDecay { get; }
}