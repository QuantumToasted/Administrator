using Qmmands;

namespace Administrator.Commands
{
    public sealed class ListCommandAttribute : CommandAttribute
    {
        public ListCommandAttribute()
            : base ("list", "")
        { }
    }
}