using System;
using System.Collections.Generic;
using System.Linq;

namespace Administrator.Extensions
{
    public static class EnumerableExtensions
    {
        public static List<List<T>> SplitBy<T>(this List<T> list, int count)
        {
            var newList = new List<List<T>>();

            if (list.Count <= count)
            {
                newList.Add(list);
            }
            else
            {
                for (var i = 0; i < list.Count; i += count)
                {
                    newList.Add(list.GetRange(i, Math.Min(count, list.Count - i)));
                }
            }

            return newList;
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
            => enumerable.GroupBy(keySelector).Select(x => x.FirstOrDefault());

        public static bool ContainsAny<T>(this IEnumerable<T> enumerable, params T[] ts)
        {
            if (ts.Length == 0)
                throw new Exception();

            var list = enumerable as IList<T> ?? enumerable.ToList();
            foreach (var t in ts)
            {
                if (list.Contains(t))
                    return true;
            }

            return false;
        }

        public static T GetRandomElement<T>(this IEnumerable<T> enumerable, Random random)
        {
            random ??= new Random();
            if (!(enumerable is IList<T> list))
                list = enumerable.ToList();
            return list[random.Next(list.Count)];
        }
    }
}