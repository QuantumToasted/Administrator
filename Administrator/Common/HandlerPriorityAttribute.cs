using System;

namespace Administrator.Common
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HandlerPriorityAttribute : Attribute
    {
        public HandlerPriorityAttribute(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}