using System;
using System.Collections.Generic;

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
    }
}