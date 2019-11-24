using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kelson.Common.Async
{
    public interface IDispatcher
    {
        IEnumerable<DispatcherException> Faults { get; }
        void Dispatch(Action action, StackTrace dispatchTrace, string dispachee);
    }
}
