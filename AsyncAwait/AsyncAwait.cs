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

            //await DoSomethingAsync();
            //await TrySomethingAsync();

            //Deadlock
            Deadlock();

            //Report Progress
            //await ReportProgressAsync();

            //ConfigureAwait
            //Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");
            //await DoSomethingAsync();
            //Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");
            //await DoSomethingAsync().ConfigureAwait(false);
            //Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");
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

        static async Task<int> DoSomethingAsync(IProgress<int> progress = null)
        {
            int val = 6;
            // Asynchronously wait 1 second.
            await Task.Delay(TimeSpan.FromSeconds(1));
            val += 10;
            progress?.Report(val);
            await Task.Delay(TimeSpan.FromSeconds(1));
            val += 10;
            progress?.Report(val);
            await Task.Delay(TimeSpan.FromSeconds(1));
            val += 10;
            progress?.Report(val);
            return val;
        }

        #endregion

        #region Async Exception

        static async Task TrySomethingAsync()
        {
            // Il metodo inizia e l'eccezione viene salvata nel task
            Task task = ThrowExceptionAsync();
            try
            {
                // l'eccezione viene sollevata qui quando si attende il task
                await task;
            }
            catch (NotImplementedException ex)
            {
                Trace.WriteLine(ex);
            }
        }

        static async Task ThrowExceptionAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            throw new NotImplementedException();
        }

        #endregion

        #region Deadlock

        static void Deadlock()
        {
            //richiedere Result significa bloccare in modo sincorono il chiamante in attesa del risultato
            //se SynchronizationContext ammette un singolo thread succede che il Main thread rimane bloccato in attesa
            //e non può essere richiamato quando l'await ritorna 
            var result = DoSomethingAsync().Result;

            //qui non ci arriva mai --> deadlock
            Console.WriteLine(result);
        }

        #endregion

        #region Report Progress

        static async Task ReportProgressAsync()
        {
            //attenzione che il report può avvenire in asincrono quindi è meglio utilizzare un value type o un tipo immutabile
            //come parametro T per evitare che il valore venga modificato dalla continuazione del metodo in asincrono  
            var progress = new Progress<int>();
            progress.ProgressChanged += (sender, i) =>
            {
                //N.B.: la callback cattura il contesto, sappiamo che quando viene costrutita in questo caso il contesto è quello
                //del Main thread quindi è possibile aggiornare l'interfaccia senza icorrere in problemi
                Console.WriteLine(i);
            };

            await DoSomethingAsync(progress);
        }

        #endregion

        #region Mapping
   
        /*
            
        Type                                    Lambda                                                  Parameters	    Return Value
            
        Action	                                () => { }	                                            None	        None
        Func<Task>	                            async () => { await Task.Yield(); }	                    None	        None
            
        Func<TResult>	                        () => { return 6; }	                                    None	        TResult
        Func<Task<TResult>>	                    async () => { await Task.Yield(); return 6; }	        None	        TResult
            
        Action<TArg1>	                        x => { }	                                            TArg1	        None
        Func<TArg1, Task>	                    async x => { await Task.Yield(); }	                    TArg1	        None
            
        Func<TArg1, TResult>	                x => { return 6; }	                                    TArg1	        TResult
        Func<TArg1, Task<TResult>>	            async x => { await Task.Yield(); return 6; }	        TArg1	        TResult
            
        Action<TArg1, TArg2>	                (x, y) => { }	                                        TArg1, TArg2	None
        Func<TArg1, TArg2, Task>	            async (x, y) => { await Task.Yield(); }	                TArg1, TArg2	None
            
        Func<TArg1, TArg2, TResult>	            (x, y) => { return 6; }	                                TArg1, TArg2	TResult
        Func<TArg1, TArg2, Task<TResult>>	    async (x, y) => { await Task.Yield(); return 6; }	    TArg1, TArg2	TResult

        */     

        #endregion
    }
}
