using Administrator.Core;
using Disqord;
using LinqToDB;
using LinqToDB.Async;

namespace Administrator.Database;

public static class TableExtensions
{
    extension<TTable>(TTable table) where TTable : ITable<IPermissionsPlus>
    {
        public async Task<IReadOnlyCollection<IPermissionsPlus>> GetPermissions(IMember member)
        {
            var guildId = member.GuildId.RawValue;
            var userId = member.Id.RawValue;
            var roleIds = member.RoleIds.Select(x => x.RawValue).ToArray();

            var permissions = await table
                .Where(x => x.GuildId == guildId && (x.TargetId == userId || roleIds.Contains(x.TargetId)))
                .ToListAsync();

            return permissions;
        }
    }
}