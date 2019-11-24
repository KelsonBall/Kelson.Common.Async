using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kelson.Common.Async
{
    public static class TaskOfEnumerableExtensions
    {
        public static async Task<T> SingleAsync<T>(this Task<IEnumerable<T>> source)
            => (await source).Single();

        public static async Task<T> FirstAsync<T>(this Task<IEnumerable<T>> source)
            => (await source).First();

        public static async Task<IEnumerable<T>> WhereAsync<T>(this Task<IEnumerable<T>> source, Func<T, bool> predicate)
            => (await source).Where(predicate);

        public static async Task<IEnumerable<T2>> SelectAsync<T, T2>(this Task<IEnumerable<T>> source, Func<T, T2> selector)
            => (await source).Select(selector);
    }
}
