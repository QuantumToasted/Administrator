using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Administrator.Database
{
    public interface ICleanupHandler<in TArgs>
        where TArgs : EventArgs
    {
        Task<List<object>> FindMatches(AdminDbContext ctx, TArgs e);
    }
}