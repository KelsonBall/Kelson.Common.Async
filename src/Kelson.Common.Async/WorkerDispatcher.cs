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

        private readonly Thread[] Channels;

        private readonly ConcurrentQueue<Message> queue = new ConcurrentQueue<Message>();

        private readonly object faultLock = new object();
        private readonly Queue<DispatcherException> faults;

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

        public bool[] Alive { get; private set; }

        private readonly Action<Exception> onException;
        private readonly Action onDispatch;

        private readonly int pollingDelay;
        private readonly int faultBufferSize;

        public WorkerDispatcher(Action<Exception> onException, Action onDispatch = null, ThreadPriority priority = ThreadPriority.Normal, int threadCount = 1, int faultBufferSize = 10)
        {
            this.onException = onException;
            this.onDispatch = onDispatch ?? (() => { });
            this.faultBufferSize = faultBufferSize;
            faults = new Queue<DispatcherException>(faultBufferSize);
            Channels =
                Enumerable.Range(0, threadCount)
                    .Select(id =>
                        new Thread(() => WatchQueue(id).Wait())
                        {
                            Priority = priority,
                            Name = "Worker Dispatcher",
                        })
                    .ToArray();

            Alive = Channels.Select(c => false).ToArray();

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
            
            foreach (var channel in Channels)
                channel.Start();
        }

        public void Stop()
        {
            Running = false;
        }

        public void Abort()
        {
            Running = false;
            for (int i = 0; i < Alive.Length; i++)
                Alive[i] = false;
            foreach (var channel in Channels)
                channel.Abort();
        }

        private async Task WatchQueue(int id)
        {
            Running = true;
            Alive[id] = true;
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
                        EnqueueException(exception);
                        onException(e);
                    }
                }
                else
                    await Task.Delay(pollingDelay);
            }
            Alive[id] = false;
        }

        private void EnqueueException(DispatcherException exception)
        {
            lock (faultLock)
            {
                if (faults.Count >= faultBufferSize)
                    faults.Dequeue();
                faults.Enqueue(exception);                
            }
        }

        public void Dispatch(Action action, StackTrace trace = null, [CallerMemberName] string dispatchee = null)
        {
            trace = trace ?? new StackTrace(1);
            queue.Enqueue(new Message
            {
                Action = action,
                Dispatchee = dispatchee,
                Trace = trace,
            });
        }
    }
}
