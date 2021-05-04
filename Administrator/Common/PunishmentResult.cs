using Administrator.Database;

namespace Administrator.Common
{
    public sealed class PunishmentResult<TPunishment>
        where TPunishment : Punishment
    {
        public PunishmentResult(TPunishment punishment)
        {
            Punishment = punishment;
        }
        
        public PunishmentResult(string failureReason)
        {
            FailureReason = failureReason;
        }
        
        public TPunishment Punishment { get; }

        public string FailureReason { get; }

        public bool IsSuccessful => string.IsNullOrWhiteSpace(FailureReason);
    }
}