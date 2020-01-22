using System;
using System.Linq;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class AlphanumericAttribute : SanitaryAttribute
    {
        public AlphanumericAttribute() 
            : base(x => string.Join(string.Empty, x.Where(char.IsLetterOrDigit)))
        { }
    }
}