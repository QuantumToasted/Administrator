﻿using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireContextAttribute : CheckAttribute
    {
        public RequireContextAttribute(ContextType requiredContext)
        {
            RequiredContext = requiredContext;
        }

        public ContextType RequiredContext { get; }

        public override ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            var contextType = context.IsPrivate ? ContextType.DM : ContextType.Guild;

            return RequiredContext != contextType
                ? CheckResult.Unsuccessful(context.Localize($"requirecontext_{RequiredContext.ToString().ToLower()}"))
                : CheckResult.Successful;
        }
    }
}