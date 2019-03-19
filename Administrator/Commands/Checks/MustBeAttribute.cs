using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;
using Remotion.Linq.Parsing;

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
                    Operator.GreaterThan => value > Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Localize("operator_greaterthan", Value)),
                    Operator.EqualTo => value == Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Localize("operator_equalto", Value)),
                    Operator.LessThan => value < Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Localize("operator_lessthan", Value)),
                    Operator.DivisibleBy => value % Value == 0
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Localize("operator_divisibleby", Value)),
                        _ => throw new ArgumentOutOfRangeException(nameof(Operator))
                };
            }

            var str = (string) argument;
            return StringLength switch
            {
                StringLength.LongerThan => str.Length > Value
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(context.Localize("stringvalue_longerthan", Value)),
                StringLength.Exactly => str.Length == Value
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(context.Localize("stringvalue_exactly", Value)),
                StringLength.ShorterThan => str.Length < Value
                    ? CheckResult.Successful
                    : CheckResult.Unsuccessful(context.Localize("stringvalue_shorterthan", Value)),
                _ => throw new ArgumentOutOfRangeException(nameof(StringLength))
            };
        }
    }
}