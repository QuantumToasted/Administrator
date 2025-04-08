using System.Numerics;

namespace Administrator.Database;

public interface INumberKeyedDbEntity
{
    int Id { get; }
    //Type NumberType { get; }
}

/*
public interface INumberKeyedDbEntity<out T> : INumberKeyedDbEntity
    where T : INumber<T>
{
    new T Id { get; }

    object INumberKeyedDbEntity.Id => Id;
    Type INumberKeyedDbEntity.NumberType => typeof(T);
}
*/