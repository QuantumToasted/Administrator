using System.Collections.Generic;
using Administrator.Common;

namespace Administrator.Database
{
    public sealed class Modmail
    {
        private Modmail()
        { }

        public Modmail(ulong guildId, ulong userId, bool isAnonymous, string firstMessage)
        {
            GuildId = guildId;
            UserId = userId;
            IsAnonymous = isAnonymous;
            Messages = new List<ModmailMessage> {new ModmailMessage(ModmailTarget.User, firstMessage, this)};
        }

        public int Id { get; set; }

        public ulong GuildId { get; set; }

        public ulong UserId { get; set; }

        public bool IsAnonymous { get; set; }

        public ModmailTarget? ClosedBy { get; set; }

        public List<ModmailMessage> Messages { get; set; }

        public void Reply(ModmailTarget target, string message)
        {
            Messages.Add(new ModmailMessage(target, message, this));
        }

        public void Close(ModmailTarget closedBy)
        {
            ClosedBy = closedBy;
        }
    }
}