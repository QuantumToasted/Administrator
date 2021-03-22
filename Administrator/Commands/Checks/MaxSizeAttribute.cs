using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class MaxSizeAttribute : ParameterCheckAttribute
    {
        private readonly double _value;
        private readonly FileSize _measure;
        
        public MaxSizeAttribute(double value, FileSize measure)
        {
            _value = value;
            _measure = measure;
        }
        
        public override async ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var upload = (Upload) argument;
            var maxSizeInBytes = (long) (_value * (long) _measure);

            return await upload.VerifySizeAsync(maxSizeInBytes)
                ? Success()
                : Failure($"The provided file must be {_value:F}{_measure} or smaller in size.");
        }
    }
}