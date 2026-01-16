using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Administrator.Database;

public static class DbContextExtensions
{
    extension(AdminDbContext db)
    {
        public ITable<GuildConfigurationModel> GuildConfigurations => db.GuildConfigurations.ToLinqToDBTable();
        
        public ITable<PermissionsPlusModel> Permissions => db.Permissions.ToLinqToDBTable();
        
        public ITable<GuildBlacklistedChannelModel> GuildBlacklistedChannels => db.GuildBlacklistedChannels.ToLinqToDBTable();

    }
}