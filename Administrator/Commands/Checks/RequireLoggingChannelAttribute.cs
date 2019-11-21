﻿using System;
using System.Threading.Tasks;
using Administrator.Common;
using Qmmands;

namespace Administrator.Commands
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequireLoggingChannelAttribute : RequireContextAttribute
    {
        public RequireLoggingChannelAttribute(LogType requiredLogType)
            : base(ContextType.Guild)
        {
            RequiredLogType = requiredLogType;
        }

        public LogType RequiredLogType { get; }

        public override async ValueTask<CheckResult> CheckAsync(CommandContext ctx)
        {
            var baseCheck = await base.CheckAsync(ctx);
            if (!baseCheck.IsSuccessful)
                return baseCheck;

            var context = (AdminCommandContext) ctx;

            if (!(await context.Database.GetLoggingChannelAsync(context.Guild.Id, RequiredLogType) is { } channel))
                return CheckResult.Unsuccessful(context.Localize("requireloggingchannel_notfound", RequiredLogType));

            return channel.Id != context.Channel.Id
                ? CheckResult.Unsuccessful(context.Localize("requireloggingchannel", RequiredLogType))
                : CheckResult.Successful;
        }
    }
}