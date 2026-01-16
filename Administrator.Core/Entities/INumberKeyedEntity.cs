using System.Numerics;

namespace Administrator.Core;

public interface INumberKeyedEntity<out TKey> where TKey : INumber<TKey>
{
    TKey Key { get; }
}