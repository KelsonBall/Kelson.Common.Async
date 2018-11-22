using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Kelson.Common.Async
{
    public class ActionDispatcher : IDispatcher
    {
        private readonly Action<Action> action;
        private readonly Action<Exception> onException;

        public ActionDispatcher(Action<Action> action, Action<Exception> onException) 
            => (this.action, this.onException) = (action, onException);

        private readonly object faultsLock = new object();
        private readonly Queue<DispatcherException> faults = new Queue<DispatcherException>();

        public IEnumerable<DispatcherException> Faults
        {
            get
            {
                DispatcherException[] exceptions = null;
                lock (faultsLock)
                {
                    exceptions = faults.ToArray();
                }
                return exceptions.AsEnumerable();
            }
        }

        public void Dispatch(Action dispatch, StackTrace trace, [CallerMemberName] string dispatchee = null)
        {            
            try
            {
                action(dispatch);
            }
            catch (Exception e)
            {
                var exception = new DispatcherException(dispatchee, trace, e);
                lock (faultsLock)
                {
                    faults.Enqueue(exception);
                }
                onException(exception);
            }
        }        
    }
}
