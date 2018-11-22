using Kelson.Common.Async;
using System;
using System.Threading;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var dispatcher = new WorkerDispatcher(
                onException: e => Console.Error.Write(e),
                priority: ThreadPriority.AboveNormal,
                threadCount: 10,
                faultBufferSize: 2);
            
            for (int i = 0; i < 100; i++)
            {
                int number = i;
                dispatcher.Dispatch(() =>
                    {
                        Thread.Sleep(100);
                        if (number == 81)
                            throw new NotImplementedException("Encountered 81!");
                        Console.WriteLine(number);
                    });
            }

            Console.WriteLine("Done dispatching");
            Console.ReadLine();
            dispatcher.Stop();
            foreach (var fault in dispatcher.Faults)
            {
                Console.WriteLine(fault);
                Console.WriteLine("Dispatcher Trace: ");
                foreach (var frame in fault.DispatcherStack)
                    Console.WriteLine(frame);
                Console.WriteLine("Exception Trace: ");
                foreach (var frame in fault.ExceptionStack)
                    Console.WriteLine(frame);
            }
            Console.ReadLine();
        }
    }
}
