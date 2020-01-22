using System;
using System.Threading.Tasks;

namespace Administrator.Services
{
    public interface IHandler
    { }

    public interface IHandler<in T1> : IHandler
        where T1 : EventArgs
    {
        Task HandleAsync(T1 args);
    }
}