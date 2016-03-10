using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utility;

namespace AsyncTest
{
    [TestClass]
    public class AsyncTest
    {
        [TestMethod]
        public async Task MessageLoop()
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(TimeSpan.FromSeconds(1));
            var token = tokenSource.Token;

            var pumpTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    #region Output

                    "Pumping...".Output();

                    #endregion

                    await HandleMessage().ConfigureAwait(false);
                }
            });

            await pumpTask.ConfigureAwait(false);

            tokenSource.Dispose();
        }

        static Task HandleMessage()
        {
            return Task.Delay(TimeSpan.FromSeconds(1));
        }
        
        [TestMethod]
        public async Task TheCompletePumpWithAsyncHandleMessage()
        {
            var runningTasks = new ConcurrentDictionary<Task, Task>();
            var semaphore = new SemaphoreSlim(100);
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var token = tokenSource.Token;
            int numberOfTasks = 0;

            var pumpTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(token).ConfigureAwait(false);
                    Interlocked.Increment(ref numberOfTasks);

                    var task = HandleMessageWithCancellation(token);

                    runningTasks.TryAdd(task, task);

                    task.ContinueWith(t =>
                    {
                        semaphore.Release();
                        Task taskToBeRemoved;
                        runningTasks.TryRemove(t, out taskToBeRemoved);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                        .Ignore();
                }
            });

            await pumpTask.IgnoreCancellation().ConfigureAwait(false);
            await Task.WhenAll(runningTasks.Values).IgnoreCancellation().ConfigureAwait(false);
            tokenSource.Dispose();
            semaphore.Dispose();

            $"Consumed {numberOfTasks} messages with concurrency {semaphore.CurrentCount} in 5 seconds. Throughput {numberOfTasks / 5} msgs/s"
                .Output();
        }

        static Task HandleMessageWithCancellation(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Delay(1000, cancellationToken);
        }


        [TestMethod]
        public async Task TheCompletePumpWithBlockingHandleMessage()
        {
            var runningTasks = new ConcurrentDictionary<Task, Task>();
            var semaphore = new SemaphoreSlim(100);
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var token = tokenSource.Token;
            int numberOfTasks = 0;

            var pumpTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await semaphore.WaitAsync(token).ConfigureAwait(false);

                    var runningTask = Task.Run(() =>
                    {
                        Interlocked.Increment(ref numberOfTasks);

                        return BlockingHandleMessage();
                    }, CancellationToken.None);

                    runningTasks.TryAdd(runningTask, runningTask);

                    runningTask.ContinueWith(t =>
                    {
                        semaphore.Release();

                        Task taskToBeRemoved;
                        runningTasks.TryRemove(t, out taskToBeRemoved);
                    }, TaskContinuationOptions.ExecuteSynchronously)
                        .Ignore();
                }
            });

            await pumpTask.IgnoreCancellation().ConfigureAwait(false);
            await Task.WhenAll(runningTasks.Values).IgnoreCancellation().ConfigureAwait(false);
            tokenSource.Dispose();

            $"Consumed {numberOfTasks} messages with concurrency {semaphore.CurrentCount} in 5 seconds. Throughput {numberOfTasks / 5} msgs/s"
                .Output();
        }

        private static Task BlockingHandleMessage(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            Thread.Sleep(1000);
            return Task.CompletedTask;
        }
    }


}