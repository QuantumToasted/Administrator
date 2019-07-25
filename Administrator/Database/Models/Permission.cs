using System.Text;
using Administrator.Common;
using Administrator.Services;
using Discord.WebSocket;

namespace Administrator.Database
{
    public sealed class Permission
    {
        private Permission()
        { }

        public Permission(ulong? guildId, PermissionType type, bool isEnabled, string name, PermissionFilter filter, ulong? targetId)
        {
            GuildId = guildId;
            Type = type;
            IsEnabled = isEnabled;
            Name = name;
            Filter = filter;
            TargetId = targetId;
        }

        public int Id { get; set; }

        public ulong? GuildId { get; set; }

        public PermissionType Type { get; set; }

        public bool IsEnabled { get; set; }

        public string Name { get; set; }

        public PermissionFilter Filter { get; set; }

        public ulong? TargetId { get; set; }
    }
}