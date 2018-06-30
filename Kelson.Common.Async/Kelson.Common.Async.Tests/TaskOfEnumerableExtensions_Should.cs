using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Kelson.Common.Async.Tests
{
    public class TaskOfEnumerableExtensions_Should
    {
        private Task<IEnumerable<int>> SlowCollection => 
            Task.Run(async () => 
            {
                await Task.Delay(1000);
                return new int[] { 1, 2, 3, 4 }.AsEnumerable();
            });

        private Task<IEnumerable<int>> SlowSingleItemCollection =>
            Task.Run(async () =>
            {
                await Task.Delay(1000);
                return new int[] { 1 }.AsEnumerable();
            });


        [Fact]
        public async Task ReturnSingleWhenCompleted()
        {
            (await SlowSingleItemCollection.SingleAsync()).Should().Be(1);
            ((Func<Task>)(async () => await SlowCollection.SingleAsync())).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public async Task ReturnFirstWhenCompleted()
        {
            (await SlowSingleItemCollection.FirstAsync()).Should().Be(1);
            (await SlowCollection.FirstAsync()).Should().Be(1);
        }

        [Fact]
        public async Task ReturnFilteredWhenCompleted()
        {
            (await SlowCollection.WhereAsync(i => i % 2 == 0)).Should().OnlyContain(i => i % 2 == 0);            
        }

        [Fact]
        public async Task ReturnSelectedWhenCompleted()
        {
            (await SlowCollection.SelectAsync(i => i.ToString())).Should().ContainInOrder(new string[] { "1", "2", "3", "4" });
        }
    }
}
