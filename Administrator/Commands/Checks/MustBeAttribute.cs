using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class MustBeAttribute : ParameterCheckAttribute
    {
        private readonly bool _isOperator;

        public MustBeAttribute(Operator oper, int value)
        {
            Operator = oper;
            Value = value;
            _isOperator = true;
        }

        public MustBeAttribute(StringLength stringLength, int value)
        {
            StringLength = stringLength;
            Value = value;
        }

        public Operator Operator { get; }

        public StringLength StringLength { get; }

        public int Value { get; }

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;

            if (_isOperator)
            {
                var value = (int) argument;
                return Operator switch
                {
                    Operator.GreaterThan => value < Value
                        ? CheckResult.Unsuccessful(context.Localize("operator_greaterthan", Value))
                        : CheckResult.Successful,
                    Operator.EqualTo => value != Value
                        ? CheckResult.Unsuccessful(context.Localize("operator_equalto", Value))
                        : CheckResult.Successful,
                    Operator.LessThan => value > Value
                        ? CheckResult.Unsuccessful(context.Localize("operator_lessthan", Value))
                        : CheckResult.Successful,
                    Operator.DivisibleBy => value % Value != 0
                        ? CheckResult.Unsuccessful(context.Localize("operator_divisibleby", Value))
                        : CheckResult.Successful,
                        _ => throw new ArgumentOutOfRangeException(nameof(Operator))
                };
            }

            var str = (string) argument;
            return StringLength switch
            {
                StringLength.LongerThan => str.Length < Value
                    ? CheckResult.Unsuccessful(context.Localize("stringvalue_longerthan", Value))
                    : CheckResult.Successful,
                StringLength.Exactly => str.Length != Value
                    ? CheckResult.Unsuccessful(context.Localize("stringvalue_exactly", Value))
                    : CheckResult.Successful,
                StringLength.ShorterThan => str.Length > Value
                    ? CheckResult.Unsuccessful(context.Localize("stringvalue_shorterthan", Value))
                    : CheckResult.Successful,
                _ => throw new ArgumentOutOfRangeException(nameof(StringLength))
            };
        }
    }
}