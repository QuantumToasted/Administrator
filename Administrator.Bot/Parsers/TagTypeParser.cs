using Administrator.Database;
using Disqord.Bot.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Bot;

public sealed class TagTypeParser : DiscordGuildTypeParser<Tag>
{
    public override async ValueTask<ITypeParserResult<Tag>> ParseAsync(IDiscordGuildCommandContext context, IParameter parameter, ReadOnlyMemory<char> value)
    {
        var stringValue = value.ToString();
        var db = context.Services.GetRequiredService<AdminDbContext>();
        var tag = await db.Tags.Where(x => x.GuildId == context.GuildId).FirstOrDefaultAsync(x => x.Name == stringValue || x.Aliases.Contains(stringValue));
        var check = parameter.Checks.OfType<RequireTagOwnerAttribute>().FirstOrDefault();

        return tag switch
        {
            null => Failure($"No tag exists with the name or alias \"{stringValue}\"!"),
            not null when check is not null && tag.OwnerId != context.AuthorId => Failure($"You don't own the tag \"{tag.Name}\"!"),
            _ => Success(tag)
        };
    }
}