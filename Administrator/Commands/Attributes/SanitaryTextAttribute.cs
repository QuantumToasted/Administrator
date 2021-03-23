using System;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class SanitaryTextAttribute : OverrideArgumentParserAttribute
    {
        protected SanitaryTextAttribute(Func<string, string> modification)
            : base(typeof(SanitizedStringTypeParser))
        {
            Modification = modification;
        }

        public Func<string, string> Modification { get; }
    }
}