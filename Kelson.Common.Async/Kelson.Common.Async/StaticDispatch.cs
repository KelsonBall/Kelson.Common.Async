using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kelson.Common.Async
{
    /// <summary>
    /// Allows the registration of dispatch callbacks, and the continuation of tasks via those dispatchers
    /// </summary>
    public static class StaticDispatch
    {
        private static readonly ConcurrentDictionary<int, Action<Action>> CallbackDispatcher = new ConcurrentDictionary<int, Action<Action>>();

        /// <summary>
        /// Register an action with wich to dispatch actions to specific threads
        /// </summary>
        public static void RegisterCallbackDispatcher(Action<Action> callback, int dispatcherHandle = 0)
        {
            CallbackDispatcher[dispatcherHandle] = callback;
        }

        private static Action<Exception> ExceptionLogger;

        /// <summary>
        /// Register a callback to be called when an exception is thrown by a piped task
        /// </summary>
        public static void RegisterExceptionLogger(Action<Exception> handler)
        {
            ExceptionLogger += handler;
        }

        /// <summary>
        /// Run the action on another thread, specified by the dispatchHandle
        /// </summary>
        public static void Dispatch(Action action, int dispatcherHandle = 0)
        {
            if (CallbackDispatcher.TryGetValue(dispatcherHandle, out Action<Action> handler))
                handler(action);
            else
                throw new KeyNotFoundException($"Could not find a registered callback dispatcher with handle {dispatcherHandle}, use {nameof(StaticDispatch)}.{nameof(RegisterCallbackDispatcher)} to register dispatchers.");
        }

        /// <summary>
        /// When the task completes, dispatches its result through the action
        /// </summary>
        public static void Then<T>(this Task<T> task, Action<T> action, int dispatcherHandle = 0, [CallerMemberName] string callerName = null)
        {
            task.ContinueWith(
                t => Dispatch(() =>
                {
                    try
                    {
                        var callerName2 = callerName;
                        action(t.Result);
                    }
                    catch (Exception e)
                    {
                        ExceptionLogger?.Invoke(e);
                        throw e;
                    }
                }, dispatcherHandle));
        }

        /// <summary>
        /// When teh task finishes, dispatch the action
        /// </summary>
        public static void Then(this Task task, Action action, int dispatchHandle = 0, [CallerMemberName] string callerName = null)
        {
            task.ContinueWith(
                t => Dispatch(() => {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        ExceptionLogger?.Invoke(e);
                        throw e;
                    }
                }, dispatchHandle));
        }

        /// <summary>
        /// Waits for a task to finish and then confirms it doesn't contain exceptions.
        /// If no exception, calls successCallback if provided
        /// If exception, calls errorCallback if provided, otherwise throws the exception
        /// If canceled, calls cancelCallback if provided
        /// </summary>
        public static void Confirm(this Task task, int dispatchHandle = 0, Action successCallback = null, Action<Exception> errorCallback = null, Action cancelCallback = null)
        {
            task.ContinueWith(t => Dispatch(() =>
            {
                if (t.IsFaulted)
                {
                    if (errorCallback != null)
                        errorCallback?.Invoke(t.Exception);
                    else
                        throw t.Exception;
                }
                else if (t.IsCompleted)
                    successCallback?.Invoke();
                else if (t.IsCanceled)
                    cancelCallback?.Invoke();
            }, dispatchHandle));
        }

        /// <summary>
        /// Waits for a task to finish and then confirms it doesn't contain exceptions.
        /// If no exception, calls successCallback if provided
        /// If exception, calls errorCallback if provided, otherwise throws the exception
        /// If canceled, calls cancelCallback if provided
        /// </summary>
        public static void Confirm<T>(this Task<T> task, int dispatchHandle = 0, Action<T> successCallback = null, Action<Exception> errorCallback = null, Action cancelCallback = null)
        {
            task.ContinueWith(t => Dispatch(() =>
            {
                if (t.IsFaulted)
                {
                    if (errorCallback != null)
                        errorCallback?.Invoke(t.Exception);
                    else
                        throw t.Exception;
                }
                if (t.IsCompleted)
                    successCallback?.Invoke(t.Result);
                else if (t.IsCanceled)
                    cancelCallback?.Invoke();
            }, dispatchHandle));
        }
    }
}
