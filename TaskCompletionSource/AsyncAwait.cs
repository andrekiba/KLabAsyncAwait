using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace AsyncAwait
{
    class AsyncAwait
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));

            Console.WriteLine("End");
            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            //var dump = await DumpWebPageAsync(new WebClient(), new Uri("http://www.elfo.net"));
            //Console.WriteLine(dump);

            //var ip = await LookupHostNameAsync("www.elfo.net");
            //Console.WriteLine(ip);

            await DoSomethingAsync();
            await TrySomethingAsync();
        }

        #region Wrap EAP

        private static Task<string> DumpWebPageAsync(WebClient client, Uri uri)
        {
            var tcs = new TaskCompletionSource<string>();

            DownloadStringCompletedEventHandler handler = null;

            handler = (sender, args) =>
            {
                client.DownloadStringCompleted -= handler;

                if (args.Cancelled)
                    tcs.TrySetCanceled();
                else if (args.Error != null)
                    tcs.TrySetException(args.Error);
                else
                    tcs.TrySetResult(args.Result);
            };

            client.DownloadStringCompleted += handler;
            client.DownloadStringAsync(uri);

            return tcs.Task;
        }

        #endregion

        #region Wrap APM

        private static Task<IPAddress[]> LookupHostNameAsync(string hostName)
        {
            //si utilizza uno degli overload di TaskFactory.FromAsync

            //come primo parametro si passa il metodo Begin
            //come secondo si passa il metodo End
            //si passano in ordine tutti i parametri che verrebbero passati al metodo begin
            //si passa null come state obejct
            return Task<IPAddress[]>.Factory.FromAsync(Dns.BeginGetHostAddresses, Dns.EndGetHostAddresses, hostName, null);
        }

        #endregion

        #region Something Async

        static async Task DoSomethingAsync()
        {
            int val = 6;
            // Asynchronously wait 1 second.
            await Task.Delay(TimeSpan.FromSeconds(1));
            val += 10;
            // Asynchronously wait 1 second.
            await Task.Delay(TimeSpan.FromSeconds(1));
            Trace.WriteLine(val);
        }

        #endregion

        #region Async Exception

        static async Task TrySomethingAsync()
        {
            // The exception will end up on the Task, not thrown directly.
            Task task = ThrowExceptionAsync();
            try
            {
                // The Task's exception will be raised here, at the await.
                await task;
            }
            catch (NotSupportedException ex)
            {
                Trace.WriteLine(ex);
                throw;
            }
        }

        static async Task ThrowExceptionAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            throw new NotImplementedException();
        }

        #endregion
    }
}
