using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public sealed class MustBeAttribute : ParameterCheckBaseAttribute
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

        public override Task<CheckResult> CheckAsync(object argument, ICommandContext ctx, IServiceProvider provider)
        {
            var context = (AdminCommandContext) ctx;

            if (_isOperator)
            {
                var value = (int) argument;
                switch (Operator)
                {
                    case Operator.GreaterThan:
                        return Task.FromResult(value > Value
                            ? CheckResult.Successful
                            : CheckResult.Unsuccessful(context.Language.Localize("operator_greaterthan", Value)));
                    case Operator.EqualTo:
                        return Task.FromResult(value == Value
                            ? CheckResult.Successful
                            : CheckResult.Unsuccessful(context.Language.Localize("operator_equalto", Value)));
                    case Operator.LessThan:
                        return Task.FromResult(value < Value
                            ? CheckResult.Successful
                            : CheckResult.Unsuccessful(context.Language.Localize("operator_lessthan", Value)));
                    case Operator.DivisibleBy:
                        return Task.FromResult(value % Value == 0
                            ? CheckResult.Successful
                            : CheckResult.Unsuccessful(context.Language.Localize("operator_divisibleby", Value)));
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var str = (string) argument;
            switch (StringLength)
            {
                case StringLength.LongerThan:
                    return Task.FromResult(str.Length > Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Language.Localize("stringvalue_longerthan", Value)));
                case StringLength.Exactly:
                    return Task.FromResult(str.Length == Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Language.Localize("stringvalue_exactly", Value)));
                case StringLength.ShorterThan:
                    return Task.FromResult(str.Length < Value
                        ? CheckResult.Successful
                        : CheckResult.Unsuccessful(context.Language.Localize("stringvalue_shorterthan", Value)));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}