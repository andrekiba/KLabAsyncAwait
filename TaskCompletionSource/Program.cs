using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskCompletionSource
{
    class Program
    {
        static void Main(string[] args)
        {
           
        }

        public static Task Delay(int millisecondsTimeout)
        {
            var tcs = new TaskCompletionSource<bool>();

            // Register for the "event".
            //   For example, if this is an I/O operation, start the I/O and register for its completion.

            // When the event fires, it should call:
            //   tcs.TrySetResult(...); // For a successful event.
            // or
            //   tcs.TrySetException(...); // For some error.
            // or
            //   tcs.TrySetCanceled(); // If the event was canceled.

            // TaskCompletionSource is thread-safe, so you can call these methods from whatever thread you want.

            // Return the Task<int>, which will complete when the event triggers.

            new Timer(self =>
            {
                //callback
                ((IDisposable)self).Dispose();
                tcs.TrySetResult(true);
            }).Change(millisecondsTimeout, -1);
            return tcs.Task;
        }

    }
}
