using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Kelson.Common.Async
{
    public class WorkerDispatcher : IDispatcher
    {
        private class Message
        {
            public Action Action { get; set; }
            public string Dispatchee { get; set; }
            public StackTrace Trace { get; set; }
        }

        private readonly Thread Channel;

        private readonly ConcurrentQueue<Message> queue = new ConcurrentQueue<Message>();

        private readonly object faultLock = new object();
        private readonly Queue<DispatcherException> faults = new Queue<DispatcherException>();

        public IEnumerable<DispatcherException> Faults
        {
            get
            {
                DispatcherException[] exceptions = null;
                lock(faultLock)
                {
                    exceptions = faults.ToArray();
                }
                return exceptions.AsEnumerable();
            }            
        }

        public bool Running { get; private set; } = false;

        public bool Alive { get; private set; } = false;

        private readonly Action<Exception> onException;
        private readonly Action onDispatch;

        private readonly int pollingDelay;
        
        public WorkerDispatcher(Action<Exception> onException, Action onDispatch = null, ThreadPriority priority = ThreadPriority.Normal)
        {
            this.onException = onException;
            this.onDispatch = onDispatch ?? (() => { });
            Channel = new Thread(() => WatchQueue().Wait())
            {
                Priority = priority,
                Name = "Worker Dispatcher",
            };

            switch (priority)
            {
                case ThreadPriority.Lowest:
                    pollingDelay = 1000;
                    break;
                case ThreadPriority.BelowNormal:
                    pollingDelay = 1000 / 8;
                    break;
                case ThreadPriority.Normal:
                    pollingDelay = 1000 / 64;
                    break;
                case ThreadPriority.AboveNormal:
                    pollingDelay = 1000 / 500;
                    break;
                case ThreadPriority.Highest:
                    pollingDelay = 1000 / 1000;
                    break;
            }
            
            Channel.Start();
        }

        public void Stop()
        {
            Running = false;
        }

        public void Abort()
        {
            Running = false;
            Alive = false;
            Channel.Abort();
        }

        private async Task WatchQueue()
        {
            Running = true;
            Alive = true;
            while (Running)
            {
                if (queue.TryDequeue(out Message message))
                {
                    try
                    {
                        onDispatch();
                        message.Action();
                    }
                    catch (Exception e)
                    {
                        var exception = new DispatcherException(message.Dispatchee, message.Trace, e);
                        lock(faultLock)
                        {
                            faults.Enqueue(exception);
                        }
                        onException(e);
                    }
                }
                else
                    await Task.Delay(pollingDelay);
            }
            Alive = false;
        }

        public void Dispatch(Action action, StackTrace trace, [CallerMemberName] string dispatchee = null)
        {            
            queue.Enqueue(new Message
            {
                Action = action,
                Dispatchee = dispatchee,
                Trace = trace,
            });
        }
    }
}
