using System;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCommandResult : CommandResult
    {
        public AdminCommandResult(TimeSpan executionTime, string text, LocalEmbed embed, LocalAttachment attachment, bool isSuccessful)
        {
            ExecutionTime = executionTime;
            Text = text;
            Embed = embed;
            Attachment = attachment;
            IsSuccessful = isSuccessful;
        }
        
        public TimeSpan ExecutionTime { get; }
        
        public string Text { get; }
        
        public LocalEmbed Embed { get; }
        
        public LocalAttachment Attachment { get; }
        
        public override bool IsSuccessful { get; }
        
        public static implicit operator ValueTask<AdminCommandResult>(AdminCommandResult result)
            => new ValueTask<AdminCommandResult>(result);
    }
}