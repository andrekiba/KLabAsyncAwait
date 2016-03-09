using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nito.AsyncEx;

namespace AsyncAwait
{
    [TestClass]
    public class AsyncAwait
    {
        static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync(args));

            Console.WriteLine("End");
            Console.ReadLine();
        }

        static async Task MainAsync(string[] args)
        {
            await DoSomethingAsync();

            Task<int> t = DoSomethingAsync();
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " libero di fare altro nel frattempo!");
            int result = await t;
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " risultato finale: " + result);

            //await TrySomethingAsync();

            //Report Progress
            //await ReportProgressAsync();

            //Deadlock
            //Deadlock();
      
        }

        #region CPU-bound

        [TestMethod]
        public async Task CPUBound()
        {
            Parallel.ForEach(Enumerable.Range(1, 100), CpuBoundMethod);

            //await Task.Run(() => CpuBoundMethod(201));
            //await Task.Factory.StartNew(() => CpuBoundMethod(202));
        }

        static void CpuBoundMethod(int i)
        {
            Console.WriteLine(i);
        }

        #endregion

        #region Sequential - Concurrent

        [TestMethod]
        public async Task Sequential()
        {
            var sequential = Enumerable.Range(1, 4).Select(t => Task.Delay(TimeSpan.FromSeconds(1)));

            foreach (var task in sequential)
            {
                await task;
            }
        }

        [TestMethod]
        public async Task Concurrent()
        {
            var concurrent = Enumerable.Range(1, 4).Select(t => Task.Delay(TimeSpan.FromSeconds(1)));
            await Task.WhenAll(concurrent);
            //await Task.WhenAny(concurrent);
        }

        #endregion

        #region IO-bound

        [TestMethod]
        public void TestDoSomethingAsync()
        {
            AsyncContext.Run(async () =>
            {
                Task<int> t = DoSomethingAsync();

                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " libero di fare altro nel frattempo!");

                int result = await t;

                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " risultato finale: " + result);
            }); 
        }

        static async Task<int> DoSomethingAsync()
        {
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " start DoSomethingAsync" );

            int result = 6;
            
            await Task.Delay(TimeSpan.FromSeconds(2));
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " incremento risultato di 10");
            result += 10;

            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " end DoSomethingAsync");
            return result;
        }

        #endregion

        #region Async Exception

        [TestMethod]
        public async Task TestTrySomethingAsync()
        {
            await TrySomethingAsync();
        }

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
                Console.WriteLine(ex.Message);
            }
        }

        static async Task ThrowExceptionAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            throw new NotImplementedException();
        }

        #endregion

        #region Async Void

        [TestMethod]
        public async Task AsyncVoid()
        {
            try
            {
                AvoidAsyncVoid();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine(e);
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        static async void AvoidAsyncVoid()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new InvalidOperationException("Eccezione non catturata!");
        }

        #endregion

        #region Deadlock

        [TestMethod]
        public void TestDeadlock()
        {
            AsyncContext.Run(async () =>
            {
               await Deadlock();
            });            
        }

        static async Task Deadlock()
        {
            //richiedere Result significa bloccare in modo sincorono il chiamante in attesa del risultato
            //se SynchronizationContext ammette un singolo thread accade che il thread rimane bloccato in attesa
            //e non può essere richiamato quando il Task è completo 
            
            var result = DoSomethingAsync().Result;
            //var result = await DoSomethingAsync();

            //Task.Delay(TimeSpan.FromSeconds(2)).Wait();
            //await Task.Delay(TimeSpan.FromSeconds(2))

            //qui non ci arriva mai --> deadlock
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " rusultato: " + result);
        }

        #endregion

        #region ConfigureAwait

        [TestMethod]
        public void TestConfigureAwait()
        {
            AsyncContext.Run(async () =>
            {
                Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");

                await DoSomethingAsync();
                Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");

                await DoSomethingAsync().ConfigureAwait(false);
                Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");
            }); 
        }



        #endregion

        #region Report Progress

        [TestMethod]
        public async Task TestReportProgressAsync()
        {
            AsyncContext.Run(async () =>
            {
                await ReportProgressAsync();
            });   
        }

        static async Task ReportProgressAsync()
        {
            //attenzione che il report può avvenire in asincrono quindi è meglio utilizzare un value type o un tipo immutabile
            //come parametro T per evitare che il valore venga modificato dalla continuazione del metodo in asincrono  
            var progress = new Progress<int>();
            progress.ProgressChanged += (sender, p) =>
            {
                //N.B.: la callback cattura il contesto, sappiamo che quando viene costrutita in questo caso il contesto è quello
                //del Main thread quindi è possibile aggiornare l'interfaccia senza incorrere in problemi
                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " report progress: " + p);
            };

            await DoSomethingWithProgressAsync(progress);
        }

        static async Task<int> DoSomethingWithProgressAsync(IProgress<int> progress = null)
        {
            int val = 6;
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " incremento risultato di 10");
            val += 10;
            progress?.Report(val);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " incremento risultato di 10");
            val += 10;
            progress?.Report(val);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " incremento risultato di 10");
            val += 10;
            progress?.Report(val);
            return val;
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
