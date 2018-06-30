using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kelson.Common.Async.Tests
{
    public class Actor_Should
    {
        enum First
        {
            Unset,
            Fib,
            Odds
        }

        /// <summary>
        /// HELP WANTED - better way to test for concurrent access?
        /// </summary>        
        [Fact]
        public async Task ProtectAgainstConcurrentModification()
        {
            var listActor = new Actor<List<int>>(new List<int> { 0, 1 });

            var first = First.Unset;

            Task fib = listActor.Do(list =>
            {
                first = first == First.Unset ? First.Fib : first;
                for (int i = 0; i < 10000; i++)
                    list.Add(list[list.Count - 1] + list[list.Count - 2]);
            });

            Task odds = listActor.Do(list =>
            {
                first = first == First.Unset ? First.Odds : first;
                for (int i = 1; i < list.Count; i += 2)
                    list[i] = i;
            });

            await fib;
            await odds;

            await listActor.Do(list =>
            {
                int a = 0;
                int b = 1;
                int next = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if (i % 2 == 0 || first == First.Odds)
                        list[i].Should().Be(a);
                    else
                        list[i].Should().Be(i);
                    next = a + b;
                    a = b;
                    b = next;
                }                
            });
        }
    }
}
