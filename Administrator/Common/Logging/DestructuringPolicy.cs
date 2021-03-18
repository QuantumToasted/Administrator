using System;
using System.Collections.Generic;
using System.Linq;
using Disqord;
using Disqord.Bot;
using Disqord.Gateway;
using Serilog.Core;
using Serilog.Events;

namespace Administrator.Common
{
    public sealed class DestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value is DiscordCommandContext context)
            {
                var contextProperties = new Dictionary<string, LogEventPropertyValue>();
                var authorProperties = new Dictionary<string, LogEventPropertyValue>
                {
                    ["Id"] = new ScalarValue(context.Author.Id.RawValue),
                    ["Tag"] = new ScalarValue(context.Author.Tag)
                };
                
                var channelProperties = new Dictionary<string, LogEventPropertyValue>
                {
                    ["Id"] = new ScalarValue(context.ChannelId.RawValue)
                };
                
                var messageProperties = new Dictionary<string, LogEventPropertyValue>
                {
                    ["Id"] = new ScalarValue(context.Message.Id.RawValue),
                    ["Content"] = new ScalarValue(context.Message.Content)
                };

                if (context is DiscordGuildCommandContext guildContext)
                {
                    var guild = guildContext.Guild;
                    var guildProperties = new Dictionary<string, LogEventPropertyValue>
                    {
                        ["Id"] = new ScalarValue(guild.Id.RawValue),
                        ["Name"] = new ScalarValue(guild.Name),
                        ["Members"] = new ScalarValue(guild.MemberCount)
                    };

                    channelProperties["Tag"] = new ScalarValue(guildContext.Channel.Tag);
                    contextProperties["Guild"] =
                        new StructureValue(guildProperties.Select(x => new LogEventProperty(x.Key, x.Value)));
                }
                else
                {
                    contextProperties["Guild"] = new ScalarValue(null);
                }

                contextProperties["Author"] =
                    new StructureValue(authorProperties.Select(x => new LogEventProperty(x.Key, x.Value)));

                contextProperties["Channel"] =
                    new StructureValue(channelProperties.Select(x => new LogEventProperty(x.Key, x.Value)));

                contextProperties["Message"] =
                    new StructureValue(messageProperties.Select(x => new LogEventProperty(x.Key, x.Value)));

                result = new StructureValue(contextProperties.Select(x => new LogEventProperty(x.Key, x.Value)), value.GetType().Name);
                return true;
            }

            result = null;
            return false;
        }
    }
}