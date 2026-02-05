namespace Administrator.Core;

public enum AppealStatus
{
    Sent,
    NeedsInfo,
    Updated,
    Rejected,
    Ignored
}

public interface IRevocablePunishment : IPunishment
{
    DateTimeOffset? RevokedAt { get; }
    
    UserSnapshot? Revoker { get; }
    
    string? RevocationReason { get; }
    
    DateTimeOffset? AppealedAt { get; }
    
    string? AppealText { get; }
    
    AppealStatus? AppealStatus { get; }
}