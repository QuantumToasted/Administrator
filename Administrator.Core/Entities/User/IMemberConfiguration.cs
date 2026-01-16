namespace Administrator.Core;

public interface IMemberConfiguration
{
    string Blurb { get; }
    
    DateTimeOffset? NextDemeritPointDecay { get; }
}