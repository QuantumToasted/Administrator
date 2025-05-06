using Disqord;

namespace Administrator.Core;

public interface ILuaCommand : IGuildEntity
{
    string Metadata { get; }
    
    string Command { get; }
    
    string Persistence { get; }
}