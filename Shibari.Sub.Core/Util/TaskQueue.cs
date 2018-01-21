using System;
using System.Threading.Tasks;

namespace Shibari.Sub.Core.Util
{
    public class TaskQueue
    {
        private readonly object _key = new object();
        private Task _previous = Task.FromResult(false);

        public Task<T> Enqueue<T>(Func<Task<T>> taskGenerator)
        {
            lock (_key)
            {
                var next = _previous.ContinueWith(t => taskGenerator()).Unwrap();
                _previous = next;
                return next;
            }
        }

        public Task Enqueue(Func<Task> taskGenerator)
        {
            lock (_key)
            {
                var next = _previous.ContinueWith(t => taskGenerator()).Unwrap();
                _previous = next;
                return next;
            }
        }
    }
}