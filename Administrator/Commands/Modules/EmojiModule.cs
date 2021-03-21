using System;
using System.Net.Http;
using Administrator.Services;
using Disqord.Bot;
using Qmmands;

namespace Administrator.Commands
{
    [Group("emoji", "em", "e")]
    public class EmojiModule : DiscordModuleBase
    {
        public EmojiService EmojiService { get; set; }
        
        public Random Random { get; set; }
        
        public HttpClient Http { get; set; }
    }
}