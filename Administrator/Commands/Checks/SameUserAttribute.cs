using System.Threading.Tasks;
using Administrator.Database;
using Disqord.Bot;
using Humanizer;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class SameUserAttribute : DiscordGuildParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, DiscordGuildCommandContext context)
        {
            var entity = (IUserDbEntity) argument;

            return entity.UserId == context.Author.Id
                ? Success()
                : Failure($"This {Parameter.Name.Humanize(LetterCasing.LowerCase)} does not belong to you!");
        }
    }
}