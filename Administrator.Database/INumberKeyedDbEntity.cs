using System.Numerics;

namespace Administrator.Database;

public interface INumberKeyedDbEntity<T>
    where T : INumber<T>
{
    T Id { get; }
}