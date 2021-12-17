using System;
using System.Collections.Generic;

namespace ServerManagerTool.Common.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsEmpty<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasOne<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var count = 0;

            using (IEnumerator<TSource> enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (++count > 1) 
                        return false;
                }
            }

            return count == 1;
        }
    }
}
