using System;
using System.Collections.Generic;
using System.Linq;

namespace Administrator.Extensions
{
    public static class EnumerableExtensions
    {
        public static int FirstIndexOf<T>(this IList<T> list, Func<T, bool> func)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var t = list[i];
                if (func(t))
                    return i;
            }

            return -1;
        }
        
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> enumerable, Func<T, object> keySelector)
            => enumerable.GroupBy(keySelector).Select(x => x.FirstOrDefault());

        public static T GetRandomElement<T>(this IEnumerable<T> enumerable, Random random)
        {
            if (enumerable is not IList<T> list)
                list = enumerable.ToList();
            return list[random.Next(0, list.Count)];
        }
        
        public static List<List<T>> SplitBy<T>(this IEnumerable<T> enumerable, int count)
        {
            var newList = new List<List<T>>();

            var list = enumerable as List<T> ?? enumerable.ToList();

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
    }
}