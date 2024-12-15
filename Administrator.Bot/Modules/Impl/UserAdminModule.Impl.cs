using System.Text;
using System.Text.RegularExpressions;
using Administrator.Core;
using Disqord;
using Disqord.Bot.Commands.Application;
using Disqord.Extensions.Interactivity.Menus.Paged;
using Disqord.Gateway;
using Qmmands;

namespace Administrator.Bot;

public sealed partial class UserAdminModule : DiscordApplicationGuildModuleBase
{
        public partial async Task<IResult> Search(string text, bool regex, bool usernames, bool nicknames, bool globalNames, int maxDistance)
    {
        text = text.ToLowerInvariant();
        
        await Deferral();
        var allMembers = Bot.GetMembers(Context.GuildId).Values.ToList();

        var searchRegex = regex ? new Regex(text) : null;

        var matches = new List<(IMember Member, int? Distance)>();
        foreach (var member in allMembers)
        {
            var namesToCheck = new List<string>();
            if (nicknames && !string.IsNullOrWhiteSpace(member.Nick))
                namesToCheck.Add(member.Nick);
            
            if (usernames)
                namesToCheck.Add(member.Name);
            
            if (globalNames && !string.IsNullOrWhiteSpace(member.GlobalName))
                namesToCheck.Add(member.GlobalName);

            foreach (var name in namesToCheck)
            {
                if (searchRegex?.IsMatch(name) == true)
                {
                    matches.Add((member, null));
                    break;
                }

                var distance = GetWeightedLevenshteinDistance(name.ToLowerInvariant(), text);
                if (distance <= maxDistance)
                {
                    matches.Add((member, distance));
                    break;
                }
            }
        }

        var pages = matches.OrderBy(x => x.Distance)
            .Chunk(10)
            .Select(chunk =>
            {
                var embed = new LocalEmbed()
                    .WithUnusualColor();

                if (regex)
                {
                    embed.WithTitle($"Members matching the regex {text}");
                }
                else
                {
                    embed.WithTitle($"Members approximately matching \"{text}\"");
                }

                foreach (var (member, _) in chunk)
                {
                    var valueBuilder = new StringBuilder();

                    if (nicknames && !string.IsNullOrWhiteSpace(member.Nick))
                        valueBuilder.AppendNewline($"Nickname: {member.Nick}");
            
                    if (usernames)
                        valueBuilder.AppendNewline($"Username: {member.Name}");
            
                    if (globalNames && !string.IsNullOrWhiteSpace(member.GlobalName))
                        valueBuilder.AppendNewline($"Global name: {member.GlobalName}");

                    embed.AddField(member.Id.ToString(), valueBuilder.ToString());
                }
                
                return new Page().AddEmbed(embed);
            }).ToList();
        
        return pages.Count switch
        {
            0 => Response("No members could be found matching your input rules."),
            1 => Response(pages[0].Embeds.Value[0]),
            _ => Menu(new AdminInteractionMenu(new AdminPagedView(pages), Context.Interaction))
        };

        static int GetWeightedLevenshteinDistance(string source, string other)
        {
            var distance = source.GetLevenshteinDistanceTo(other);
            if (source.Contains(other))
                distance /= 2;

            return distance;
        }
    }
}