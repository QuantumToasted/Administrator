using System;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class UppercaseAttribute : SanitaryAttribute
    {
        public UppercaseAttribute() 
            : base(x => x.ToUpperInvariant())
        { }
    }
}