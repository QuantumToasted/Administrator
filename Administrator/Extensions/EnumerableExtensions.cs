using System;
using System.Collections.Generic;
using System.Linq;

namespace Administrator.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> enumerable, Func<T, object> keySelector)
            => enumerable.GroupBy(keySelector).Select(x => x.FirstOrDefault());

        public static T GetRandomElement<T>(this IEnumerable<T> enumerable, Random random)
        {
            if (enumerable is not IList<T> list)
                list = enumerable.ToList();
            return list[random.Next(0, list.Count)];
        }
    }
}