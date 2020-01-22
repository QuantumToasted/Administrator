using System;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class LowercaseAttribute : SanitaryAttribute
    {
        public LowercaseAttribute() 
            : base(x => x.ToLowerInvariant())
        { }
    }
}