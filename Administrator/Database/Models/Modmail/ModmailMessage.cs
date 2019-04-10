using System;
using Administrator.Common;

namespace Administrator.Database
{
    public sealed class ModmailMessage
    {
        private ModmailMessage()
        { }

        public ModmailMessage(ModmailTarget target, string message, Modmail source)
        {
            Target = target;
            Message = message;
            SourceId = source.Id;
            Timestamp = DateTimeOffset.UtcNow;
        }

        public int Id { get; set; }

        public ModmailTarget Target { get; set; }

        public string Message { get; set; }

        public Modmail Source { get; set; }

        public int SourceId { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}