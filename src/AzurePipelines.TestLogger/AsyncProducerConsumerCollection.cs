using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzurePipelines.TestLogger
{
    /// <remarks>
    /// Adopted from https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/consuming-the-task-based-asynchronous-pattern.
    /// </remarks>
    internal class AsyncProducerConsumerCollection<T>
    {
        private readonly Queue<T> _collection = new Queue<T>();
        private readonly Queue<TaskCompletionSource<T[]>> _waiting = new Queue<TaskCompletionSource<T[]>>();
        private bool _canceled = false;

        public void Cancel()
        {
            TaskCompletionSource<T[]>[] allWaiting;
            lock (_collection)
            {
                _canceled = true;
                allWaiting = _waiting.ToArray();
                _waiting.Clear();
            }

            foreach (TaskCompletionSource<T[]> tcs in allWaiting)
            {
                tcs.TrySetResult(new T[] { });
            }
        }

        public void Add(T item)
        {
            TaskCompletionSource<T[]> tcs = null;
            lock (_collection)
            {
                if (_waiting.Count > 0)
                {
                    tcs = _waiting.Dequeue();
                }
                else
                {
                    _collection.Enqueue(item);
                }
            }

            tcs?.TrySetResult(new[] { item });
        }

        /// <summary>
        /// Queue producer for consumers to <c>await</c> on.
        /// </summary>
        /// <returns>Array of all available items.  Empty array if queue is being canceled.</returns>
        public Task<T[]> TakeAsync()
        {
            lock (_collection)
            {
                if (_collection.Count > 0)
                {
                    Task<T[]> result = Task.FromResult(_collection.ToArray());
                    _collection.Clear();
                    return result;
                }
                else if (!_canceled)
                {
                    TaskCompletionSource<T[]> tcs = new TaskCompletionSource<T[]>();
                    _waiting.Enqueue(tcs);
                    return tcs.Task;
                }
                else
                {
                    // canceled == true
                    return Task.FromResult(new T[] { });
                }
            }
        }
    }
}