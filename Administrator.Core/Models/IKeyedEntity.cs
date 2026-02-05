namespace Administrator.Core;

public interface IKeyedEntity<out TKey> where TKey : notnull
{
    TKey Id { get; }    
}