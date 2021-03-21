using System;
using System.Linq;
using System.Threading.Tasks;
using Administrator.Database;
using Administrator.Extensions;
using Administrator.Services;
using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace Administrator.Commands.Parsers
{
    public sealed class EmojiTypeParser<TEmoji> : TypeParser<TEmoji>
        where TEmoji : IEmoji
    {
        private readonly Random _random;
        private readonly EmojiService _emojiService;
        private readonly IServiceProvider _services;

        public EmojiTypeParser(IServiceProvider services)
        {
            _random = services.GetRequiredService<Random>();
            _emojiService = services.GetRequiredService<EmojiService>();
        }
        
        public override async ValueTask<TypeParserResult<TEmoji>> ParseAsync(Parameter parameter, string value, CommandContext _)
        {
            var context = (DiscordCommandContext) _;
            
            if (_emojiService.TryParseEmoji(value, out var parsedEmoji) && parsedEmoji is TEmoji emoji)
            {
                return Success(emoji);
            }

            using var scope = _services.CreateScope();
            await using var ctx = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
            var allEmojis = await ctx.GetAllBigEmojisAsync();
            var approvedEmojis = allEmojis.OfType<ApprovedBigEmoji>().ToList();
            var approvedIds = approvedEmojis.Select(x => x.Id).ToList();

            var guildEmojis = (context as DiscordGuildCommandContext)?.Guild?.Emojis.Values.Where(x =>
                x.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase) ||
                Snowflake.TryParse(value, out var i) && x.Id == i).OfType<TEmoji>().ToList();

            if (guildEmojis?.Count > 0)
            {
                return Success(guildEmojis.GetRandomElement(_random));
            }
            
            // TODO: Enumerate over all valid emojis, find a random one with the same name as value
            return Failure("This parser has not been finished yet.");
        }
    }
}