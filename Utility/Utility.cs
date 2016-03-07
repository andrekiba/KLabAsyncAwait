using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Utility
{
    public static class TaskExtensions
    {
        public static void Ignore(this Task task)
        {
        }

        public static async Task IgnoreCancellation(this Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public static class StringExtensions
    {
        public static void Output(this string value)
        {
            Console.WriteLine(DateTime.Now.ToString("hh:mm:ss:fff") + ": " + value);
        }
    }

    public static class ProcessExtensions
    {
        public static TaskAwaiter<int> GetAwaiter(this Process process)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);
            if (process.HasExited) tcs.TrySetResult(process.ExitCode);
            return tcs.Task.GetAwaiter();
        }
    }
}
