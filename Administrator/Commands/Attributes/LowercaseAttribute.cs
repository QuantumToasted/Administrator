using System;

namespace Administrator.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class LowercaseAttribute : SanitaryAttribute
    {
        public LowercaseAttribute() 
            : base(x => x.ToLowerInvariant())
        { }
    }
}