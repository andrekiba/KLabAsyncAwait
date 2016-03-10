using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        public async Task TestCpuBound()
        {
            //esegue il metodo in parallelo su più thread
            Parallel.ForEach(Enumerable.Range(1, 100), CpuBoundMethod);

            //è corretto utilizzare Task.Run solo per operazioni CPU-bound poichè utilizza un thread
            await Task.Run(() => CpuBoundMethod(100));
            await Task.Factory.StartNew(() => CpuBoundMethod(101));

            //var result = Enumerable.Range(1, 10000).AsParallel().Select(x => x.ToString());
            //result.ToList().ForEach(Console.WriteLine);
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
            //SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            //Console.WriteLine(SynchronizationContext.Current != null ? SynchronizationContext.Current.ToString() : "null");

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

            await Task.Delay(TimeSpan.FromSeconds(4));
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " incremento risultato di 10");
            result += 10;

            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " end DoSomethingAsync");
            return result;
        }

        [TestMethod]
        public async Task TestIoBound()
        {
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " start IoBoundMethod");

            await IoBoundMethod();

            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " end IoBoundMethod");
        }

        static async Task IoBoundMethod()
        {
            using (var stream = new FileStream(".\\IoBound.txt", FileMode.OpenOrCreate))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync("Scrivo 6 in asincrono!");
                writer.Close();
                stream.Close();
            }
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
                Console.WriteLine(e.Message);
            }

            //await Task.Delay(TimeSpan.FromSeconds(2));
        }

        static async void AvoidAsyncVoid()
        {
            Console.WriteLine("Sono dentro AvoidAsyncVoid");
            await Task.Delay(TimeSpan.FromSeconds(1));

            Console.WriteLine("Sto per sollevare eccezione");
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

        #region WaitAll

        [TestMethod]
        public async Task TestConcurrentlyOperations()
        {
            await DoOperationsConcurrentlyAsync();
        }

        public async Task DoOperationsConcurrentlyAsync()
        {
            Task<int> task1 = Task.FromResult(3);
            Task<int> task2 = Task.FromResult(5);
            Task<int> task3 = Task.FromResult(7);

            var allTasks = Task.WhenAll(task1, task2, task3);

            // a questo punto tutti e 3 i task sono in running

            // WhenAll reswtituisce un task che diventa completo quando tutti i task sottesi sono completi
            // se i task ritornano tutti lo stesso tipo il risutlato sarà un array dei risultati
            int[] results = await allTasks;
            
            results.ToList().ForEach(Console.WriteLine);
        }

        static async Task<string> DownloadAllAsync(IEnumerable<string> urls)
        {
            var httpClient = new HttpClient();
            // voglio fare il downlaod di tutte le url
            var downloads = urls.Select(url => httpClient.GetStringAsync(url));
            // qui i task non sono ancora partiti perchè la sequenza non è ancora stata valutata
            
            // qui partono effettivamente i task perchè viene materializzata l'IEnumerable
            Task<string>[] downloadTasks = downloads.ToArray();
            
            // attende in asincrono che tutti i download siano temrinati
            string[] htmlPages = await Task.WhenAll(downloadTasks);
            return string.Concat(htmlPages);
        }

        #endregion

        #region WaitAny

        [TestMethod]
        public async Task TestGetFirstToEndAsync()
        {
            await GetFirstToEndAsync();
        }

        public async Task GetFirstToEndAsync()
        {
            // effettua due operazioni in asincorno e vede chi finisce prima
            Task<int>[] tasks = { DoSomethingAsync(), DoSomethingAsync() };

            // attende il primo che risponde
            Task<int> firstTask = await Task.WhenAny(tasks);
            var result = await firstTask;

            Console.WriteLine(result);

            //Console.WriteLine(await await Task.WhenAny(tasks));
        }

        #endregion

        #region Processare i Task man mano che finsicono

        [TestMethod]
        public async Task TestUseOrderByCompletionAsync()
        {
            await UseOrderByCompletionAsync();
        }

        static async Task<int> DelayAndReturnAsync(int n)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return n;
        }

        static async Task UseOrderByCompletionAsync()
        {
            Task<int> task1 = DelayAndReturnAsync(3);
            Task<int> task2 = DelayAndReturnAsync(5);
            Task<int> task3 = DelayAndReturnAsync(7);

            var tasks = new[] {task1, task2, task3};

            foreach (var task in tasks.OrderByCompletion())
            {
                Console.WriteLine(await task);
            }
        }

        #endregion

        #region Exceptions

        [TestMethod]
        public async Task TestObserveExceptions()
        {
            await ObserveOneExceptionAsync();

            await ObserveAllExceptionAsync();
        }

        static async Task ThrowNotImplementedExceptionAsync()
        {
            await Task.Delay(10);
            throw new NotImplementedException();
        }
        static async Task ThrowInvalidOperationExceptionAsync()
        {
            await Task.Delay(100);
            throw new InvalidOperationException();
        }

        static async Task ObserveOneExceptionAsync()
        {
            var task1 = ThrowNotImplementedExceptionAsync();
            var task2 = ThrowInvalidOperationExceptionAsync();

            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch (Exception ex)
            {
                // "ex" può essere NotImplementedException oppure InvalidOperationException.
                Console.WriteLine(ex.Message);
            }
        }

        static async Task ObserveAllExceptionAsync()
        {
            var task1 = ThrowNotImplementedExceptionAsync();
            var task2 = ThrowInvalidOperationExceptionAsync();

            var allTasks = Task.WhenAll(task1, task2);

            try
            {
                await allTasks;
            }
            catch
            {
                // "ex" è un AggregateEsception
                AggregateException allExceptions = allTasks.Exception;
                allExceptions?.InnerExceptions.ToList().ForEach(ex => Console.WriteLine(ex.Message));
            }
        }

        #endregion

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

        #region Cancellation

        [TestMethod]
        public async Task TestCancellation()
        { 
           await Cancellation(); 
        }

        private async Task Cancellation()
        {
            CancellationTokenSource source = new CancellationTokenSource();
            source.CancelAfter(TimeSpan.FromSeconds(1));
            Task task = Task.Run(() => SlowMethod(source.Token), source.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }
            
        }

        private void SlowMethod(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 200000; i++)
            {
                if (i % 1000 == 0)
                    cancellationToken.ThrowIfCancellationRequested();
            }
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

        #region DelayImplementation

        private static Task Delay(int milliseconds)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            Timer timer = new Timer(x => tcs.SetResult(null), null, milliseconds, Timeout.Infinite);

            tcs.Task.ContinueWith(x => timer.Dispose());

            return tcs.Task;
        }

        #endregion
    }
}
