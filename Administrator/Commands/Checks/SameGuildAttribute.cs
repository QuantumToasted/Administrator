﻿using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class SameGuildAttribute : DiscordGuildParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, DiscordGuildCommandContext context)
        {
            var entity = (IGuildDbEntity) argument;

            return entity.GuildId == context.GuildId
                ? Success()
                : Failure($"This {Parameter.Name.Humanize(LetterCasing.LowerCase)} is not from this server!");
        }
    }
}