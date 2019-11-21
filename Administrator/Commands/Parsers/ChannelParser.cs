﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Qmmands;

namespace Administrator.Commands
{
    public sealed class ChannelParser<TChannel> : TypeParser<TChannel>
        where TChannel : CachedGuildChannel
    {
        public override ValueTask<TypeParserResult<TChannel>> ParseAsync(Parameter parameter, string value, CommandContext ctx)
        {
            var context = (AdminCommandContext) ctx;
            if (context.IsPrivate)
                return TypeParserResult<TChannel>.Unsuccessful(context.Localize("requirecontext_guild"));

            TChannel channel = null;
            IEnumerable<TChannel> channels;

            if (typeof(CachedVoiceChannel).IsAssignableFrom(typeof(TChannel)))
                channels = context.Guild.VoiceChannels.OfType<TChannel>().ToList();
            else if (typeof(CachedCategoryChannel).IsAssignableFrom(typeof(TChannel)))
                channels = context.Guild.CategoryChannels.OfType<TChannel>().ToList();
            else if (typeof(CachedTextChannel).IsAssignableFrom(typeof(TChannel)))
                channels = context.Guild.TextChannels.OfType<TChannel>().ToList();
            else channels = context.Guild.Channels.OfType<TChannel>().ToList();

            // Parse by channel ID (or text channel mention)
            if (Snowflake.TryParse(value, out var id) || Discord.TryParseChannelMention(value, out id))
            {
                channel = channels.FirstOrDefault(x => x.Id == id);
            }

            // Parse by channel name
            if (channel is null)
            {
                var matchingChannels = channels
                    .Where(x => x.Name.Equals(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingChannels.Count > 1)
                {
                    return TypeParserResult<TChannel>.Unsuccessful(context.Localize("channelparser_multiple"));
                }

                channel = matchingChannels.FirstOrDefault();
            }

            return !(channel is null)
                ? TypeParserResult<TChannel>.Successful(channel)
                : TypeParserResult<TChannel>.Unsuccessful(context.Localize("channelparser_notfound"));
        }
    }
}