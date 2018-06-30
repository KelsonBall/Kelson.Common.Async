using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kelson.Common.Async.Tests
{
    public class StaticDispatch_Should
    {
        [Fact]
        public async Task DispatchToRegisteredHandler()
        {
            int counter1 = 0;
            Action<Action> dispatchCounter1 = a =>
            {
                counter1++;
                a();
            };

            int counter2 = 0;
            Action<Action> dispatchCounter2 = a =>
            {
                counter2++;
                a();
            };

            StaticDispatch.RegisterCallbackDispatcher(dispatchCounter1, 1);
            StaticDispatch.RegisterCallbackDispatcher(dispatchCounter2, 2);

            Task.Run(async () => await Task.Delay(100)).Then(() => counter1.Should().Be(1), 1);
            Task.Run(async () => await Task.Delay(200)).Then(() => counter1.Should().Be(2), 1);
            Task.Run(async () => await Task.Delay(100)).Then(() => counter2.Should().Be(1), 2);
            Task.Run(async () => await Task.Delay(200)).Then(() => counter2.Should().Be(2), 2);

            await Task.Delay(500);
            counter1.Should().Be(2);
            counter2.Should().Be(2);
        }

        /// <summary>
        /// HELP WANTED - exception gets thrown, but I'm not sure how to test for it since it happens during dispatch...
        /// </summary>
        [Fact]
        public void ThrowExceptionIfUsingUnregisteredDispatcher()
        {
            //bool set = false;
            //Func<Task> dispatch = async () =>
            //{
            //    Task.Run(() => { }).Then(() => set = true, 1000);
            //    await Task.Delay(100);
            //};

            //dispatch.Should().Throw<KeyNotFoundException>();
            //set.Should().BeFalse();
        }
    }
}
