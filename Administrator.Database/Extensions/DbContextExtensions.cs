using LinqToDB;
using LinqToDB.EntityFrameworkCore;

namespace Administrator.Database;

public static class DbContextExtensions
{
    extension(AdminDbContext db)
    {
        public ITable<GuildConfiguration> GuildConfigurations => db.GuildConfigurations.ToLinqToDBTable();
    }
}