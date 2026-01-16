using System.Numerics;
using Disqord;

namespace Administrator.Core;

public static class EntityExtensions
{
    extension<TEntity>(TEntity entity) where TEntity : ITimestampedEntity
    {
        public string Format(Markdown.TimestampFormat format = Markdown.TimestampFormat.RelativeTime)
            => Markdown.Timestamp(entity.Timestamp, format);
    }

    extension<TEntity, TKey>(TEntity entity) where TEntity : INumberKeyedEntity<TKey>
        where TKey : INumber<TKey>
    {
        public string Format(bool code = true)
            => code ? Markdown.Code($"[#{entity.Key}]") : $"[#{entity.Key}]";
    }
}