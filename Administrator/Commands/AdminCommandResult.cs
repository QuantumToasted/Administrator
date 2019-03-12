using System;
using System.Threading.Tasks;
using Administrator.Common;
using Discord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class AdminCommandResult : CommandResult
    {
        public AdminCommandResult(TimeSpan executionTime, string text, Embed embed, MessageFile file, bool isSuccessful)
        {
            ExecutionTime = executionTime;
            Text = text;
            Embed = embed;
            File = file;
            IsSuccessful = isSuccessful;
        }
        
        public TimeSpan ExecutionTime { get; }
        
        public string Text { get; }
        
        public Embed Embed { get; }
        
        public MessageFile File { get; }
        
        public override bool IsSuccessful { get; }
        
        public static implicit operator Task<AdminCommandResult>(AdminCommandResult result)
            => Task.FromResult(result);
    }
}