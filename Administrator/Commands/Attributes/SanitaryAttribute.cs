using System;
using Qmmands;

namespace Administrator.Commands.Attributes
{
    public abstract class SanitaryAttribute : OverrideTypeParserAttribute
    {
        protected SanitaryAttribute(Func<string, string> transformation)
            : base(typeof(SanitaryStringParser))
        {
            Transformation = transformation;
        }

        public Func<string, string> Transformation { get; }
    }
}