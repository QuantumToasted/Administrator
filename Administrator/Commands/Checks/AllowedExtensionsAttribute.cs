using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Common;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public class AllowedExtensionsAttribute : ParameterCheckAttribute
    {
        private readonly IReadOnlyList<string> _allowedExtensions;
        
        public AllowedExtensionsAttribute(params string[] allowedExtensions)
        {
            if (allowedExtensions.Length == 0)
                throw new ArgumentException("More than one extension must be supplied.", nameof(allowedExtensions));
            
            _allowedExtensions = allowedExtensions.ToList();
        }
        
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var upload = (Upload) argument;
            return _allowedExtensions.Contains(upload.Extension)
                ? Success()
                : Failure(
                    $"Only files of the following type(s) are allowed: {string.Join(", ", _allowedExtensions.Select(Markdown.Code))}");
        }
    }
}