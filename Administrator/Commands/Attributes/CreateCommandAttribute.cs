using Qmmands;

namespace Administrator.Commands
{
    public sealed class CreateCommandAttribute : CommandAttribute
    {
        public CreateCommandAttribute()
            : base("create", "add", "new")
        { }
    }
}