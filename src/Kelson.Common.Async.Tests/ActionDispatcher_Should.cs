using FluentAssertions;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kelson.Common.Async.Tests
{
    public class ActionDispatcher_Should
    {
        [Fact]
        public void DispatchToHandler()
        {
            for (int i = 0; i < 1000; i++)
            {
                int count = -1;

                void handler(Action a)
                {
                    Task.Factory.StartNew(a);
                }

                var dispatcher = new ActionDispatcher(handler, e => { });

                var wait = new AutoResetEvent(false);

                dispatcher.Dispatch(() =>
                {                
                    count = i;
                    wait.Set();

                }, new StackTrace(1));               

                wait.WaitOne();

                dispatcher.Faults.Count().Should().Be(0);

                count.Should().Be(i);
            }
        }

        [Fact]
        public void RecordAndEmitExceptions()
        {
            void handler(Action a)
            {
                a();
            }

            Exception expected = null;

            var dispatcher = new ActionDispatcher(handler, e => expected = e);

            dispatcher.Dispatch(() =>
            {
                throw new Exception("Expected");
            }, new StackTrace(1));

            expected.Should().BeAssignableTo<DispatcherException>();
            expected.InnerException.Message.Should().Be("Expected");            
            dispatcher.Faults.Single().Should().BeSameAs(expected);
        }
    }
}
