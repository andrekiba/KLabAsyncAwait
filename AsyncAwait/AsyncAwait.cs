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
            Task<int> t = DoSomethingAsync();
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " libero di fare altro nel frattempo!");
            int result = await t;
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " risultato finale: " + result);
        }

        #region CPU-bound

        [TestMethod]
        public void TestCpuBound()
        {
            //Parallel.ForEach(Enumerable.Range(1, 100), CpuBoundMethod);

            AsyncContext.Run(async () =>
            {
                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId);

                Parallel.ForEach(Enumerable.Range(1, 100), CpuBoundMethod);

                await Task.Run(() => CpuBoundMethod(100));
                await Task.Factory.StartNew(() => CpuBoundMethod(101));
            });
        }

        static void CpuBoundMethod(int n)
        {
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " scrivo " + n);
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
        public async Task TestAsyncVoid()
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

        //WPF
        private async void DownloadButton_Click(object sender, EventArgs e)
        {
            // Attende in modo asincrono UI thread non è bloccato
            await DownloadFileAsync("file.txt");

            // viene recuperato il contesto e quindi possiamo aggiornare direttamente la UI
            //resultTextBox.Text = "File downloaded!";
        }

        private async Task DownloadFileAsync(string fileName)
        {
            
            // utilizziamo HttpClient o simili per fare il download.
            //var fileContent = await DownloadFileContentsAsync(fileName).ConfigureAwait(false);

            // poichè abbiamo usato ConfigureAwait(false), qui non siamo più nel contesto della UI.
            // Invece stiamo eseguendo su un thread del thread pool

            // scrive il file su disco in asincrono
            //await WriteToDiskAsync(fileName, fileContent).ConfigureAwait(false);

            // la secondo ConfigureAwait non è necessaria ma è buona pratica metterla
        }

        #endregion

        #region Composition

        public async Task DoOperationsConcurrentlyAsync()
        {
            Task[] tasks = new Task[3];
            tasks[0] = DoSomethingAsync();
            tasks[1] = DoSomethingAsync();
            tasks[2] = DoSomethingAsync();

            // a questo punto tutti e 3 i task sono in running

            // WhenAll reswtituisce un task che diventa completo quando tutti i task sottesi sono completi
            await Task.WhenAll(tasks);
        }

        public async Task<int> GetFirstToRespondAsync()
        {
            // chiama due web service e vede chi risponde prima
            Task<int>[] tasks = { DoSomethingAsync(), DoSomethingAsync() };

            // attende il primo che risponde
            Task<int> firstTask = await Task.WhenAny(tasks);

            // Return the result.
            return await firstTask;
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

        #region Guidelines

        /*

        Old                     New                                 Description
        
        task.Wait	            await task	                        Wait/await for a task to complete
        
        task.Result	            await task	                        Get the result of a completed task
        
        Task.WaitAny	        await Task.WhenAny	                Wait/await for one of a collection of tasks to complete
        
        Task.WaitAll	        await Task.WhenAll	                Wait/await for every one of a collection of tasks to complete
        
        Thread.Sleep	        await Task.Delay	                Wait/await for a period of time
        
        Task constructor	    Task.Run or TaskFactory.StartNew	Create a code-based task

        */

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
