using System;
using System.Threading.Tasks;

namespace Kelson.Common.Async
{
    /// <summary>
    /// Creates a single task that "owns" the value and controls access to it
    /// </summary>    
    public class Actor<T> where T : class
    {
        private readonly Task<T> asset;

        public Actor(T value)
        {
            asset = Task.Run(() => value);
        }

        public async Task Do(Action<T> action)
        {
            await asset.ContinueWith(t => action(t.Result));
        }
    }
}
