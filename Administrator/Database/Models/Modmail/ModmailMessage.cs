using System;
using Administrator.Common;

namespace Administrator.Database
{
    public sealed class ModmailMessage
    {
        private ModmailMessage()
        { }

        public ModmailMessage(ModmailTarget target, string text, Modmail source)
        {
            Target = target;
            Text = text;
            SourceId = source.Id;
            Timestamp = DateTimeOffset.UtcNow;
        }

        public int Id { get; set; }

        public ModmailTarget Target { get; set; }

        public string Text { get; set; }

        public Modmail Source { get; set; }

        public int SourceId { get; set; }

        public DateTimeOffset Timestamp { get; set; }
    }
}