using System;

namespace Administrator.Database
{
    public interface IUpdateHandler<in TArgs>
        where TArgs : EventArgs
    {
        void Update(AdminDbContext ctx, TArgs e);
    }
}