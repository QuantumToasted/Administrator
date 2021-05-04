using Qmmands;

namespace Administrator.Commands
{
    public sealed class DeleteCommandAttribute : CommandAttribute
    {
        public DeleteCommandAttribute()
            : base("delete", "del", "remove", "rem", "rm")
        { }
    }
}