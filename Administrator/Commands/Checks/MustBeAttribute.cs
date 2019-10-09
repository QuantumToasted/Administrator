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

        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;

            string result;
            if (_isOperator)
            {
                var value = (int) argument;
                result = Operator switch
                {
                    Operator.GreaterThan when value < Value => "operator_greaterthan",
                    Operator.EqualTo when value != Value => "operator_equalto",
                    Operator.LessThan when value > Value => "operator_lessthan",
                    Operator.DivisibleBy when value % Value != 0 => "operator_divisibleby",
                    _ => string.Empty
                };
            }
            else
            {
                var str = (string)argument;
                result = StringLength switch
                {
                    StringLength.LongerThan when str.Length < Value => "stringvalue_longerthan",
                    StringLength.Exactly when str.Length != Value => "stringvalue_exactly",
                    StringLength.ShorterThan when str.Length > Value => "stringvalue_shorterthan",
                    _ => string.Empty
                };
            }
            
            return !string.IsNullOrWhiteSpace(result)
                ? CheckResult.Unsuccessful(context.Localize(result, Value))
                : CheckResult.Successful;
        }
    }
}