namespace Administrator.Core;

public interface IRevocablePunishment : IPunishment
{
    DateTimeOffset? RevokedAt { get; }
    
    UserSnapshot? Revoker { get; }
    
    string? RevocationReason { get; }
    
    DateTimeOffset? AppealedAt { get; }
    
    string? AppealText { get; }
}