using System;

namespace Administrator.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class UppercaseAttribute : SanitaryAttribute
    {
        public UppercaseAttribute() 
            : base(x => x.ToLowerInvariant())
        { }
    }
}