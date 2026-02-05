using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Administrator.Database;

public sealed class LinqToDBForEFTools(string connectionString) : LinqToDBForEFToolsImplDefault
{
    public override EFConnectionInfo ExtractConnectionInfo(IDbContextOptions? options)
    {
        return new EFConnectionInfo
        {
            ConnectionString = connectionString
        };
    }
}