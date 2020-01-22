using System;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class MinimumCooldownAttribute : CooldownAttribute
    {
        public static readonly TimeSpan MinimumCooldown = TimeSpan.FromSeconds(2);

        public MinimumCooldownAttribute()
            : this(2, CooldownMeasure.Seconds)
        { }

        public MinimumCooldownAttribute(double per, CooldownMeasure measure)
            : base(1, per, measure)
        { }
    }
}